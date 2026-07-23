using System.Text;
using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Jogo;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class MiniMapa : PainelDeEstado
{
    private const int LarguraInterna = 44;
    private const int AlturaInterna = 22;

    public const int LarguraTotal = LarguraInterna + 2;
    public const int AlturaTotal = AlturaInterna + 2;

    public MiniMapa(Func<EstadoDoJogo> obterEstado) : base(obterEstado)
    {
        Width = LarguraTotal;
        Height = AlturaTotal;
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        this.DesenharMoldura(LarguraInterna, AlturaInterna, Cores.TextoSecundario);

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
                SetAttribute(new Attribute(cor, Cores.Fundo));
                AddRune(mx + 1, my + 1, glifo);
            }
        }

        foreach (var p in estado.PersonagensNoLocalAtual.Where(p => !ReferenceEquals(p, estado.Personagem)))
            DesenharMarcador(p, Cores.TextoSecundario, escalaX, escalaY);

        DesenharMarcador(estado.Personagem, Cores.Personagem, escalaX, escalaY);
    }

    private void DesenharMarcador(Personagem personagem, Color cor, double escalaX, double escalaY)
    {
        var mx = Math.Clamp((int)(personagem.Posicao.X / escalaX), 0, LarguraInterna - 1);
        var my = Math.Clamp((int)(personagem.Posicao.Y / escalaY), 0, AlturaInterna - 1);

        SetAttribute(new Attribute(cor, Cores.Fundo));
        AddRune(mx + 1, my + 1, new Rune('@'));
    }

    private static readonly Dictionary<TipoDeCelula, (Rune Glifo, Color Cor)> VisualPorCelula = new()
    {
        [TipoDeCelula.Parede] = (new Rune(' '), Cores.Fundo),
        [TipoDeCelula.ParedeDecorada] = (new Rune(' '), Cores.Fundo),
        [TipoDeCelula.Escada] = (new Rune('>'), Cores.Escada),
        [TipoDeCelula.Ouro] = (new Rune('$'), Cores.Ouro),
        [TipoDeCelula.EntradaMasmorra] = (new Rune('▼'), Cores.EntradaMasmorra),
        [TipoDeCelula.SaidaParaVila] = (new Rune('▲'), Cores.SaidaParaVila),
    };

    private static (Rune Glifo, Color Cor) ObterVisual(TipoDeCelula celula) =>
        VisualPorCelula.TryGetValue(celula, out var visual) ? visual : (new Rune('·'), Cores.TextoSecundario);
}
