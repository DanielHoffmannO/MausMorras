using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Entidades;

public sealed class Bicho
{
    public Posicao Posicao { get; set; }

    public Bicho(Posicao posicaoInicial) => Posicao = posicaoInicial;
}
