using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelMensagens : View
{
    private readonly Func<EstadoDoJogo> _obterEstado;

    public PainelMensagens(Func<EstadoDoJogo> obterEstado)
    {
        _obterEstado = obterEstado;
        Width = Dim.Fill();
        Height = 6;
    }

    protected override bool OnDrawingContent(DrawContext context)
    {
        ClearViewport(context);

        var viewport = Viewport;
        var mensagens = _obterEstado().Mensagens;
        var visiveis = mensagens.Skip(Math.Max(0, mensagens.Count - viewport.Height)).ToList();

        SetAttribute(new Attribute(new Color(ColorName16.Gray), new Color(ColorName16.Black)));
        for (var i = 0; i < visiveis.Count; i++)
            AddStr(0, i, visiveis[i]);

        return true;
    }
}
