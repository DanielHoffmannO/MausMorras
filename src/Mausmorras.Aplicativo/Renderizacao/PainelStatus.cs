using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelStatus : PainelDeEstado
{
    private const int LarguraDaBarra = 14;
    private const double LimiarVidaCritica = 0.3; // mesmo espirito do limiar interno de "medo de morrer"

    public PainelStatus(Func<EstadoDoJogo> obterEstado) : base(obterEstado)
    {
        Width = Dim.Fill();
        Height = 3;
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        if (estado.JogoEncerrado)
        {
            DesenharFimDeJogo(estado);
            return;
        }

        if (estado.SoRestamCriancas)
        {
            DesenharSoRestamCriancas(estado);
            return;
        }

        if (estado.Morto)
        {
            DesenharBannerDeMorte();
            return;
        }

        var personagem = estado.Personagem;

        var textoLocal = estado.LocalAtual == TipoDeLocal.Vila ? " Vila  " : $" Andar {estado.Andar}  ";
        var x = EscreverSegmento(0, 0, textoLocal, Cores.TextoPrincipal);

        var textoModo = estado.Modo == ModoDeJogo.Jogando ? "[Jogando]  " : "[Observador]  ";
        var corModo = estado.Modo == ModoDeJogo.Jogando ? Cores.TextoPrincipal : Cores.VidaMedia;
        x = EscreverSegmento(x, 0, textoModo, corModo);

        if (estado.Personagens.Count(p => p.Vida > 0) > 1)
            x = EscreverSegmento(x, 0, $"{personagem.Nome} ({estado.IndiceSelecionado + 1}/{estado.Personagens.Count})  ", Cores.TextoPrincipal);

        if (estado.LocalAtual == TipoDeLocal.Vila)
        {
            var corPeriodo = estado.EhDia ? Cores.TextoPrincipal : Cores.TextoSecundario;
            EscreverSegmento(x, 0, estado.EhDia ? $"Dia {estado.Dia}" : $"Noite {estado.Dia}", corPeriodo);
        }

        var vidaPercentual = (double)personagem.Vida / personagem.VidaMaxima;
        var corVida = CorDaVida(vidaPercentual);
        var alertaVida = vidaPercentual <= LimiarVidaCritica ? "⚠ " : "";
        var linha1 = 0;
        linha1 = EscreverSegmento(linha1, 1, $" {alertaVida}Vida  {DesenhoDeCaixa.DesenharBarra(personagem.Vida, personagem.VidaMaxima, LarguraDaBarra)} {personagem.Vida}/{personagem.VidaMaxima}   ", corVida);
        linha1 = EscreverSegmento(linha1, 1, $"Fome  {DesenhoDeCaixa.DesenharBarra(personagem.Fome, EstadoDoJogo.FomeMaxima, LarguraDaBarra)} {personagem.Fome}   ", CorDaNecessidade(personagem.Fome, EstadoDoJogo.FomeMaxima));
        EscreverSegmento(linha1, 1, $"Sono  {DesenhoDeCaixa.DesenharBarra(personagem.Sono, EstadoDoJogo.SonoMaximo, LarguraDaBarra)} {personagem.Sono}", CorDaNecessidade(personagem.Sono, EstadoDoJogo.SonoMaximo));

        var linha2 = EscreverSegmento(0, 2, $" Temp: {personagem.Temperatura}°   ", CorDaTemperatura(personagem.Temperatura));
        linha2 = EscreverSegmento(linha2, 2, $"Ouro: {estado.Ouro}   ", Cores.Ouro);
        EscreverSegmento(linha2, 2, $"Madeira: {estado.Madeira}   —  I inv, M mapa, C casa, F fogueira, P planta, Tab troca pessoa, Espaço modo, F5/F9 salvar", Cores.TextoSecundario);
    }

    private void DesenharBannerDeMorte()
    {
        EscreverCentralizado(1, "☠  VOCÊ MORREU  ☠", Cores.Perigo);
    }

    private void DesenharFimDeJogo(EstadoDoJogo estado)
    {
        var diaTexto = estado.Dia == 1 ? "1 dia" : $"{estado.Dia} dias";
        var turnoTexto = estado.Turno == 1 ? "1 turno" : $"{estado.Turno} turnos";
        var pessoaTexto = estado.PopulacaoTotal == 1 ? "1 pessoa" : $"{estado.PopulacaoTotal} pessoas";

        EscreverCentralizado(0, "☠  FIM DE JOGO — a vila foi extinta  ☠", Cores.Perigo);
        EscreverCentralizado(1, $"Sobreviveu {diaTexto}, {turnoTexto} — população total: {pessoaTexto}", Cores.TextoSecundario);
        EscreverCentralizado(2, "Pressione R para começar um novo jogo", Cores.TextoPrincipal);
    }

    private void DesenharSoRestamCriancas(EstadoDoJogo estado)
    {
        var criancas = estado.Personagens.Count(p => p.Vida > 0 && p.EhCrianca);
        var criancaTexto = criancas == 1 ? "1 criança ainda vive" : $"{criancas} crianças ainda vivem";

        EscreverCentralizado(0, "Todos os adultos morreram", Cores.Perigo);
        EscreverCentralizado(1, $"{criancaTexto} — mude pro Modo Observador (Espaço) e espere crescerem", Cores.TextoSecundario);
        EscreverCentralizado(2, "...ou pressione R para começar um novo jogo", Cores.TextoPrincipal);
    }

    private void EscreverCentralizado(int y, string texto, Color cor)
    {
        var x = Math.Max(0, (Frame.Width - texto.Length) / 2);
        SetAttribute(new Attribute(cor, Cores.Fundo));
        AddStr(x, y, texto);
    }

    private int EscreverSegmento(int x, int y, string texto, Color cor)
    {
        SetAttribute(new Attribute(cor, Cores.Fundo));
        AddStr(x, y, texto);
        return x + texto.Length;
    }

    private static Color CorDaVida(double percentual) => percentual switch
    {
        <= LimiarVidaCritica => Cores.Perigo,
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
