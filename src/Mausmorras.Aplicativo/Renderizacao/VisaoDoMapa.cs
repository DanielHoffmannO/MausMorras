using System.Text;
using Mausmorras.Nucleo.Jogo;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class VisaoDoMapa : View
{
    private const double IntensidadeEscurecimento = 0.55;
    private static readonly TimeSpan JanelaDeCliqueDuplo = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan IntervaloTempoReal = TimeSpan.FromMilliseconds(400);

    private readonly string _caminhoDoSave;
    private EstadoDoJogo _estado;
    private bool _previaAtiva;
    private DateTime _ultimoCliqueDoC;
    private object? _timeoutTempoReal;

    private readonly Dictionary<KeyCode, Func<bool>> _acoesPorTecla;
    private readonly Dictionary<char, Func<bool>> _acoesPorLetra;

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

        _acoesPorTecla = new Dictionary<KeyCode, Func<bool>>
        {
            [KeyCode.Esc] = Sair,
            [KeyCode.F5] = Salvar,
            [KeyCode.F9] = Carregar,
            [KeyCode.Space] = AlternarModo,
            [KeyCode.Tab] = () => _estado.SelecionarProximoPersonagem(),
            [KeyCode.CursorUp] = () => _estado.TentarMoverPersonagem(Direcao.Norte),
            [KeyCode.CursorDown] = () => _estado.TentarMoverPersonagem(Direcao.Sul),
            [KeyCode.CursorLeft] = () => _estado.TentarMoverPersonagem(Direcao.Oeste),
            [KeyCode.CursorRight] = () => _estado.TentarMoverPersonagem(Direcao.Leste),
        };

        _acoesPorLetra = new Dictionary<char, Func<bool>>
        {
            ['i'] = AbrirInventario,
            ['m'] = AlternarMiniMapa,
            ['c'] = AlternarOuConstruir,
            ['w'] = () => _estado.TentarMoverPersonagem(Direcao.Norte),
            ['s'] = () => _estado.TentarMoverPersonagem(Direcao.Sul),
            ['a'] = () => _estado.TentarMoverPersonagem(Direcao.Oeste),
            ['d'] = () => _estado.TentarMoverPersonagem(Direcao.Leste),
        };
    }

    private static readonly Dictionary<TipoDeCelula, (Rune Glifo, Color CorFrente)> VisualPorCelula = new()
    {
        [TipoDeCelula.Parede] = (new Rune('#'), Cores.Parede),
        [TipoDeCelula.ParedeDecorada] = (new Rune('%'), Cores.ParedeDecorada),
        [TipoDeCelula.Chao] = (new Rune('.'), Cores.Chao),
        [TipoDeCelula.Porta] = (new Rune('+'), Cores.Porta),
        [TipoDeCelula.Escada] = (new Rune('>'), Cores.Escada),
        [TipoDeCelula.Grama] = (new Rune(','), Cores.Grama),
        [TipoDeCelula.Agua] = (new Rune('~'), Cores.Agua),
        [TipoDeCelula.Entulho] = (new Rune(':'), Cores.Entulho),
        [TipoDeCelula.Ouro] = (new Rune('$'), Cores.Ouro),
        [TipoDeCelula.Item] = (new Rune('!'), Cores.Item),
        [TipoDeCelula.Terra] = (new Rune('"'), Cores.Terra),
        [TipoDeCelula.Pedra] = (new Rune('o'), Cores.Pedra),
        [TipoDeCelula.Casa] = (new Rune('⌂'), Cores.Casa),
        [TipoDeCelula.Arvore] = (new Rune('♣'), Cores.Arvore),
        [TipoDeCelula.EntradaMasmorra] = (new Rune('▼'), Cores.EntradaMasmorra),
        [TipoDeCelula.SaidaParaVila] = (new Rune('▲'), Cores.SaidaParaVila),
    };

    private static (Rune Glifo, Color CorFrente) ObterVisualDaCelula(TipoDeCelula celula) =>
        VisualPorCelula.TryGetValue(celula, out var visual) ? visual : (new Rune('?'), Cores.Perigo);

    protected override bool OnDrawingContent(DrawContext context)
    {
        ClearViewport(context);

        var mapa = _estado.Mapa;
        var personagem = _estado.Personagem.Posicao;
        var viewport = Viewport;

        var camX = Math.Clamp(personagem.X - viewport.Width / 2, 0, Math.Max(0, mapa.Largura - viewport.Width));
        var camY = Math.Clamp(personagem.Y - viewport.Height / 2, 0, Math.Max(0, mapa.Altura - viewport.Height));

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

        foreach (var p in _estado.PersonagensNoLocalAtual)
        {
            var tx = p.Posicao.X - camX;
            var ty = p.Posicao.Y - camY;
            if (tx < 0 || tx >= viewport.Width || ty < 0 || ty >= viewport.Height)
                continue;

            var cor = ReferenceEquals(p, _estado.Personagem) ? Cores.Personagem : Cores.TextoSecundario;
            SetAttribute(new Attribute(cor, Cores.Fundo));
            AddRune(tx, ty, new Rune('@'));
        }

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
        if (_acoesPorTecla.TryGetValue(key.KeyCode, out var acaoPorTecla))
            return Executar(acaoPorTecla);

        var letra = char.ToLowerInvariant((char)key.AsRune.Value);
        if (_acoesPorLetra.TryGetValue(letra, out var acaoPorLetra))
            return Executar(acaoPorLetra);

        return base.OnKeyDown(key);
    }

    private bool Executar(Func<bool> acao)
    {
        if (acao())
        {
            SetNeedsDraw();
            AoAtualizar?.Invoke();
        }

        return true;
    }

    private bool Sair()
    {
        Application.RequestStop();
        return false;
    }

    private bool Salvar()
    {
        _estado.Salvar(_caminhoDoSave);
        return true;
    }

    private bool Carregar()
    {
        if (!File.Exists(_caminhoDoSave))
            return false;

        _estado = EstadoDoJogo.CarregarDe(_caminhoDoSave);
        if (_estado.Modo == ModoDeJogo.Observador) IniciarTempoReal(); else PararTempoReal();
        return true;
    }

    private bool AlternarModo()
    {
        _estado.AlternarModo();
        if (_estado.Modo == ModoDeJogo.Observador) IniciarTempoReal(); else PararTempoReal();
        return true;
    }

    private bool AbrirInventario()
    {
        AoAbrirInventario?.Invoke();
        return false;
    }

    private bool AlternarMiniMapa()
    {
        AoAlternarMiniMapa?.Invoke();
        return false;
    }

    private void IniciarTempoReal()
    {
        if (_timeoutTempoReal is not null)
            return;

        _timeoutTempoReal = Application.AddTimeout(IntervaloTempoReal, () =>
        {
            _estado.AvancarTurno();
            SetNeedsDraw();
            AoAtualizar?.Invoke();
            return _estado.Modo == ModoDeJogo.Observador;
        });
    }

    private void PararTempoReal()
    {
        if (_timeoutTempoReal is null)
            return;

        Application.RemoveTimeout(_timeoutTempoReal);
        _timeoutTempoReal = null;
    }

    private bool AlternarOuConstruir()
    {
        if (_estado.Modo != ModoDeJogo.Jogando)
            return false;

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
        return true;
    }
}
