using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Entidades;

public sealed class Personagem
{
    public Posicao Posicao { get; set; }
    public int VidaMaxima { get; }
    public int Vida { get; set; }
    public int Ouro { get; set; }
    public int Fome { get; set; }
    public int Temperatura { get; set; } = 33; // graus; 33 = ideal (ver EstadoDoJogo.TemperaturaIdeal)
    public int Sono { get; set; }
    public bool EhCrianca { get; set; }
    public int Idade { get; set; } // em turnos; so relevante enquanto EhCrianca
    public string? ObjetivoAtual { get; set; } // rotulo da necessidade que a IA autonoma esta perseguindo agora (ver EstadoDoJogo.PensarPersonagensAutonomos)
    public string? DesejoAtual { get; set; } // vontade cosmetica do momento (ver EstadoDoJogo.ExpressarDesejo), so preenche tempo ocioso
    public bool EstaComMedo { get; set; } // vida criticamente baixa -- rastreado so pra disparar a fala de medo uma unica vez, na transicao
    public TracoDePersonalidade Traco { get; set; } = TracoDePersonalidade.Equilibrada;
    public double AversaoAoFrio { get; set; }
    public double AversaoAFome { get; set; }
    public double AversaoAoSono { get; set; }

    public Item? Capacete { get; set; }
    public Item? Peitoral { get; set; }
    public Item? Pernas { get; set; }
    public Item? Botas { get; set; }
    public List<Item> Mochila { get; } = new();

    public int DefesaTotal =>
        (Capacete?.Valor ?? 0) + (Peitoral?.Valor ?? 0) + (Pernas?.Valor ?? 0) + (Botas?.Valor ?? 0);

    public Personagem(Posicao posicaoInicial, int vidaMaxima = 20)
    {
        Posicao = posicaoInicial;
        VidaMaxima = vidaMaxima;
        Vida = vidaMaxima;
        Ouro = 0;
    }

    public Item? ObterEquipado(TipoDeItem tipo) => tipo switch
    {
        TipoDeItem.Capacete => Capacete,
        TipoDeItem.Peitoral => Peitoral,
        TipoDeItem.Pernas => Pernas,
        TipoDeItem.Botas => Botas,
        _ => null
    };

    public void Equipar(TipoDeItem tipo, Item? item)
    {
        switch (tipo)
        {
            case TipoDeItem.Capacete: Capacete = item; break;
            case TipoDeItem.Peitoral: Peitoral = item; break;
            case TipoDeItem.Pernas: Pernas = item; break;
            case TipoDeItem.Botas: Botas = item; break;
        }
    }
}
