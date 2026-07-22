using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public abstract class PainelDeEstado : View
{
    protected readonly Func<EstadoDoJogo> _obterEstado;

    protected PainelDeEstado(Func<EstadoDoJogo> obterEstado)
    {
        _obterEstado = obterEstado;
    }

    protected override bool OnDrawingContent(DrawContext context)
    {
        ClearViewport(context);
        Desenhar(_obterEstado());
        return true;
    }

    protected abstract void Desenhar(EstadoDoJogo estado);
}
