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
        var personagem = estado.Personagem;
        var textoLocal = estado.LocalAtual == TipoDeLocal.Vila ? " Vila  " : $" Andar {estado.Andar}  ";
        var x = EscreverSegmento(0, textoLocal, Cores.TextoPrincipal);

        var textoModo = estado.Modo == ModoDeJogo.Jogando ? "[Jogando]  " : "[Observador]  ";
        var corModo = estado.Modo == ModoDeJogo.Jogando ? Cores.TextoPrincipal : Cores.VidaMedia;
        x = EscreverSegmento(x, textoModo, corModo);

        if (estado.Personagens.Count(p => p.Vida > 0) > 1)
            x = EscreverSegmento(x, $"Pessoa {estado.IndiceSelecionado + 1}/{estado.Personagens.Count}  ", Cores.TextoPrincipal);

        var corVida = estado.Morto ? Cores.Perigo : CorDaVida((double)personagem.Vida / personagem.VidaMaxima);
        var textoVida = estado.Morto ? $"Vida: 0/{personagem.VidaMaxima} (MORTO)" : $"Vida: {personagem.Vida}/{personagem.VidaMaxima}";
        x = EscreverSegmento(x, textoVida, corVida);

        if (estado.LocalAtual == TipoDeLocal.Vila)
        {
            var corPeriodo = estado.EhDia ? Cores.TextoPrincipal : Cores.TextoSecundario;
            x = EscreverSegmento(x, estado.EhDia ? "  Dia" : "  Noite", corPeriodo);
        }

        x = EscreverSegmento(x, $"  Ouro: {personagem.Ouro}", Cores.Ouro);
        x = EscreverSegmento(x, $"  Madeira: {personagem.Madeira}", Cores.Casa);
        x = EscreverSegmento(x, $"  Fome: {personagem.Fome}", CorDaNecessidade(personagem.Fome, EstadoDoJogo.FomeMaxima));
        x = EscreverSegmento(x, $"  Temp: {personagem.Temperatura}°", CorDaTemperatura(personagem.Temperatura));
        x = EscreverSegmento(x, $"  Sono: {personagem.Sono}", CorDaNecessidade(personagem.Sono, EstadoDoJogo.SonoMaximo));
        EscreverSegmento(x, "  —  I inv, M mapa, C casa, F fogueira, Tab troca pessoa, Espaço modo, F5/F9 salvar ", Cores.TextoSecundario);
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

    private static Color CorDaNecessidade(int valor, int maximo) => ((double)valor / maximo) switch
    {
        >= 0.85 => Cores.VidaBaixa,
        >= 0.5 => Cores.VidaMedia,
        _ => Cores.TextoSecundario
    };

    private static Color CorDaTemperatura(int temperatura) => temperatura switch
    {
        <= EstadoDoJogo.TemperaturaCritica => Cores.VidaBaixa,
        <= EstadoDoJogo.TemperaturaCritica + 5 => Cores.VidaMedia,
        _ => Cores.TextoSecundario
    };
}
