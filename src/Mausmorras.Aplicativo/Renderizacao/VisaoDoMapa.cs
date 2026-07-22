using System.Text;
using Mausmorras.Nucleo.Jogo;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class VisaoDoMapa : View
{
    private const double IntensidadeEscurecimento = 0.55;
    private static readonly TimeSpan JanelaDeCliqueDuplo = TimeSpan.FromMilliseconds(400);

    private readonly string _caminhoDoSave;
    private EstadoDoJogo _estado;
    private bool _previaAtiva;
    private DateTime _ultimoCliqueDoC;

    public Action? AoAtualizar { get; set; }
    public Action? AoAbrirInventario { get; set; }
    public Action? AoAlternarMiniMapa { get; set; }

    public EstadoDoJogo Estado => _estado;

    public VisaoDoMapa(EstadoDoJogo estado, string caminhoDoSave)
    {
        _estado = estado;
        _caminhoDoSave = caminhoDoSave;
        CanFocus = true;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    private static (Rune Glifo, Color CorFrente) ObterVisualDaCelula(TipoDeCelula celula) => celula switch
    {
        TipoDeCelula.Parede => (new Rune('#'), Cores.Parede),
        TipoDeCelula.ParedeDecorada => (new Rune('%'), Cores.ParedeDecorada),
        TipoDeCelula.Chao => (new Rune('.'), Cores.Chao),
        TipoDeCelula.Porta => (new Rune('+'), Cores.Porta),
        TipoDeCelula.Escada => (new Rune('>'), Cores.Escada),
        TipoDeCelula.Grama => (new Rune(','), Cores.Grama),
        TipoDeCelula.Agua => (new Rune('~'), Cores.Agua),
        TipoDeCelula.Entulho => (new Rune(':'), Cores.Entulho),
        TipoDeCelula.Ouro => (new Rune('$'), Cores.Ouro),
        TipoDeCelula.Item => (new Rune('!'), Cores.Item),
        TipoDeCelula.Terra => (new Rune('"'), Cores.Terra),
        TipoDeCelula.Pedra => (new Rune('o'), Cores.Pedra),
        TipoDeCelula.Casa => (new Rune('⌂'), Cores.Casa),
        TipoDeCelula.Arvore => (new Rune('♣'), Cores.Arvore),
        TipoDeCelula.EntradaMasmorra => (new Rune('▼'), Cores.EntradaMasmorra),
        TipoDeCelula.SaidaParaVila => (new Rune('▲'), Cores.SaidaParaVila),
        _ => (new Rune('?'), Cores.Perigo)
    };

    protected override bool OnDrawingContent(DrawContext context)
    {
        ClearViewport(context);

        var mapa = _estado.Mapa;
        var jogador = _estado.Jogador.Posicao;
        var viewport = Viewport;

        var camX = Math.Clamp(jogador.X - viewport.Width / 2, 0, Math.Max(0, mapa.Largura - viewport.Width));
        var camY = Math.Clamp(jogador.Y - viewport.Height / 2, 0, Math.Max(0, mapa.Altura - viewport.Height));

        for (var telaY = 0; telaY < viewport.Height; telaY++)
        {
            var mapaY = camY + telaY;
            if (mapaY >= mapa.Altura)
                break;

            for (var telaX = 0; telaX < viewport.Width; telaX++)
            {
                var mapaX = camX + telaX;
                if (mapaX >= mapa.Largura)
                    break;

                if (!mapa.FoiExplorada(mapaX, mapaY))
                    continue;

                var (glifo, corFrente) = ObterVisualDaCelula(mapa[mapaX, mapaY]);
                var visivel = _estado.TodosVisiveis || _estado.CelulasVisiveis.Contains(new Posicao(mapaX, mapaY));

                SetAttribute(new Attribute(visivel ? corFrente : corFrente.GetDimmerColor(IntensidadeEscurecimento), Cores.Fundo));
                AddRune(telaX, telaY, glifo);
            }
        }

        if (_estado.LocalAtual == TipoDeLocal.Vila && _previaAtiva)
            DesenharPreviaDeConstrucao(mapa, camX, camY, viewport);

        SetAttribute(new Attribute(Cores.Jogador, Cores.Fundo));
        AddRune(jogador.X - camX, jogador.Y - camY, new Rune('@'));

        return true;
    }

    private void DesenharPreviaDeConstrucao(MapaDaMasmorra mapa, int camX, int camY, System.Drawing.Rectangle viewport)
    {
        var (area, portaExterna, valida) = _estado.ObterPreviaDeConstrucao();
        var corFundoPrevia = valida ? Cores.PreviaValida : Cores.PreviaInvalida;

        DesenharCelulaComPrevia(mapa, portaExterna.X, portaExterna.Y, camX, camY, viewport, corFundoPrevia);

        for (var x = area.X; x < area.X + area.Largura; x++)
            for (var y = area.Y; y < area.Y + area.Altura; y++)
                DesenharCelulaComPrevia(mapa, x, y, camX, camY, viewport, corFundoPrevia);
    }

    private void DesenharCelulaComPrevia(MapaDaMasmorra mapa, int x, int y, int camX, int camY, System.Drawing.Rectangle viewport, Color corFundoPrevia)
    {
        var telaX = x - camX;
        var telaY = y - camY;

        if (telaX < 0 || telaX >= viewport.Width || telaY < 0 || telaY >= viewport.Height || !mapa.FoiExplorada(x, y))
            return;

        var (glifo, corFrente) = ObterVisualDaCelula(mapa[x, y]);
        SetAttribute(new Attribute(corFrente, corFundoPrevia));
        AddRune(telaX, telaY, glifo);
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key.KeyCode == KeyCode.Esc)
        {
            Application.RequestStop();
            return true;
        }

        if (key.KeyCode == KeyCode.F5)
        {
            _estado.Salvar(_caminhoDoSave);
            SetNeedsDraw();
            AoAtualizar?.Invoke();
            return true;
        }

        if (key.KeyCode == KeyCode.F9)
        {
            if (File.Exists(_caminhoDoSave))
            {
                _estado = EstadoDoJogo.CarregarDe(_caminhoDoSave);
                SetNeedsDraw();
                AoAtualizar?.Invoke();
            }

            return true;
        }

        if (key.AsRune.Value is 'i' or 'I')
        {
            AoAbrirInventario?.Invoke();
            return true;
        }

        if (key.AsRune.Value is 'm' or 'M')
        {
            AoAlternarMiniMapa?.Invoke();
            return true;
        }

        if (key.AsRune.Value is 'c' or 'C')
        {
            AlternarOuConstruir();
            return true;
        }

        var moveu = key.KeyCode switch
        {
            KeyCode.CursorUp => _estado.TentarMoverJogador(Direcao.Norte),
            KeyCode.CursorDown => _estado.TentarMoverJogador(Direcao.Sul),
            KeyCode.CursorLeft => _estado.TentarMoverJogador(Direcao.Oeste),
            KeyCode.CursorRight => _estado.TentarMoverJogador(Direcao.Leste),
            _ => TratarTeclasDeLetra(key)
        };

        if (moveu)
        {
            SetNeedsDraw();
            AoAtualizar?.Invoke();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private void AlternarOuConstruir()
    {
        var agora = DateTime.UtcNow;
        var cliqueDuplo = _previaAtiva && agora - _ultimoCliqueDoC <= JanelaDeCliqueDuplo;

        if (cliqueDuplo)
        {
            _previaAtiva = false;
        }
        else if (_previaAtiva)
        {
            _estado.TentarConstruir();
            _previaAtiva = false;
        }
        else
        {
            _previaAtiva = true;
        }

        _ultimoCliqueDoC = agora;
        SetNeedsDraw();
        AoAtualizar?.Invoke();
    }

    private bool TratarTeclasDeLetra(Key key) => key.AsRune.Value switch
    {
        'w' or 'W' => _estado.TentarMoverJogador(Direcao.Norte),
        's' or 'S' => _estado.TentarMoverJogador(Direcao.Sul),
        'a' or 'A' => _estado.TentarMoverJogador(Direcao.Oeste),
        'd' or 'D' => _estado.TentarMoverJogador(Direcao.Leste),
        _ => false
    };
}
