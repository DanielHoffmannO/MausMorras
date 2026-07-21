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

        var estado = _obterEstado();
        var jogador = estado.Jogador;
        var corTexto = estado.Morto ? new Color(ColorName16.BrightRed) : new Color(ColorName16.White);

        SetAttribute(new Attribute(corTexto, new Color(ColorName16.Black)));

        var vida = estado.Morto ? $"Vida: 0/{jogador.VidaMaxima} (MORTO)" : $"Vida: {jogador.Vida}/{jogador.VidaMaxima}";
        AddStr(0, 0, $" Andar {estado.Andar}  {vida}  Ouro: {jogador.Ouro}  —  F5 salva, F9 carrega ");

        return true;
    }
}
