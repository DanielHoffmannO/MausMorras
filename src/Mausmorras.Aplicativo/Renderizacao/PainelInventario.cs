using System.Text;
using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelInventario : PainelDeEstado
{
    private const int LarguraInterna = 96;
    private const int AlturaInterna = 22;
    private const int LarguraColuna = 28;

    public const int LarguraTotal = LarguraInterna + 2;
    public const int AlturaTotal = AlturaInterna + 2;

    private static readonly TipoDeItem[] SlotsEquipamento =
        { TipoDeItem.Arma, TipoDeItem.Capacete, TipoDeItem.Peitoral, TipoDeItem.Pernas, TipoDeItem.Botas };

    private static readonly Dictionary<TipoDeItem, string> NomesSlot = new()
    {
        [TipoDeItem.Arma] = "Arma",
        [TipoDeItem.Capacete] = "Capacete",
        [TipoDeItem.Peitoral] = "Peitoral",
        [TipoDeItem.Pernas] = "Pernas",
        [TipoDeItem.Botas] = "Botas"
    };

    private enum Coluna
    {
        Equipamento,
        Mochila,
        Bau
    }

    private Coluna _coluna = Coluna.Equipamento;
    private int _indiceEquipamento;
    private int _indiceMochila;
    private int _indiceBau;

    private readonly Dictionary<KeyCode, Action<EstadoDoJogo>> _acoesPorTecla;
    private readonly Dictionary<char, Action<EstadoDoJogo>> _acoesPorLetra;

    public Action? AoFechar { get; set; }
    public Action? AoAtualizarOutros { get; set; }

    public PainelInventario(Func<EstadoDoJogo> obterEstado) : base(obterEstado)
    {
        CanFocus = true;
        Width = LarguraTotal;
        Height = AlturaTotal;

        _acoesPorTecla = new Dictionary<KeyCode, Action<EstadoDoJogo>>
        {
            [KeyCode.Esc] = _ => Fechar(),
            [KeyCode.Tab] = _ => TrocarColuna(),
            [KeyCode.CursorUp] = estado => MoverSelecao(estado, -1),
            [KeyCode.CursorDown] = estado => MoverSelecao(estado, 1),
            [KeyCode.Enter] = Confirmar,
            [KeyCode.Delete] = Descartar,
        };

        _acoesPorLetra = new Dictionary<char, Action<EstadoDoJogo>>
        {
            ['i'] = _ => Fechar(),
            ['x'] = Descartar,
            ['b'] = MoverParaOBau,
        };
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        var personagem = estado.Personagem;
        this.DesenharMoldura(LarguraInterna, AlturaInterna, Cores.TextoPrincipal, $"Inventário de {personagem.Nome}");

        // divisorias entre as tres colunas, no mesmo estilo de traco da moldura -- sem isso as
        // colunas viravam so blocos de texto flutuando lado a lado, sem nenhuma separacao visual
        SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
        for (var y = 1; y <= AlturaInterna - 3; y++)
        {
            AddRune(32, y, new Rune('│'));
            AddRune(64, y, new Rune('│'));
        }

        SetAttribute(new Attribute(Cores.Selecao, Cores.Fundo));
        AddStr(2, 1, "Equipamento");
        AddStr(34, 1, "Mochila");
        AddStr(66, 1, "Baú (compartilhado)");

        for (var i = 0; i < SlotsEquipamento.Length; i++)
        {
            var slot = SlotsEquipamento[i];
            var item = personagem.ObterEquipado(slot);
            var texto = $"{NomesSlot[slot]}: {(item is null ? "(vazio)" : item.Nome)}";
            DesenharLinha(2, 3 + i, texto, item is not null, _coluna == Coluna.Equipamento && _indiceEquipamento == i);
        }

        var corVida = (double)personagem.Vida / personagem.VidaMaxima <= 0.3 ? Cores.Perigo : Cores.TextoSecundario;
        SetAttribute(new Attribute(corVida, Cores.Fundo));
        AddStr(2, 9, $"Vida: {DesenhoDeCaixa.DesenharBarra(personagem.Vida, personagem.VidaMaxima, 10)} {personagem.Vida}/{personagem.VidaMaxima}");
        SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
        AddStr(2, 10, $"Ataque: +{personagem.AtaqueTotal}   Defesa: {personagem.DefesaTotal}   Traço: {personagem.Traco}");
        AddStr(2, 11, $"Ouro: {estado.Ouro}   Madeira: {estado.Madeira}   Fome: {personagem.Fome}");

        if (personagem.Mochila.Count == 0)
        {
            SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
            AddStr(34, 3, "(vazia)");
        }
        else
        {
            for (var i = 0; i < personagem.Mochila.Count && i < AlturaInterna - 6; i++)
            {
                var texto = $"{i + 1}) {personagem.Mochila[i].Nome}";
                DesenharLinha(34, 3 + i, texto, true, _coluna == Coluna.Mochila && _indiceMochila == i);
            }
        }

        if (estado.Bau.Count == 0)
        {
            SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
            AddStr(66, 3, "(vazio)");
        }
        else
        {
            for (var i = 0; i < estado.Bau.Count && i < AlturaInterna - 6; i++)
            {
                var texto = $"{i + 1}) {estado.Bau[i].Nome}";
                DesenharLinha(66, 3 + i, texto, true, _coluna == Coluna.Bau && _indiceBau == i);
            }
        }

        SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
        AddStr(2, AlturaInterna - 2, "Tab troca coluna   Setas navega   Enter equipa/usa/retira do baú   B guarda no baú");
        AddStr(2, AlturaInterna - 1, "Del joga no lixo   I ou Esc fecha");
    }

    private void DesenharLinha(int x, int y, string texto, bool preenchido, bool selecionado)
    {
        // cursor "▸" alem da cor invertida -- so a cor sozinha (um bloco solido tomando a linha
        // inteira) deixava a selecao dificil de notar rapido, alem de nao dar nenhum indicio de
        // "voce esta aqui" quando o olho ja passou batido pela cor
        var prefixo = selecionado ? "▸ " : "  ";
        var textoFormatado = (prefixo + texto).Length > LarguraColuna
            ? (prefixo + texto)[..LarguraColuna]
            : (prefixo + texto).PadRight(LarguraColuna);

        var cor = selecionado ? Cores.Selecao : preenchido ? Cores.TextoPrincipal : Cores.TextoSecundario;
        SetAttribute(selecionado
            ? new Attribute(Cores.Fundo, cor)
            : new Attribute(cor, Cores.Fundo));

        AddStr(x, y, textoFormatado);
    }

    protected override bool OnKeyDown(Key key)
    {
        var estado = _obterEstado();

        if (_acoesPorTecla.TryGetValue(key.KeyCode, out var acaoPorTecla))
        {
            acaoPorTecla(estado);
            return true;
        }

        var letra = char.ToLowerInvariant((char)key.AsRune.Value);
        if (_acoesPorLetra.TryGetValue(letra, out var acaoPorLetra))
        {
            acaoPorLetra(estado);
            return true;
        }

        return base.OnKeyDown(key);
    }

    private void Fechar() => AoFechar?.Invoke();

    private void TrocarColuna()
    {
        _coluna = _coluna switch
        {
            Coluna.Equipamento => Coluna.Mochila,
            Coluna.Mochila => Coluna.Bau,
            _ => Coluna.Equipamento
        };
        SetNeedsDraw();
    }

    private void MoverSelecao(EstadoDoJogo estado, int delta)
    {
        if (_coluna == Coluna.Equipamento)
        {
            _indiceEquipamento = Math.Clamp(_indiceEquipamento + delta, 0, SlotsEquipamento.Length - 1);
        }
        else if (_coluna == Coluna.Mochila)
        {
            var quantidade = estado.Personagem.Mochila.Count;
            if (quantidade > 0)
                _indiceMochila = Math.Clamp(_indiceMochila + delta, 0, quantidade - 1);
        }
        else
        {
            var quantidade = estado.Bau.Count;
            if (quantidade > 0)
                _indiceBau = Math.Clamp(_indiceBau + delta, 0, quantidade - 1);
        }

        SetNeedsDraw();
    }

    private void Confirmar(EstadoDoJogo estado)
    {
        if (_coluna == Coluna.Mochila && _indiceMochila < estado.Personagem.Mochila.Count)
        {
            estado.AcionarItemDaMochila(_indiceMochila);
            _indiceMochila = Math.Clamp(_indiceMochila, 0, Math.Max(0, estado.Personagem.Mochila.Count - 1));
        }
        else if (_coluna == Coluna.Bau && _indiceBau < estado.Bau.Count)
        {
            estado.RetirarDoBau(_indiceBau);
            _indiceBau = Math.Clamp(_indiceBau, 0, Math.Max(0, estado.Bau.Count - 1));
        }

        SetNeedsDraw();
        AoAtualizarOutros?.Invoke();
    }

    private void MoverParaOBau(EstadoDoJogo estado)
    {
        if (_coluna != Coluna.Mochila || _indiceMochila >= estado.Personagem.Mochila.Count)
            return;

        estado.DepositarNoBau(_indiceMochila);
        _indiceMochila = Math.Clamp(_indiceMochila, 0, Math.Max(0, estado.Personagem.Mochila.Count - 1));
        SetNeedsDraw();
        AoAtualizarOutros?.Invoke();
    }

    private void Descartar(EstadoDoJogo estado)
    {
        if (_coluna == Coluna.Equipamento)
        {
            estado.DescartarEquipado(SlotsEquipamento[_indiceEquipamento]);
        }
        else if (_coluna == Coluna.Mochila && _indiceMochila < estado.Personagem.Mochila.Count)
        {
            estado.DescartarDaMochila(_indiceMochila);
            _indiceMochila = Math.Clamp(_indiceMochila, 0, Math.Max(0, estado.Personagem.Mochila.Count - 1));
        }

        SetNeedsDraw();
        AoAtualizarOutros?.Invoke();
    }
}
