using System.Text;
using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Jogo;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class MiniMapa : PainelDeEstado
{
    private const int LarguraInterna = 40;
    private const int AlturaInterna = 18;

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
        // uma escala SO (nao uma por eixo) -- mapas com proporcao diferente da caixa do minimapa
        // (vila e masmorra nao tem a mesma proporcao) ficariam esticados num eixo e comprimidos no
        // outro se cada eixo escalasse independente. usa a maior escala pra garantir que o mapa
        // inteiro cabe, e centraliza o resultado (letterbox) na caixa fixa
        var escala = Math.Max((double)mapa.Largura / LarguraInterna, (double)mapa.Altura / AlturaInterna);
        var larguraVisivel = (int)(mapa.Largura / escala);
        var alturaVisivel = (int)(mapa.Altura / escala);
        var deslocamentoX = (LarguraInterna - larguraVisivel) / 2;
        var deslocamentoY = (AlturaInterna - alturaVisivel) / 2;

        for (var my = 0; my < alturaVisivel; my++)
        {
            for (var mx = 0; mx < larguraVisivel; mx++)
            {
                var x = (int)(mx * escala);
                var y = (int)(my * escala);

                if (!mapa.FoiExplorada(x, y))
                    continue;

                var (glifo, cor) = ObterVisual(mapa[x, y]);
                SetAttribute(new Attribute(cor, Cores.Fundo));
                AddRune(mx + 1 + deslocamentoX, my + 1 + deslocamentoY, glifo);
            }
        }

        foreach (var p in estado.PersonagensNoLocalAtual.Where(p => !ReferenceEquals(p, estado.Personagem)))
            DesenharMarcador(p, p.Vida <= 0 ? Cores.TextoSecundario : Cores.PersonagemVivo, escala, deslocamentoX, deslocamentoY);

        DesenharMarcador(estado.Personagem, estado.Personagem.Vida <= 0 ? Cores.TextoSecundario : Cores.Personagem, escala, deslocamentoX, deslocamentoY);
    }

    private void DesenharMarcador(Personagem personagem, Color cor, double escala, int deslocamentoX, int deslocamentoY)
    {
        var mx = Math.Clamp((int)(personagem.Posicao.X / escala) + deslocamentoX, 0, LarguraInterna - 1);
        var my = Math.Clamp((int)(personagem.Posicao.Y / escala) + deslocamentoY, 0, AlturaInterna - 1);

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
