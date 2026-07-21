using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelStatus : View
{
    private readonly Func<EstadoDoJogo> _obterEstado;

    public PainelStatus(Func<EstadoDoJogo> obterEstado)
    {
        _obterEstado = obterEstado;
        Width = Dim.Fill();
        Height = 1;
    }

    protected override bool OnDrawingContent(DrawContext context)
    {
        ClearViewport(context);
        SetAttribute(new Attribute(new Color(ColorName16.White), new Color(ColorName16.Black)));
        AddStr(0, 0, $" Andar {_obterEstado().Andar}  —  F5 salva, F9 carrega ");
        return true;
    }
}
