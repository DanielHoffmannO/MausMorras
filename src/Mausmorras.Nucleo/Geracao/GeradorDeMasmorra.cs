using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Geracao;

public sealed class GeradorDeMasmorra
{
    private static readonly TipoDeCelula[] TemasDeTerreno =
    {
        TipoDeCelula.Grama, TipoDeCelula.Agua, TipoDeCelula.Entulho, TipoDeCelula.Abismo
    };

    private readonly int _maxSalas;
    private readonly int _tamanhoMinimoSala;
    private readonly int _tamanhoMaximoSala;
    private readonly int _larguraCorredor;

    public GeradorDeMasmorra(int maxSalas = 40, int tamanhoMinimoSala = 6, int tamanhoMaximoSala = 14, int larguraCorredor = 3)
    {
        _maxSalas = maxSalas;
        _tamanhoMinimoSala = tamanhoMinimoSala;
        _tamanhoMaximoSala = tamanhoMaximoSala;
        _larguraCorredor = larguraCorredor;
    }

    public (MapaDaMasmorra Mapa, IReadOnlyList<Sala> Salas) Gerar(int largura, int altura, Random random)
    {
        var mapa = new MapaDaMasmorra(largura, altura);
        var salas = new List<Sala>();

        for (var i = 0; i < _maxSalas; i++)
        {
            var largSala = random.Next(_tamanhoMinimoSala, _tamanhoMaximoSala + 1);
            var altSala = random.Next(_tamanhoMinimoSala, _tamanhoMaximoSala + 1);
            var x = random.Next(1, Math.Max(2, largura - largSala - 1));
            var y = random.Next(1, Math.Max(2, altura - altSala - 1));

            var novaSala = new Sala(x, y, largSala, altSala);

            if (salas.Any(s => s.Sobrepoe(novaSala)))
                continue;

            EscavarSala(mapa, novaSala);

            if (salas.Count > 0)
                EscavarCorredor(mapa, salas[^1].Centro, novaSala.Centro, random, _larguraCorredor);

            salas.Add(novaSala);
        }

        foreach (var sala in salas)
        {
            EspalharTerreno(mapa, sala, random);
            PosicionarPortas(mapa, sala);
        }

        DecorarParedes(mapa, random);
        PosicionarEscada(mapa, salas);

        return (mapa, salas);
    }

    private static void EscavarSala(MapaDaMasmorra mapa, Sala sala)
    {
        for (var x = sala.X; x < sala.X + sala.Largura; x++)
            for (var y = sala.Y; y < sala.Y + sala.Altura; y++)
                mapa[x, y] = TipoDeCelula.Chao;
    }

    private static void EscavarCorredor(MapaDaMasmorra mapa, Posicao de, Posicao para, Random random, int largura)
    {
        if (random.Next(2) == 0)
        {
            EscavarHorizontal(mapa, de.X, para.X, de.Y, largura);
            EscavarVertical(mapa, de.Y, para.Y, para.X, largura);
        }
        else
        {
            EscavarVertical(mapa, de.Y, para.Y, de.X, largura);
            EscavarHorizontal(mapa, de.X, para.X, para.Y, largura);
        }
    }

    private static void EscavarHorizontal(MapaDaMasmorra mapa, int x1, int x2, int y, int largura)
    {
        var metade = largura / 2;
        for (var x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            for (var oy = -metade; oy < largura - metade; oy++)
                mapa[x, y + oy] = TipoDeCelula.Chao;
    }

    private static void EscavarVertical(MapaDaMasmorra mapa, int y1, int y2, int x, int largura)
    {
        var metade = largura / 2;
        for (var y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            for (var ox = -metade; ox < largura - metade; ox++)
                mapa[x + ox, y] = TipoDeCelula.Chao;
    }

    private static void EspalharTerreno(MapaDaMasmorra mapa, Sala sala, Random random)
    {
        if (sala.Largura <= 2 || sala.Altura <= 2)
            return;

        if (random.NextDouble() > 0.5)
            return;

        var tema = TemasDeTerreno[random.Next(TemasDeTerreno.Length)];
        var densidade = tema switch
        {
            TipoDeCelula.Agua => 0.35,
            TipoDeCelula.Abismo => 0.15,
            _ => 0.25
        };

        for (var x = sala.X + 1; x < sala.X + sala.Largura - 1; x++)
        {
            for (var y = sala.Y + 1; y < sala.Y + sala.Altura - 1; y++)
            {
                if (mapa[x, y] == TipoDeCelula.Chao && random.NextDouble() < densidade)
                    mapa[x, y] = tema;
            }
        }
    }

    private static void PosicionarPortas(MapaDaMasmorra mapa, Sala sala)
    {
        for (var x = sala.X; x < sala.X + sala.Largura; x++)
        {
            TentarPosicionarPorta(mapa, x, sala.Y, x, sala.Y - 1);
            TentarPosicionarPorta(mapa, x, sala.Y + sala.Altura - 1, x, sala.Y + sala.Altura);
        }

        for (var y = sala.Y; y < sala.Y + sala.Altura; y++)
        {
            TentarPosicionarPorta(mapa, sala.X, y, sala.X - 1, y);
            TentarPosicionarPorta(mapa, sala.X + sala.Largura - 1, y, sala.X + sala.Largura, y);
        }
    }

    private static void TentarPosicionarPorta(MapaDaMasmorra mapa, int xBorda, int yBorda, int xFora, int yFora)
    {
        if (mapa[xBorda, yBorda] == TipoDeCelula.Chao && mapa[xFora, yFora] == TipoDeCelula.Chao)
            mapa[xBorda, yBorda] = TipoDeCelula.Porta;
    }

    private static void DecorarParedes(MapaDaMasmorra mapa, Random random)
    {
        for (var x = 0; x < mapa.Largura; x++)
        {
            for (var y = 0; y < mapa.Altura; y++)
            {
                if (mapa[x, y] != TipoDeCelula.Parede)
                    continue;

                var adjacenteACaminhavel = mapa.EhCaminhavel(x + 1, y) || mapa.EhCaminhavel(x - 1, y) ||
                                           mapa.EhCaminhavel(x, y + 1) || mapa.EhCaminhavel(x, y - 1);

                if (adjacenteACaminhavel && random.NextDouble() < 0.08)
                    mapa[x, y] = TipoDeCelula.ParedeDecorada;
            }
        }
    }

    private static void PosicionarEscada(MapaDaMasmorra mapa, List<Sala> salas)
    {
        if (salas.Count < 2)
            return;

        var inicio = salas[0].Centro;
        var maisDistante = salas
            .Skip(1)
            .OrderByDescending(s => DistanciaAoQuadrado(inicio, s.Centro))
            .First();

        var escada = maisDistante.Centro;
        mapa[escada.X, escada.Y] = TipoDeCelula.Escada;
    }

    private static int DistanciaAoQuadrado(Posicao a, Posicao b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
