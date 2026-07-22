using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Geracao;

public sealed class GeradorDeVila
{
    private readonly double _densidadeDeArvores;

    public GeradorDeVila(double densidadeDeArvores = 0.05)
    {
        _densidadeDeArvores = densidadeDeArvores;
    }

    public (MapaDaMasmorra Mapa, IReadOnlyList<Sala> Salas, IReadOnlyDictionary<Posicao, Item> Itens) Gerar(int largura, int altura, Random random)
    {
        var mapa = new MapaDaMasmorra(largura, altura);

        for (var x = 1; x < largura - 1; x++)
            for (var y = 1; y < altura - 1; y++)
                mapa[x, y] = TipoDeCelula.Grama;

        var spawn = new Posicao(largura / 2, altura / 2);
        LimparPraca(mapa, spawn, 2);

        var entrada = new Posicao(spawn.X, Math.Min(altura - 2, spawn.Y + 10));
        LimparPraca(mapa, entrada, 1);
        mapa[entrada.X, entrada.Y] = TipoDeCelula.EntradaMasmorra;

        EspalharArvores(mapa, random);

        var salas = new List<Sala> { new(spawn.X, spawn.Y, 1, 1), new(entrada.X, entrada.Y, 1, 1) };

        ConectividadeDeMapa.GarantirAlcancavel(mapa, spawn, new[] { entrada }, random, larguraCorredor: 1);

        return (mapa, salas, new Dictionary<Posicao, Item>());
    }

    private static void LimparPraca(MapaDaMasmorra mapa, Posicao centro, int raio)
    {
        for (var x = centro.X - raio; x <= centro.X + raio; x++)
            for (var y = centro.Y - raio; y <= centro.Y + raio; y++)
                if (mapa.DentroDosLimites(x, y))
                    mapa[x, y] = TipoDeCelula.Chao;
    }

    private void EspalharArvores(MapaDaMasmorra mapa, Random random)
    {
        for (var x = 1; x < mapa.Largura - 1; x++)
            for (var y = 1; y < mapa.Altura - 1; y++)
                if (mapa[x, y] == TipoDeCelula.Grama && random.NextDouble() < _densidadeDeArvores)
                    mapa[x, y] = TipoDeCelula.Arvore;
    }
}
