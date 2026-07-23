using Mausmorras.Nucleo.Itens;

namespace Mausmorras.Nucleo.Jogo;

public sealed partial class EstadoDoJogo
{
    public void AcionarItemDaMochila(int indice)
    {
        if (IndiceInvalido(indice))
            return;

        var item = Personagem.Mochila[indice];

        if (item.Tipo == TipoDeItem.Comida)
        {
            ComerAlimento(item);
            Personagem.Mochila.RemoveAt(indice);
            return;
        }

        if (item.Tipo == TipoDeItem.Generico)
        {
            UsarConsumivel(item);
            Personagem.Mochila.RemoveAt(indice);
            return;
        }

        var equipadoAtual = Personagem.ObterEquipado(item.Tipo);
        Personagem.Equipar(item.Tipo, item);
        Personagem.Mochila.RemoveAt(indice);

        if (equipadoAtual is not null)
            Personagem.Mochila.Add(equipadoAtual);

        AdicionarMensagem($"Você equipa {item.Nome}.");
    }

    public void DescartarDaMochila(int indice)
    {
        if (IndiceInvalido(indice))
            return;

        var item = Personagem.Mochila[indice];
        Personagem.Mochila.RemoveAt(indice);
        AdicionarMensagem($"Você joga {item.Nome} no lixo.");
    }

    private bool IndiceInvalido(int indice) => indice < 0 || indice >= Personagem.Mochila.Count;

    public void DescartarEquipado(TipoDeItem tipo)
    {
        var item = Personagem.ObterEquipado(tipo);
        if (item is null)
            return;

        Personagem.Equipar(tipo, null);
        AdicionarMensagem($"Você joga {item.Nome} no lixo.");
    }

    private void UsarConsumivel(Item item)
    {
        var antes = Personagem.Vida;
        Personagem.Vida = Math.Min(Personagem.VidaMaxima, Personagem.Vida + item.Valor);
        var curado = Personagem.Vida - antes;

        AdicionarMensagem(curado > 0
            ? $"Você usa {item.Nome} e recupera {curado} de vida."
            : $"Você usa {item.Nome}, mas já estava com a vida cheia.");
    }

    private void ComerAlimento(Item item)
    {
        var antes = Personagem.Fome;
        Personagem.Fome = Math.Max(0, Personagem.Fome - item.Valor);
        var reduzido = antes - Personagem.Fome;

        AdicionarMensagem(reduzido > 0
            ? $"Você come {item.Nome} e reduz {reduzido} de fome."
            : $"Você come {item.Nome}, mas já não estava com fome.");
    }
}
