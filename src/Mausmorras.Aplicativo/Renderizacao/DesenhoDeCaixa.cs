using System.Text;

namespace Mausmorras.Aplicativo.Renderizacao;

internal static class DesenhoDeCaixa
{
    public static void DesenharMoldura(this View view, int larguraInterna, int alturaInterna, Color cor, string? titulo = null)
    {
        view.SetAttribute(new Attribute(cor, Cores.Fundo));

        view.AddRune(0, 0, new Rune('┌'));
        view.AddRune(larguraInterna + 1, 0, new Rune('┐'));
        view.AddRune(0, alturaInterna + 1, new Rune('└'));
        view.AddRune(larguraInterna + 1, alturaInterna + 1, new Rune('┘'));

        for (var x = 1; x <= larguraInterna; x++)
        {
            view.AddRune(x, 0, new Rune('─'));
            view.AddRune(x, alturaInterna + 1, new Rune('─'));
        }

        for (var y = 1; y <= alturaInterna; y++)
        {
            view.AddRune(0, y, new Rune('│'));
            view.AddRune(larguraInterna + 1, y, new Rune('│'));
        }

        if (titulo is not null)
        {
            var textoTitulo = $" {titulo} ";
            view.AddStr((larguraInterna + 2 - textoTitulo.Length) / 2, 0, textoTitulo);
        }
    }
}
