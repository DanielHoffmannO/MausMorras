using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Geracao;

internal static class ConectividadeDeMapa
{
    public static void GarantirAlcancavel(MapaDaMasmorra mapa, Posicao origem, IEnumerable<Posicao> destinosObrigatorios, Random random, int larguraCorredor)
    {
        var alcancaveis = CalcularAlcancaveis(mapa, origem);

        foreach (var destino in destinosObrigatorios)
        {
            if (alcancaveis.Contains(destino))
                continue;

            EscavarCorredor(mapa, destino, origem, random, larguraCorredor, permitirEscombros: false);
            alcancaveis = CalcularAlcancaveis(mapa, origem);
        }
    }

    public static HashSet<Posicao> CalcularAlcancaveis(MapaDaMasmorra mapa, Posicao origem)
    {
        var visitado = new HashSet<Posicao> { origem };
        var fila = new Queue<Posicao>();
        fila.Enqueue(origem);

        while (fila.Count > 0)
        {
            var p = fila.Dequeue();
            foreach (var vizinho in new[] { new Posicao(p.X + 1, p.Y), new Posicao(p.X - 1, p.Y), new Posicao(p.X, p.Y + 1), new Posicao(p.X, p.Y - 1) })
            {
                if (visitado.Contains(vizinho) || !mapa.EhCaminhavel(vizinho))
                    continue;

                visitado.Add(vizinho);
                fila.Enqueue(vizinho);
            }
        }

        return visitado;
    }

    public static void EscavarCorredor(MapaDaMasmorra mapa, Posicao de, Posicao para, Random random, int largura, bool permitirEscombros = true)
    {
        if (random.Next(2) == 0)
        {
            EscavarHorizontal(mapa, de.X, para.X, de.Y, largura, random, permitirEscombros);
            EscavarVertical(mapa, de.Y, para.Y, para.X, largura, random, permitirEscombros);
        }
        else
        {
            EscavarVertical(mapa, de.Y, para.Y, de.X, largura, random, permitirEscombros);
            EscavarHorizontal(mapa, de.X, para.X, para.Y, largura, random, permitirEscombros);
        }
    }

    private static void EscavarHorizontal(MapaDaMasmorra mapa, int x1, int x2, int y, int largura, Random random, bool permitirEscombros)
    {
        var metade = largura / 2;
        for (var x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            for (var oy = -metade; oy < largura - metade; oy++)
                mapa[x, y + oy] = EscolherCelulaDoCorredor(oy, random, permitirEscombros);
    }

    private static void EscavarVertical(MapaDaMasmorra mapa, int y1, int y2, int x, int largura, Random random, bool permitirEscombros)
    {
        var metade = largura / 2;
        for (var y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            for (var ox = -metade; ox < largura - metade; ox++)
                mapa[x + ox, y] = EscolherCelulaDoCorredor(ox, random, permitirEscombros);
    }

    private static TipoDeCelula EscolherCelulaDoCorredor(int offsetLateral, Random random, bool permitirEscombros)
    {
        // a linha central do corredor (offset 0) nunca vira obstáculo, garantindo que o mapa continue sempre conectado
        if (!permitirEscombros || offsetLateral == 0 || random.NextDouble() > 0.18)
            return TipoDeCelula.Chao;

        return random.NextDouble() < 0.5 ? TipoDeCelula.Entulho : TipoDeCelula.Pedra;
    }
}
