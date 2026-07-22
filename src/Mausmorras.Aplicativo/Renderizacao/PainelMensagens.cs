using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelMensagens : PainelDeEstado
{
    public PainelMensagens(Func<EstadoDoJogo> obterEstado) : base(obterEstado)
    {
        Width = Dim.Fill();
        Height = 6;
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        var viewport = Viewport;
        var mensagens = estado.Mensagens;
        var visiveis = mensagens.Skip(Math.Max(0, mensagens.Count - viewport.Height)).ToList();

        SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
        for (var i = 0; i < visiveis.Count; i++)
            AddStr(0, i, visiveis[i]);
    }
}
