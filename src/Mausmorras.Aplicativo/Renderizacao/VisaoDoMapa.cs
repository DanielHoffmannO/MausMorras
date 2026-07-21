using System.Text;
using Mausmorras.Nucleo.Jogo;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class VisaoDoMapa : View
{
    private const double IntensidadeEscurecimento = 0.55;

    private readonly string _caminhoDoSave;
    private EstadoDoJogo _estado;

    public Action? AoAtualizar { get; set; }

    public EstadoDoJogo Estado => _estado;

    public VisaoDoMapa(EstadoDoJogo estado, string caminhoDoSave)
    {
        _estado = estado;
        _caminhoDoSave = caminhoDoSave;
        CanFocus = true;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    private static readonly Color CorDeFundo = new(ColorName16.Black);
    private static readonly Color CorDoJogador = new(ColorName16.BrightYellow);

    private static (Rune Glifo, Color CorFrente) ObterVisualDaCelula(TipoDeCelula celula) => celula switch
    {
        TipoDeCelula.Parede => (new Rune('#'), new Color(ColorName16.DarkGray)),
        TipoDeCelula.ParedeDecorada => (new Rune('%'), new Color(ColorName16.BrightRed).GetDimmerColor(0.3)),
        TipoDeCelula.Chao => (new Rune('.'), new Color(ColorName16.Gray)),
        TipoDeCelula.Porta => (new Rune('+'), new Color(ColorName16.BrightYellow)),
        TipoDeCelula.Escada => (new Rune('>'), new Color(ColorName16.BrightCyan)),
        TipoDeCelula.Grama => (new Rune(','), new Color(ColorName16.BrightGreen)),
        TipoDeCelula.Agua => (new Rune('~'), new Color(ColorName16.BrightBlue)),
        TipoDeCelula.Entulho => (new Rune(':'), new Color(ColorName16.Yellow)),
        TipoDeCelula.Abismo => (new Rune(' '), new Color(ColorName16.Black)),
        TipoDeCelula.Ouro => (new Rune('$'), new Color(ColorName16.BrightYellow)),
        _ => (new Rune('?'), new Color(ColorName16.Red))
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
                var visivel = _estado.CelulasVisiveis.Contains(new Posicao(mapaX, mapaY));

                SetAttribute(new Attribute(visivel ? corFrente : corFrente.GetDimmerColor(IntensidadeEscurecimento), CorDeFundo));
                AddRune(telaX, telaY, glifo);
            }
        }

        SetAttribute(new Attribute(CorDoJogador, CorDeFundo));
        AddRune(jogador.X - camX, jogador.Y - camY, new Rune('@'));

        return true;
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

    private bool TratarTeclasDeLetra(Key key) => key.AsRune.Value switch
    {
        'w' or 'W' => _estado.TentarMoverJogador(Direcao.Norte),
        's' or 'S' => _estado.TentarMoverJogador(Direcao.Sul),
        'a' or 'A' => _estado.TentarMoverJogador(Direcao.Oeste),
        'd' or 'D' => _estado.TentarMoverJogador(Direcao.Leste),
        _ => false
    };
}
