using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Entidades;

public sealed class Personagem
{
    public Posicao Posicao { get; set; }
    public int VidaMaxima { get; }
    public int Vida { get; set; }
    public int Fome { get; set; }
    public int Temperatura { get; set; } = 30; // graus; ver EstadoDoJogo.TemperaturaIdeal para o valor ideal
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
    public Bicho? AlvoDeCaca { get; set; } // bicho reservado pra evitar que outro personagem persiga o mesmo (ver EstadoDoJogo.EscolherBichoAlvo)
    public Posicao? AlvoDeColeta { get; set; } // posicao da celula de arvore (comum ou frutifera) reservada (ver EstadoDoJogo.EscolherArvoreAlvo)
    public int? TurnoDoAlvoDeCaca { get; set; } // ultimo turno em que AlvoDeCaca foi confirmado -- reservas nao reconfirmadas expiram (ver EstadoDoJogo.PensarPersonagensAutonomos)
    public int? TurnoDoAlvoDeColeta { get; set; } // ultimo turno em que AlvoDeColeta foi confirmado
    public Posicao? LocalDeCacaConhecido { get; set; } // posicao do ultimo sucesso de caca, nao o alvo atual -- usado como ultimo recurso quando a busca normal nao acha nada (ver EstadoDoJogo.EscolherBichoAlvo)
    public Posicao? LocalDeColetaConhecido { get; set; } // idem, pro ultimo sucesso de coleta de madeira/fruta
    public int? TurnoDeRetornoDaExpedicao { get; set; } // null = nao esta em expedicao autonoma a masmorra (ver EstadoDoJogo.IniciarExpedicaoAutonoma)
    public bool EstaEmExpedicao => TurnoDeRetornoDaExpedicao is not null;
    public double AversaoAExpedicao { get; set; } // aumenta se morre numa expedicao -- deixa o grupo mais relutante a mandar gente de novo

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
