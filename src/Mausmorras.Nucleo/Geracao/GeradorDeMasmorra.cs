using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Geracao;

public sealed class GeradorDeMasmorra
{
    private static readonly TipoDeCelula[] TemasDeTerreno =
    {
        TipoDeCelula.Grama, TipoDeCelula.Agua, TipoDeCelula.Entulho, TipoDeCelula.Terra
    };

    // Tier sobe com o andar (ver EscolherTier) -- assim o loot acompanha a dificuldade dos monstros
    // em vez de ficar identico do andar 1 ao 20
    private static readonly (Func<Item> Criar, int Tier)[] CatalogoDeItens =
    {
        (() => new Item("Capacete de Couro", TipoDeItem.Capacete, 2), 1),
        (() => new Item("Elmo de Ferro", TipoDeItem.Capacete, 4), 2),
        (() => new Item("Elmo Élfico", TipoDeItem.Capacete, 7), 3),
        (() => new Item("Peitoral de Couro", TipoDeItem.Peitoral, 3), 1),
        (() => new Item("Armadura de Ferro", TipoDeItem.Peitoral, 6), 2),
        (() => new Item("Armadura Élfica", TipoDeItem.Peitoral, 10), 3),
        (() => new Item("Calça de Couro", TipoDeItem.Pernas, 2), 1),
        (() => new Item("Grevas de Ferro", TipoDeItem.Pernas, 4), 2),
        (() => new Item("Grevas Élficas", TipoDeItem.Pernas, 7), 3),
        (() => new Item("Botas de Couro", TipoDeItem.Botas, 1), 1),
        (() => new Item("Botas de Ferro", TipoDeItem.Botas, 3), 2),
        (() => new Item("Botas Élficas", TipoDeItem.Botas, 5), 3),
        (() => new Item("Adaga Enferrujada", TipoDeItem.Arma, 2), 1),
        (() => new Item("Espada de Ferro", TipoDeItem.Arma, 4), 2),
        (() => new Item("Espada Élfica", TipoDeItem.Arma, 7), 3),
        (() => new Item("Poção de Vida", TipoDeItem.Generico, 10), 1)
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

    public (MapaDaMasmorra Mapa, IReadOnlyList<Sala> Salas, IReadOnlyDictionary<Posicao, Item> Itens) Gerar(int largura, int altura, int andar, Random random)
    {
        var mapa = new MapaDaMasmorra(largura, altura);
        var salas = new List<Sala>();
        var itens = new Dictionary<Posicao, Item>();

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
                ConectividadeDeMapa.EscavarCorredor(mapa, salas[^1].Centro, novaSala.Centro, random, _larguraCorredor);

            salas.Add(novaSala);
        }

        foreach (var sala in salas)
        {
            EspalharTerreno(mapa, sala, random);
            EspalharPedras(mapa, sala, random);
            EspalharOuro(mapa, sala, random);
            EspalharItens(mapa, sala, andar, random, itens);
            PosicionarPortas(mapa, sala);
        }

        DecorarParedes(mapa, random);

        if (salas.Count > 0)
            ConectividadeDeMapa.GarantirAlcancavel(mapa, salas[0].Centro, salas.Skip(1).Select(s => s.Centro), random, _larguraCorredor);

        PosicionarEscada(mapa, salas);

        return (mapa, salas, itens);
    }

    private static void EscavarSala(MapaDaMasmorra mapa, Sala sala)
    {
        for (var x = sala.X; x < sala.X + sala.Largura; x++)
            for (var y = sala.Y; y < sala.Y + sala.Altura; y++)
                mapa[x, y] = TipoDeCelula.Chao;
    }

    private static void EspalharTerreno(MapaDaMasmorra mapa, Sala sala, Random random)
    {
        if (sala.Largura <= 2 || sala.Altura <= 2)
            return;

        if (random.NextDouble() > 0.65)
            return;

        var tema = TemasDeTerreno[random.Next(TemasDeTerreno.Length)];
        var densidade = tema switch
        {
            TipoDeCelula.Agua => 0.55,
            TipoDeCelula.Terra => 0.20,
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

    private static void EspalharPedras(MapaDaMasmorra mapa, Sala sala, Random random)
    {
        if (sala.Largura <= 2 || sala.Altura <= 2)
            return;

        if (random.NextDouble() > 0.4)
            return;

        var quantidadePedras = random.Next(1, 5);
        for (var i = 0; i < quantidadePedras; i++)
        {
            var x = random.Next(sala.X + 1, sala.X + sala.Largura - 1);
            var y = random.Next(sala.Y + 1, sala.Y + sala.Altura - 1);

            if (mapa[x, y] == TipoDeCelula.Chao)
                mapa[x, y] = TipoDeCelula.Pedra;
        }
    }

    private static void EspalharOuro(MapaDaMasmorra mapa, Sala sala, Random random)
    {
        if (sala.Largura <= 2 || sala.Altura <= 2)
            return;

        if (random.NextDouble() > 0.5)
            return;

        var quantidadePilhas = random.Next(1, 4);
        for (var i = 0; i < quantidadePilhas; i++)
        {
            var x = random.Next(sala.X + 1, sala.X + sala.Largura - 1);
            var y = random.Next(sala.Y + 1, sala.Y + sala.Altura - 1);

            if (mapa[x, y] == TipoDeCelula.Chao)
                mapa[x, y] = TipoDeCelula.Ouro;
        }
    }

    private static void EspalharItens(MapaDaMasmorra mapa, Sala sala, int andar, Random random, Dictionary<Posicao, Item> itens)
    {
        if (sala.Largura <= 2 || sala.Altura <= 2)
            return;

        if (random.NextDouble() > 0.3)
            return;

        var x = random.Next(sala.X + 1, sala.X + sala.Largura - 1);
        var y = random.Next(sala.Y + 1, sala.Y + sala.Altura - 1);

        if (mapa[x, y] != TipoDeCelula.Chao)
            return;

        var item = EscolherItem(andar, random);
        var posicao = new Posicao(x, y);
        mapa[x, y] = TipoDeCelula.Item;
        itens[posicao] = item;
    }

    private static Item EscolherItem(int andar, Random random)
    {
        var tier = EscolherTier(andar, random);
        var candidatos = CatalogoDeItens.Where(e => e.Tier == tier).ToArray();
        return candidatos[random.Next(candidatos.Length)].Criar();
    }

    // andares rasos favorecem tier 1, andares fundos favorecem tier 3 -- sem isso o loot era
    // identico do andar 1 ao 20 mesmo com o monstro ficando cada vez mais forte
    private static int EscolherTier(int andar, Random random)
    {
        var (peso1, peso2, peso3) = andar switch
        {
            <= 3 => (70, 25, 5),
            <= 7 => (30, 50, 20),
            _ => (10, 40, 50)
        };

        var sorteio = random.Next(peso1 + peso2 + peso3);
        if (sorteio < peso1) return 1;
        return sorteio < peso1 + peso2 ? 2 : 3;
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
