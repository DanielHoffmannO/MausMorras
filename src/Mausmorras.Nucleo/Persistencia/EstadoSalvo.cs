using Mausmorras.Nucleo.Itens;

namespace Mausmorras.Nucleo.Persistencia;

public sealed class EstadoSalvo
{
    public int Largura { get; set; }
    public int Altura { get; set; }
    public int Andar { get; set; }
    public int JogadorX { get; set; }
    public int JogadorY { get; set; }
    public int VidaJogador { get; set; }
    public int VidaMaximaJogador { get; set; }
    public int OuroJogador { get; set; }
    public int[] Celulas { get; set; } = Array.Empty<int>();
    public bool[] Explorada { get; set; } = Array.Empty<bool>();
    public List<string> Mensagens { get; set; } = new();
    public List<ItemSalvo> Mochila { get; set; } = new();
    public ItemSalvo? Capacete { get; set; }
    public ItemSalvo? Peitoral { get; set; }
    public ItemSalvo? Pernas { get; set; }
    public ItemSalvo? Botas { get; set; }
    public List<ItemNoChaoSalvo> ItensNoChao { get; set; } = new();
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
