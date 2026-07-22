namespace Mausmorras.Nucleo.Mapa;

public sealed class MapaDaMasmorra
{
    private readonly TipoDeCelula[,] _celulas;
    private readonly bool[,] _explorada;

    public int Largura { get; }
    public int Altura { get; }

    public MapaDaMasmorra(int largura, int altura)
    {
        Largura = largura;
        Altura = altura;
        _celulas = new TipoDeCelula[largura, altura];
        _explorada = new bool[largura, altura];
        for (var x = 0; x < largura; x++)
            for (var y = 0; y < altura; y++)
                _celulas[x, y] = TipoDeCelula.Parede;
    }

    public TipoDeCelula this[int x, int y]
    {
        get => DentroDosLimites(x, y) ? _celulas[x, y] : TipoDeCelula.Parede;
        set
        {
            if (DentroDosLimites(x, y))
                _celulas[x, y] = value;
        }
    }

    public bool DentroDosLimites(int x, int y) => x >= 0 && x < Largura && y >= 0 && y < Altura;

    public bool EhCaminhavel(int x, int y) => DentroDosLimites(x, y) && this[x, y] switch
    {
        TipoDeCelula.Parede or TipoDeCelula.ParedeDecorada or TipoDeCelula.Pedra or TipoDeCelula.Casa or TipoDeCelula.Arvore => false,
        _ => true
    };

    public bool EhCaminhavel(Posicao p) => EhCaminhavel(p.X, p.Y);

    public bool EhOpaca(int x, int y) => !DentroDosLimites(x, y) || this[x, y] is TipoDeCelula.Parede or TipoDeCelula.ParedeDecorada or TipoDeCelula.Casa;

    public bool FoiExplorada(int x, int y) => DentroDosLimites(x, y) && _explorada[x, y];

    public void MarcarExplorada(int x, int y)
    {
        if (DentroDosLimites(x, y))
            _explorada[x, y] = true;
    }
}
