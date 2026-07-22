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

    public Action? AoFechar { get; set; }
    public Action? AoAtualizarOutros { get; set; }

    public PainelInventario(Func<EstadoDoJogo> obterEstado) : base(obterEstado)
    {
        CanFocus = true;
        Width = LarguraTotal;
        Height = AlturaTotal;
    }

    protected override void Desenhar(EstadoDoJogo estado)
    {
        this.DesenharMoldura(LarguraInterna, AlturaInterna, Cores.TextoPrincipal, "Inventário");

        var jogador = estado.Jogador;

        SetAttribute(new Attribute(Cores.TextoPrincipal, Cores.Fundo));
        AddStr(2, 1, "Equipamento");
        AddStr(34, 1, "Mochila");

        for (var i = 0; i < SlotsEquipamento.Length; i++)
        {
            var slot = SlotsEquipamento[i];
            var item = jogador.ObterEquipado(slot);
            var texto = $"{NomesSlot[slot]}: {(item is null ? "(vazio)" : item.Nome)}";
            DesenharLinha(2, 3 + i, texto, _coluna == Coluna.Equipamento && _indiceEquipamento == i);
        }

        SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
        AddStr(2, 8, $"Defesa total: {jogador.DefesaTotal}");
        AddStr(2, 9, $"Vida: {jogador.Vida}/{jogador.VidaMaxima}   Ouro: {jogador.Ouro}");

        if (jogador.Mochila.Count == 0)
        {
            SetAttribute(new Attribute(Cores.TextoSecundario, Cores.Fundo));
            AddStr(34, 3, "(vazia)");
        }
        else
        {
            for (var i = 0; i < jogador.Mochila.Count && i < AlturaInterna - 6; i++)
            {
                var texto = $"{i + 1}) {jogador.Mochila[i].Nome}";
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

        if (key.KeyCode == KeyCode.Esc || key.AsRune.Value is 'i' or 'I')
        {
            AoFechar?.Invoke();
            return true;
        }

        if (key.KeyCode == KeyCode.Tab)
        {
            _coluna = _coluna == Coluna.Equipamento ? Coluna.Mochila : Coluna.Equipamento;
            SetNeedsDraw();
            return true;
        }

        if (key.KeyCode == KeyCode.CursorUp)
        {
            MoverSelecao(estado, -1);
            return true;
        }

        if (key.KeyCode == KeyCode.CursorDown)
        {
            MoverSelecao(estado, 1);
            return true;
        }

        if (key.KeyCode == KeyCode.Enter)
        {
            Confirmar(estado);
            return true;
        }

        if (key.KeyCode == KeyCode.Delete || key.AsRune.Value is 'x' or 'X')
        {
            Descartar(estado);
            return true;
        }

        return base.OnKeyDown(key);
    }

    private void MoverSelecao(EstadoDoJogo estado, int delta)
    {
        if (_coluna == Coluna.Equipamento)
        {
            _indiceEquipamento = Math.Clamp(_indiceEquipamento + delta, 0, SlotsEquipamento.Length - 1);
        }
        else
        {
            var quantidade = estado.Jogador.Mochila.Count;
            if (quantidade > 0)
                _indiceMochila = Math.Clamp(_indiceMochila + delta, 0, quantidade - 1);
        }

        SetNeedsDraw();
    }

    private void Confirmar(EstadoDoJogo estado)
    {
        if (_coluna == Coluna.Mochila && _indiceMochila < estado.Jogador.Mochila.Count)
        {
            estado.AcionarItemDaMochila(_indiceMochila);
            _indiceMochila = Math.Clamp(_indiceMochila, 0, Math.Max(0, estado.Jogador.Mochila.Count - 1));
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
        else if (_indiceMochila < estado.Jogador.Mochila.Count)
        {
            estado.DescartarDaMochila(_indiceMochila);
            _indiceMochila = Math.Clamp(_indiceMochila, 0, Math.Max(0, estado.Jogador.Mochila.Count - 1));
        }

        SetNeedsDraw();
        AoAtualizarOutros?.Invoke();
    }
}
