using System.Text;
using Mausmorras.Nucleo.Jogo;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class MiniMapa : View
{
    private const int LarguraInterna = 44;
    private const int AlturaInterna = 22;

    public const int LarguraTotal = LarguraInterna + 2;
    public const int AlturaTotal = AlturaInterna + 2;

    private readonly Func<EstadoDoJogo> _obterEstado;

    public MiniMapa(Func<EstadoDoJogo> obterEstado)
    {
        _obterEstado = obterEstado;
        Width = LarguraTotal;
        Height = AlturaTotal;
    }

    protected override bool OnDrawingContent(DrawContext context)
    {
        ClearViewport(context);
        DesenharMoldura();

        var estado = _obterEstado();
        var mapa = estado.Mapa;

        var escalaX = (double)mapa.Largura / LarguraInterna;
        var escalaY = (double)mapa.Altura / AlturaInterna;

        for (var my = 0; my < AlturaInterna; my++)
        {
            for (var mx = 0; mx < LarguraInterna; mx++)
            {
                var x = (int)(mx * escalaX);
                var y = (int)(my * escalaY);

                if (!mapa.FoiExplorada(x, y))
                    continue;

                var (glifo, cor) = ObterVisual(mapa[x, y]);
                SetAttribute(new Attribute(cor, new Color(ColorName16.Black)));
                AddRune(mx + 1, my + 1, glifo);
            }
        }

        var jogadorMx = Math.Clamp((int)(estado.Jogador.Posicao.X / escalaX), 0, LarguraInterna - 1);
        var jogadorMy = Math.Clamp((int)(estado.Jogador.Posicao.Y / escalaY), 0, AlturaInterna - 1);

        SetAttribute(new Attribute(new Color(ColorName16.BrightYellow), new Color(ColorName16.Black)));
        AddRune(jogadorMx + 1, jogadorMy + 1, new Rune('@'));

        return true;
    }

    private void DesenharMoldura()
    {
        SetAttribute(new Attribute(new Color(ColorName16.Gray), new Color(ColorName16.Black)));

        AddRune(0, 0, new Rune('┌'));
        AddRune(LarguraInterna + 1, 0, new Rune('┐'));
        AddRune(0, AlturaInterna + 1, new Rune('└'));
        AddRune(LarguraInterna + 1, AlturaInterna + 1, new Rune('┘'));

        for (var x = 1; x <= LarguraInterna; x++)
        {
            AddRune(x, 0, new Rune('─'));
            AddRune(x, AlturaInterna + 1, new Rune('─'));
        }

        for (var y = 1; y <= AlturaInterna; y++)
        {
            AddRune(0, y, new Rune('│'));
            AddRune(LarguraInterna + 1, y, new Rune('│'));
        }
    }

    private static (Rune Glifo, Color Cor) ObterVisual(TipoDeCelula celula) => celula switch
    {
        TipoDeCelula.Parede or TipoDeCelula.ParedeDecorada => (new Rune(' '), new Color(ColorName16.Black)),
        TipoDeCelula.Escada => (new Rune('>'), new Color(ColorName16.BrightCyan)),
        TipoDeCelula.Ouro => (new Rune('$'), new Color(ColorName16.BrightYellow)),
        _ => (new Rune('·'), new Color(ColorName16.DarkGray))
    };
}
