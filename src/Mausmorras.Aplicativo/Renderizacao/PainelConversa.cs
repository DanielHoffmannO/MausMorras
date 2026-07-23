using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelConversa : PainelDeEstado
{
    public PainelConversa(Func<EstadoDoJogo> obterEstado) : base(obterEstado)
    {
        Height = 6;
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        var viewport = Viewport;

        SetAttribute(new Attribute(Cores.TextoPrincipal, Cores.Fundo));
        AddStr(0, 0, "Conversa:");

        var conversas = estado.Conversas;
        var linhasDisponiveis = viewport.Height - 1;
        var visiveis = conversas.Skip(Math.Max(0, conversas.Count - linhasDisponiveis)).ToList();

        for (var i = 0; i < visiveis.Count; i++)
            AddStr(0, i + 1, visiveis[i]);
    }
}
