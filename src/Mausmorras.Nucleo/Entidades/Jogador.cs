using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Entidades;

public sealed class Jogador
{
    public Posicao Posicao { get; set; }
    public int VidaMaxima { get; }
    public int Vida { get; set; }
    public int Ouro { get; set; }

    public Jogador(Posicao posicaoInicial, int vidaMaxima = 20)
    {
        Posicao = posicaoInicial;
        VidaMaxima = vidaMaxima;
        Vida = vidaMaxima;
        Ouro = 0;
    }
}
