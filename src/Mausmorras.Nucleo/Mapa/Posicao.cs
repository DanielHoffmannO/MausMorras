namespace Mausmorras.Nucleo.Mapa;

public readonly record struct Posicao(int X, int Y)
{
    public static Posicao operator +(Posicao a, Posicao b) => new(a.X + b.X, a.Y + b.Y);
}
