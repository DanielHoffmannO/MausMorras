using System.Text.Json.Serialization;
using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Jogo;

namespace Mausmorras.Nucleo.Persistencia;

public sealed class EstadoSalvo
{
    public int Largura { get; set; }
    public int Altura { get; set; }
    public int Andar { get; set; }

    // Nomes de campo prefixados com "Jogador" são mantidos por compatibilidade com saves
    // já existentes (~/.mausmorras_save.json) — renomear quebraria o casamento por nome do
    // System.Text.Json sem nenhum ganho, mesmo depois do tipo em memória virar Personagem.
    public int JogadorX { get; set; }
    public int JogadorY { get; set; }
    public int VidaJogador { get; set; }
    public int VidaMaximaJogador { get; set; }
    public int OuroJogador { get; set; }
    public int MadeiraJogador { get; set; }
    public int Turno { get; set; }
    public int Madeira { get; set; }
    public ModoDeJogo Modo { get; set; } = ModoDeJogo.Jogando;
    public List<PersonagemSalvo> Personagens { get; set; } = new();
    public int IndiceSelecionado { get; set; }
    public int[] Celulas { get; set; } = Array.Empty<int>();
    public bool[] Explorada { get; set; } = Array.Empty<bool>();
    public List<string> Mensagens { get; set; } = new();
    public List<ItemSalvo> Mochila { get; set; } = new();
    public ItemSalvo? Capacete { get; set; }
    public ItemSalvo? Peitoral { get; set; }
    public ItemSalvo? Pernas { get; set; }
    public ItemSalvo? Botas { get; set; }
    public List<ItemNoChaoSalvo> ItensNoChao { get; set; } = new();
    public List<BichoSalvo> Bichos { get; set; } = new();
    public List<FogueiraAtivaSalva> FogueirasAtivas { get; set; } = new();
    public bool PrimeiroAbrigoConstruido { get; set; }
    public int NumeroDeCasas { get; set; }
    public List<ColheitaPendenteSalva> ColheitasPendentes { get; set; } = new();
    public List<ItemSalvo> Bau { get; set; } = new();
}

public sealed class PersonagemSalvo
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Vida { get; set; }
    public int VidaMaxima { get; set; }
    public int Ouro { get; set; }
    [JsonPropertyName("Madeira")]
    public int MadeiraLegado { get; set; } // saves do formato intermediario (madeira por personagem, antes dela virar estoque compartilhado) -- so serve pra migracao em EstadoDoJogo.Persistencia.cs, nao usado em mais nada
    public int Fome { get; set; }
    public int Temperatura { get; set; } = 33; // saves antigos (formato "Frio" 0-300) nao tem esse campo -- default 33 = ideal, ja que as escalas sao incompativeis e nao ha migracao sensata possivel
    public int Sono { get; set; }
    public bool EhCrianca { get; set; }
    public int Idade { get; set; }
    public int Traco { get; set; }
    public double AversaoAoFrio { get; set; }
    public double AversaoAFome { get; set; }
    public double AversaoAoSono { get; set; }
    public List<ItemSalvo> Mochila { get; set; } = new();
    public ItemSalvo? Capacete { get; set; }
    public ItemSalvo? Peitoral { get; set; }
    public ItemSalvo? Pernas { get; set; }
    public ItemSalvo? Botas { get; set; }
}

public sealed class BichoSalvo
{
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class FogueiraAtivaSalva
{
    public int X { get; set; }
    public int Y { get; set; }
    public int TurnoDeExpiracao { get; set; }
}

public sealed class ColheitaPendenteSalva
{
    public int X { get; set; }
    public int Y { get; set; }
    public int TurnoDisponivel { get; set; }
}

public sealed class ItemSalvo
{
    public string Nome { get; set; } = "";
    public TipoDeItem Tipo { get; set; }
    public int Valor { get; set; }
}

public sealed class ItemNoChaoSalvo
{
    public int X { get; set; }
    public int Y { get; set; }
    public ItemSalvo Item { get; set; } = new();
}
