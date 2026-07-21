using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Geracao;

public readonly record struct Sala(int X, int Y, int Largura, int Altura)
{
    public Posicao Centro => new(X + Largura / 2, Y + Altura / 2);

    public bool Sobrepoe(Sala outra) =>
        X <= outra.X + outra.Largura &&
        X + Largura >= outra.X &&
        Y <= outra.Y + outra.Altura &&
        Y + Altura >= outra.Y;
}
