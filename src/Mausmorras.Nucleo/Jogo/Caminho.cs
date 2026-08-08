using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public static class Caminho
{
    private static readonly Posicao[] Direcoes = { Direcao.Norte, Direcao.Sul, Direcao.Leste, Direcao.Oeste };

    public static Posicao? ProximoPasso(MapaDaMasmorra mapa, Posicao origem, Func<Posicao, bool> ehDestino) =>
        ProximoPasso(mapa, origem, ehDestino, out _);

    // variante que tambem expoe QUAL celula satisfez o predicado (nao so o proximo passo ate ela) --
    // usada por quem precisa saber o alvo exato encontrado pra reserva-lo (ver EscolherBichoAlvo/
    // EscolherArvoreAlvo em EstadoDoJogo.cs), sem duplicar o BFS
    public static Posicao? ProximoPasso(MapaDaMasmorra mapa, Posicao origem, Func<Posicao, bool> ehDestino, out Posicao? destinoEncontrado)
    {
        if (ehDestino(origem))
        {
            destinoEncontrado = origem;
            return null;
        }

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
                {
                    destinoEncontrado = vizinho;
                    return primeiroPasso[vizinho];
                }

                fila.Enqueue(vizinho);
            }
        }

        destinoEncontrado = null;
        return null; // nenhum caminho encontrado (ex: nenhuma casa existe ainda)
    }
}
