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
}
