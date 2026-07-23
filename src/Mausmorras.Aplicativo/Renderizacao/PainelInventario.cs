using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Aplicativo.Renderizacao;

public sealed class PainelInventario : PainelDeEstado
{
    private const int LarguraInterna = 62;
    private const int AlturaInterna = 22;
    private const int LarguraColuna = 28;

    public const int LarguraTotal = LarguraInterna + 2;
    public const int AlturaTotal = AlturaInterna + 2;

    private static readonly TipoDeItem[] SlotsEquipamento =
        { TipoDeItem.Capacete, TipoDeItem.Peitoral, TipoDeItem.Pernas, TipoDeItem.Botas };

    private static readonly Dictionary<TipoDeItem, string> NomesSlot = new()
    {
        [TipoDeItem.Capacete] = "Capacete",
        [TipoDeItem.Peitoral] = "Peitoral",
        [TipoDeItem.Pernas] = "Pernas",
        [TipoDeItem.Botas] = "Botas"
    };

    private enum Coluna
    {
        Equipamento,
        Mochila
    }

    private Coluna _coluna = Coluna.Equipamento;
    private int _indiceEquipamento;
    private int _indiceMochila;

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
        };
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        this.DesenharMoldura(LarguraInterna, AlturaInterna, Cores.TextoPrincipal, "Inventário");

        var personagem = estado.Personagem;

        SetAttribute(new Attribute(Cores.TextoPrincipal, Cores.Fundo));
        AddStr(2, 1, "Equipamento");
        AddStr(34, 1, "Mochila");

        for (var i = 0; i < SlotsEquipamento.Length; i++)
        {
            var slot = SlotsEquipamento[i];
            var item = personagem.ObterEquipado(slot);
            var texto = $"{NomesSlot[slot]}: {(item is null ? "(vazio)" : item.Nome)}";
            DesenharLinha(2, 3 + i, texto, _coluna == Coluna.Equipamento && _indiceEquipamento == i);
        }

        SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
        AddStr(2, 8, $"Defesa total: {personagem.DefesaTotal}");
        AddStr(2, 9, $"Vida: {personagem.Vida}/{personagem.VidaMaxima}   Ouro: {personagem.Ouro}");
        AddStr(2, 10, $"Madeira: {estado.Madeira}   Fome: {personagem.Fome}");

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
                DesenharLinha(34, 3 + i, texto, _coluna == Coluna.Mochila && _indiceMochila == i);
            }
        }

        SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
        AddStr(2, AlturaInterna - 2, "Tab troca coluna   Setas navega   Enter equipa/usa");
        AddStr(2, AlturaInterna - 1, "Del joga no lixo   I ou Esc fecha");
    }

    private void DesenharLinha(int x, int y, string texto, bool selecionado)
    {
        var textoFormatado = texto.Length > LarguraColuna ? texto[..LarguraColuna] : texto.PadRight(LarguraColuna);

        SetAttribute(selecionado
            ? new Attribute(Cores.Fundo, Cores.Selecao)
            : new Attribute(Cores.TextoSecundario, Cores.Fundo));

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
        _coluna = _coluna == Coluna.Equipamento ? Coluna.Mochila : Coluna.Equipamento;
        SetNeedsDraw();
    }

    private void MoverSelecao(EstadoDoJogo estado, int delta)
    {
        if (_coluna == Coluna.Equipamento)
        {
            _indiceEquipamento = Math.Clamp(_indiceEquipamento + delta, 0, SlotsEquipamento.Length - 1);
        }
        else
        {
            var quantidade = estado.Personagem.Mochila.Count;
            if (quantidade > 0)
                _indiceMochila = Math.Clamp(_indiceMochila + delta, 0, quantidade - 1);
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

        SetNeedsDraw();
        AoAtualizarOutros?.Invoke();
    }

    private void Descartar(EstadoDoJogo estado)
    {
        if (_coluna == Coluna.Equipamento)
        {
            estado.DescartarEquipado(SlotsEquipamento[_indiceEquipamento]);
        }
        else if (_indiceMochila < estado.Personagem.Mochila.Count)
        {
            estado.DescartarDaMochila(_indiceMochila);
            _indiceMochila = Math.Clamp(_indiceMochila, 0, Math.Max(0, estado.Personagem.Mochila.Count - 1));
        }

        SetNeedsDraw();
        AoAtualizarOutros?.Invoke();
    }
}
