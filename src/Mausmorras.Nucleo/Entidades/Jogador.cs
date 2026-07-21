using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Entidades;

public sealed class Jogador
{
    public Posicao Posicao { get; set; }

    public Jogador(Posicao posicaoInicial)
    {
        Posicao = posicaoInicial;
    }
}
