namespace Mausmorras.Nucleo.Itens;

public sealed class Item
{
    public string Nome { get; }
    public TipoDeItem Tipo { get; }

    /// <summary>Defesa para equipamento, cura para itens genéricos (poções).</summary>
    public int Valor { get; }

    public Item(string nome, TipoDeItem tipo, int valor)
    {
        Nome = nome;
        Tipo = tipo;
        Valor = valor;
    }
}
