using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Entidades;

public sealed class Monstro
{
    public Posicao Posicao { get; set; }
    public int Vida { get; set; }
    public int VidaMaxima { get; }
    public int Dano { get; }
    public TipoDeMonstro Tipo { get; }

    public Monstro(Posicao posicaoInicial, int vidaMaxima, int dano, TipoDeMonstro tipo = TipoDeMonstro.Comum)
    {
        Posicao = posicaoInicial;
        VidaMaxima = vidaMaxima;
        Vida = vidaMaxima;
        Dano = dano;
        Tipo = tipo;
    }
}
