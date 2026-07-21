using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public static class CampoDeVisao
{
    public static HashSet<Posicao> Calcular(MapaDaMasmorra mapa, Posicao origem, int raio)
    {
        var visiveis = new HashSet<Posicao> { origem };
        var raioAoQuadrado = raio * raio;

        for (var y = origem.Y - raio; y <= origem.Y + raio; y++)
        {
            for (var x = origem.X - raio; x <= origem.X + raio; x++)
            {
                if (!mapa.DentroDosLimites(x, y))
                    continue;

                var dx = x - origem.X;
                var dy = y - origem.Y;
                if (dx * dx + dy * dy > raioAoQuadrado)
                    continue;

                if (TemLinhaDeVisao(mapa, origem, new Posicao(x, y)))
                    visiveis.Add(new Posicao(x, y));
            }
        }

        return visiveis;
    }

    private static bool TemLinhaDeVisao(MapaDaMasmorra mapa, Posicao de, Posicao para)
    {
        var x0 = de.X;
        var y0 = de.Y;
        var x1 = para.X;
        var y1 = para.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var erro = dx - dy;

        var x = x0;
        var y = y0;

        while (true)
        {
            if ((x, y) != (x0, y0) && (x, y) != (x1, y1) && mapa.EhOpaca(x, y))
                return false;

            if (x == x1 && y == y1)
                return true;

            var e2 = 2 * erro;
            if (e2 > -dy)
            {
                erro -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                erro += dx;
                y += sy;
            }
        }
    }
}
