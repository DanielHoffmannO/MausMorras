using System.Text;
using System.Text.Json;
using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Jogo;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class VisaoDoMapa : View
{
    private const double IntensidadeEscurecimento = 0.55;
    private static readonly TimeSpan JanelaDeCliqueDuplo = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan IntervaloTempoReal = TimeSpan.FromMilliseconds(400);

    private readonly string _caminhoDoSave;
    private readonly int _larguraDoMundo;
    private readonly int _alturaDoMundo;
    private EstadoDoJogo _estado;
    private bool _previaAtiva;
    private DateTime _ultimoCliqueDoC;
    private bool _previaFogueiraAtiva;
    private DateTime _ultimoCliqueDoF;
    private bool _previaPlantioAtiva;
    private DateTime _ultimoCliqueDoP;
    private object? _timeoutTempoReal;

    private readonly Dictionary<KeyCode, Func<bool>> _acoesPorTecla;
    private readonly Dictionary<char, Func<bool>> _acoesPorLetra;

    public Action? AoAtualizar { get; set; }
    public Action? AoAbrirInventario { get; set; }
    public Action? AoAlternarMiniMapa { get; set; }

    public EstadoDoJogo Estado => _estado;

    public VisaoDoMapa(EstadoDoJogo estado, string caminhoDoSave, int larguraDoMundo, int alturaDoMundo)
    {
        _estado = estado;
        _caminhoDoSave = caminhoDoSave;
        _larguraDoMundo = larguraDoMundo;
        _alturaDoMundo = alturaDoMundo;
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
            ['f'] = AlternarOuConstruirFogueira,
            ['p'] = AlternarOuPlantar,
            ['r'] = Reiniciar,
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
        [TipoDeCelula.PisoDaCasa] = (new Rune('.'), Cores.Casa),
        [TipoDeCelula.Fogueira] = (new Rune('^'), Cores.Fogueira),
        [TipoDeCelula.ArvoreFrutifera] = (new Rune('♣'), Cores.ArvoreFrutifera),
        [TipoDeCelula.Plantacao] = (new Rune('✿'), Cores.Plantacao),
        [TipoDeCelula.Cama] = (new Rune('z'), Cores.Cama),
        [TipoDeCelula.Bau] = (new Rune('b'), Cores.Bau),
    };

    private static (Rune Glifo, Color CorFrente) ObterVisualDaCelula(TipoDeCelula celula) =>
        VisualPorCelula.TryGetValue(celula, out var visual) ? visual : (new Rune('?'), Cores.Perigo);

    private static Color CorDoMonstro(TipoDeMonstro tipo) => tipo switch
    {
        TipoDeMonstro.Resistente => Cores.MonstroResistente,
        TipoDeMonstro.Feroz => Cores.MonstroFeroz,
        _ => Cores.Monstro
    };

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

        if (_estado.LocalAtual == TipoDeLocal.Vila && _previaFogueiraAtiva)
            DesenharPreviaDeFogueira(mapa, camX, camY, viewport);

        if (_estado.LocalAtual == TipoDeLocal.Vila && _previaPlantioAtiva)
            DesenharPreviaDePlantio(mapa, camX, camY, viewport);

        foreach (var bicho in _estado.BichosNoLocalAtual)
            DesenharEntidade(bicho.Posicao, camX, camY, viewport, Cores.Bicho, new Rune('a'));

        foreach (var monstro in _estado.MonstrosNoLocalAtual)
            DesenharEntidade(monstro.Posicao, camX, camY, viewport, CorDoMonstro(monstro.Tipo), new Rune('m'));

        foreach (var p in _estado.PersonagensNoLocalAtual)
        {
            var tx = p.Posicao.X - camX;
            var ty = p.Posicao.Y - camY;
            if (tx < 0 || tx >= viewport.Width || ty < 0 || ty >= viewport.Height)
                continue;

            var cor = p.Vida <= 0 ? Cores.TextoSecundario
                : ReferenceEquals(p, _estado.Personagem) ? Cores.Personagem
                : p.EhCrianca ? Cores.Crianca
                : Cores.PersonagemVivo;
            var glifo = p.Vida > 0 && p.EhCrianca ? new Rune('c') : new Rune('@');
            SetAttribute(new Attribute(cor, Cores.Fundo));
            AddRune(tx, ty, glifo);
        }

        return true;
    }

    private void DesenharEntidade(Posicao pos, int camX, int camY, System.Drawing.Rectangle viewport, Color cor, Rune glifo)
    {
        var tx = pos.X - camX;
        var ty = pos.Y - camY;
        if (tx < 0 || tx >= viewport.Width || ty < 0 || ty >= viewport.Height)
            return;

        SetAttribute(new Attribute(cor, Cores.Fundo));
        AddRune(tx, ty, glifo);
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

    private void DesenharPreviaDeFogueira(MapaDaMasmorra mapa, int camX, int camY, System.Drawing.Rectangle viewport)
    {
        var (posicao, valida) = _estado.ObterPreviaDeFogueira();
        var corFundoPrevia = valida ? Cores.PreviaValida : Cores.PreviaInvalida;
        DesenharCelulaComPrevia(mapa, posicao.X, posicao.Y, camX, camY, viewport, corFundoPrevia);
    }

    private void DesenharPreviaDePlantio(MapaDaMasmorra mapa, int camX, int camY, System.Drawing.Rectangle viewport)
    {
        var (posicao, valida) = _estado.ObterPreviaDePlantio();
        var corFundoPrevia = valida ? Cores.PreviaValida : Cores.PreviaInvalida;
        DesenharCelulaComPrevia(mapa, posicao.X, posicao.Y, camX, camY, viewport, corFundoPrevia);
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
        var opcao = MessageBox.Query(Application.Instance, "Sair", "Sair do jogo?", "Sair sem salvar", "Salvar e sair", "Cancelar");
        switch (opcao)
        {
            case 0:
                Application.RequestStop();
                break;
            case 1:
                if (TentarSalvar())
                    Application.RequestStop();
                break;
        }

        return false;
    }

    private bool Salvar()
    {
        TentarSalvar();
        return true;
    }

    private bool TentarSalvar()
    {
        try
        {
            _estado.Salvar(_caminhoDoSave);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            MessageBox.ErrorQuery(Application.Instance, "Erro ao salvar", ex.Message, "OK");
            return false;
        }
    }

    private bool Carregar()
    {
        if (!File.Exists(_caminhoDoSave))
            return false;

        var opcao = MessageBox.Query(Application.Instance, "Carregar", "Isso substitui o progresso atual pelo último save. Continuar?", "Carregar", "Cancelar");
        return opcao == 0 && CarregarSemConfirmar();
    }

    // so faz sentido depois da extincao da vila (JogoEncerrado/SoRestamCriancas) -- durante o jogo
    // normal, Morto so fica verdadeiro por uma fracao de turno (o controle e transferido no mesmo
    // AvancarTurno), entao a tecla fica inerte sem incomodar o jogador
    private bool Reiniciar()
    {
        if (!_estado.JogoEncerrado && !_estado.SoRestamCriancas)
            return false;

        var opcao = MessageBox.Query(Application.Instance, "Novo jogo", "Começar um novo jogo? O progresso desta vila será perdido.", "Novo jogo", "Cancelar");
        if (opcao != 0)
            return false;

        _estado = new EstadoDoJogo(_larguraDoMundo, _alturaDoMundo);
        PararTempoReal();
        return true;
    }

    // usado tambem no boot (Programa.cs), onde nao ha "progresso atual" a proteger --
    // por isso fica publico e sem o dialogo de confirmacao que Carregar() mostra pro F9
    public bool CarregarSemConfirmar()
    {
        try
        {
            _estado = EstadoDoJogo.CarregarDe(_caminhoDoSave);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            MessageBox.ErrorQuery(Application.Instance, "Erro ao carregar", ex.Message, "OK");
            return false;
        }

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
            _previaFogueiraAtiva = false;
            _previaPlantioAtiva = false;
        }

        _ultimoCliqueDoC = agora;
        return true;
    }

    private bool AlternarOuConstruirFogueira()
    {
        if (_estado.Modo != ModoDeJogo.Jogando)
            return false;

        var agora = DateTime.UtcNow;
        var cliqueDuplo = _previaFogueiraAtiva && agora - _ultimoCliqueDoF <= JanelaDeCliqueDuplo;

        if (cliqueDuplo)
        {
            _previaFogueiraAtiva = false;
        }
        else if (_previaFogueiraAtiva)
        {
            _estado.TentarConstruirFogueira();
            _previaFogueiraAtiva = false;
        }
        else
        {
            _previaFogueiraAtiva = true;
            _previaAtiva = false;
            _previaPlantioAtiva = false;
        }

        _ultimoCliqueDoF = agora;
        return true;
    }

    private bool AlternarOuPlantar()
    {
        if (_estado.Modo != ModoDeJogo.Jogando)
            return false;

        var agora = DateTime.UtcNow;
        var cliqueDuplo = _previaPlantioAtiva && agora - _ultimoCliqueDoP <= JanelaDeCliqueDuplo;

        if (cliqueDuplo)
        {
            _previaPlantioAtiva = false;
        }
        else if (_previaPlantioAtiva)
        {
            _estado.TentarPlantar();
            _previaPlantioAtiva = false;
        }
        else
        {
            _previaPlantioAtiva = true;
            _previaAtiva = false;
            _previaFogueiraAtiva = false;
        }

        _ultimoCliqueDoP = agora;
        return true;
    }
}
