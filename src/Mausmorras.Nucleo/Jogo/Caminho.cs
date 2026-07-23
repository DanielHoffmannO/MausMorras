using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public static class Caminho
{
    private static readonly Posicao[] Direcoes = { Direcao.Norte, Direcao.Sul, Direcao.Leste, Direcao.Oeste };

    public static Posicao? ProximoPasso(MapaDaMasmorra mapa, Posicao origem, Func<Posicao, bool> ehDestino)
    {
        if (ehDestino(origem))
            return null;

        var primeiroPasso = new Dictionary<Posicao, Posicao>();
        var fila = new Queue<Posicao>();
        fila.Enqueue(origem);

        while (fila.Count > 0)
        {
            var atual = fila.Dequeue();

            foreach (var d in Direcoes)
            {
                var vizinho = atual + d;
                if (!mapa.EhCaminhavel(vizinho) || primeiroPasso.ContainsKey(vizinho) || vizinho == origem)
                    continue;

                primeiroPasso[vizinho] = atual == origem ? vizinho : primeiroPasso[atual];

                if (ehDestino(vizinho))
                    return primeiroPasso[vizinho];

                fila.Enqueue(vizinho);
            }
        }

        return null; // nenhum caminho encontrado (ex: nenhuma casa existe ainda)
    }
}
