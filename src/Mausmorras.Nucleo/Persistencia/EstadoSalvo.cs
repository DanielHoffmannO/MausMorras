using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Nucleo.Persistencia;

public sealed class EstadoSalvo
{
    public int Largura { get; set; }
    public int Altura { get; set; }
    public int Andar { get; set; }

    // Nomes de campo prefixados com "Jogador" são mantidos por compatibilidade com saves
    // já existentes (~/.mausmorras_save.json) — renomear quebraria o casamento por nome do
    // System.Text.Json sem nenhum ganho, mesmo depois do tipo em memória virar Personagem.
    public int JogadorX { get; set; }
    public int JogadorY { get; set; }
    public int VidaJogador { get; set; }
    public int VidaMaximaJogador { get; set; }
    public int OuroJogador { get; set; }
    public int MadeiraJogador { get; set; }
    public int Turno { get; set; }
    public ModoDeJogo Modo { get; set; } = ModoDeJogo.Jogando;
    public List<PersonagemSalvo> Personagens { get; set; } = new();
    public int IndiceSelecionado { get; set; }
    public int[] Celulas { get; set; } = Array.Empty<int>();
    public bool[] Explorada { get; set; } = Array.Empty<bool>();
    public List<string> Mensagens { get; set; } = new();
    public List<ItemSalvo> Mochila { get; set; } = new();
    public ItemSalvo? Capacete { get; set; }
    public ItemSalvo? Peitoral { get; set; }
    public ItemSalvo? Pernas { get; set; }
    public ItemSalvo? Botas { get; set; }
    public List<ItemNoChaoSalvo> ItensNoChao { get; set; } = new();
    public List<BichoSalvo> Bichos { get; set; } = new();
}

public sealed class PersonagemSalvo
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Vida { get; set; }
    public int VidaMaxima { get; set; }
    public int Ouro { get; set; }
    public int Madeira { get; set; }
    public int Fome { get; set; }
    public int Frio { get; set; }
    public List<ItemSalvo> Mochila { get; set; } = new();
    public ItemSalvo? Capacete { get; set; }
    public ItemSalvo? Peitoral { get; set; }
    public ItemSalvo? Pernas { get; set; }
    public ItemSalvo? Botas { get; set; }
}

public sealed class BichoSalvo
{
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class ItemSalvo
{
    public string Nome { get; set; } = "";
    public TipoDeItem Tipo { get; set; }
    public int Valor { get; set; }
}

public sealed class ItemNoChaoSalvo
{
    public int X { get; set; }
    public int Y { get; set; }
    public ItemSalvo Item { get; set; } = new();
}
