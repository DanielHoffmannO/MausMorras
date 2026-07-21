using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public static class Direcao
{
    public static readonly Posicao Norte = new(0, -1);
    public static readonly Posicao Sul = new(0, 1);
    public static readonly Posicao Leste = new(1, 0);
    public static readonly Posicao Oeste = new(-1, 0);
}
