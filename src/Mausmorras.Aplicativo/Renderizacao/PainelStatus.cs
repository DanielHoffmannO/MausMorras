using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelStatus : PainelDeEstado
{
    public PainelStatus(Func<EstadoDoJogo> obterEstado) : base(obterEstado)
    {
        Width = Dim.Fill();
        Height = 1;
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        var jogador = estado.Jogador;
        var x = EscreverSegmento(0, $" Andar {estado.Andar}  ", Cores.TextoPrincipal);

        var corVida = estado.Morto ? Cores.Perigo : CorDaVida((double)jogador.Vida / jogador.VidaMaxima);
        var textoVida = estado.Morto ? $"Vida: 0/{jogador.VidaMaxima} (MORTO)" : $"Vida: {jogador.Vida}/{jogador.VidaMaxima}";
        x = EscreverSegmento(x, textoVida, corVida);

        x = EscreverSegmento(x, $"  Ouro: {jogador.Ouro}", Cores.Ouro);
        EscreverSegmento(x, "  —  I inventário, M minimapa, F5/F9 salvar ", Cores.TextoSecundario);
    }

    private int EscreverSegmento(int x, string texto, Color cor)
    {
        SetAttribute(new Attribute(cor, Cores.Fundo));
        AddStr(x, 0, texto);
        return x + texto.Length;
    }

    private static Color CorDaVida(double percentual) => percentual switch
    {
        >= 0.6 => Cores.VidaAlta,
        >= 0.3 => Cores.VidaMedia,
        _ => Cores.VidaBaixa
    };
}
