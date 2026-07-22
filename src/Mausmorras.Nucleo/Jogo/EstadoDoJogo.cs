using System.Text.Json;
using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Geracao;
using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;
using Mausmorras.Nucleo.Persistencia;

namespace Mausmorras.Nucleo.Jogo;

public sealed class EstadoDoJogo
{
    private const int RaioDeVisao = 10;
    private const int MaximoDeMensagens = 200;
    private const int OuroPorPilha = 10;

    private readonly List<string> _mensagens = new();
    private Dictionary<Posicao, Item> _itensNoChao = new();

    private int _largura;
    private int _altura;
    private Random _random = new();

    public MapaDaMasmorra Mapa { get; private set; } = null!;
    public IReadOnlyList<Sala> Salas { get; private set; } = Array.Empty<Sala>();
    public Jogador Jogador { get; private set; } = null!;
    public int Andar { get; private set; } = 1;
    public IReadOnlySet<Posicao> CelulasVisiveis { get; private set; } = new HashSet<Posicao>();
    public IReadOnlyList<string> Mensagens => _mensagens;
    public bool Morto => Jogador.Vida <= 0;

    public EstadoDoJogo(int largura, int altura, int? seed = null)
    {
        _largura = largura;
        _altura = altura;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();

        (Mapa, Salas) = GerarNivel();

        var inicio = Salas.Count > 0 ? Salas[0].Centro : new Posicao(largura / 2, altura / 2);
        Jogador = new Jogador(inicio);
        AdicionarMensagem("Você entra na masmorra escura.");
        AtualizarVisibilidade();
    }

    private EstadoDoJogo()
    {
    }

    public bool TentarMoverJogador(Posicao delta)
    {
        if (Morto)
            return false;

        var alvo = Jogador.Posicao + delta;
        if (!Mapa.EhCaminhavel(alvo))
            return false;

        Jogador.Posicao = alvo;

        switch (Mapa[alvo.X, alvo.Y])
        {
            case TipoDeCelula.Grama:
                Mapa[alvo.X, alvo.Y] = TipoDeCelula.Chao;
                break;

            case TipoDeCelula.Ouro:
                Mapa[alvo.X, alvo.Y] = TipoDeCelula.Chao;
                Jogador.Ouro += OuroPorPilha;
                AdicionarMensagem($"Você encontra {OuroPorPilha} moedas de ouro.");
                break;

            case TipoDeCelula.Item:
                ColetarItem(alvo);
                break;

            case TipoDeCelula.Escada:
                Descer();
                break;
        }

        AtualizarVisibilidade();
        return true;
    }

    public void AcionarItemDaMochila(int indice)
    {
        if (IndiceInvalido(indice))
            return;

        var item = Jogador.Mochila[indice];

        if (item.Tipo == TipoDeItem.Generico)
        {
            UsarConsumivel(item);
            Jogador.Mochila.RemoveAt(indice);
            return;
        }

        var equipadoAtual = Jogador.ObterEquipado(item.Tipo);
        Jogador.Equipar(item.Tipo, item);
        Jogador.Mochila.RemoveAt(indice);

        if (equipadoAtual is not null)
            Jogador.Mochila.Add(equipadoAtual);

        AdicionarMensagem($"Você equipa {item.Nome}.");
    }

    public void DescartarDaMochila(int indice)
    {
        if (IndiceInvalido(indice))
            return;

        var item = Jogador.Mochila[indice];
        Jogador.Mochila.RemoveAt(indice);
        AdicionarMensagem($"Você joga {item.Nome} no lixo.");
    }

    private bool IndiceInvalido(int indice) => indice < 0 || indice >= Jogador.Mochila.Count;

    public void DescartarEquipado(TipoDeItem tipo)
    {
        var item = Jogador.ObterEquipado(tipo);
        if (item is null)
            return;

        Jogador.Equipar(tipo, null);
        AdicionarMensagem($"Você joga {item.Nome} no lixo.");
    }

    public void Salvar(string caminho)
    {
        var dto = new EstadoSalvo
        {
            Largura = Mapa.Largura,
            Altura = Mapa.Altura,
            Andar = Andar,
            JogadorX = Jogador.Posicao.X,
            JogadorY = Jogador.Posicao.Y,
            VidaJogador = Jogador.Vida,
            VidaMaximaJogador = Jogador.VidaMaxima,
            OuroJogador = Jogador.Ouro,
            Celulas = new int[Mapa.Largura * Mapa.Altura],
            Explorada = new bool[Mapa.Largura * Mapa.Altura],
            Mensagens = _mensagens.ToList(),
            Mochila = Jogador.Mochila.Select(ParaSalvo).ToList(),
            Capacete = Jogador.Capacete is { } c ? ParaSalvo(c) : null,
            Peitoral = Jogador.Peitoral is { } p ? ParaSalvo(p) : null,
            Pernas = Jogador.Pernas is { } pr ? ParaSalvo(pr) : null,
            Botas = Jogador.Botas is { } b ? ParaSalvo(b) : null,
            ItensNoChao = _itensNoChao
                .Select(kv => new ItemNoChaoSalvo { X = kv.Key.X, Y = kv.Key.Y, Item = ParaSalvo(kv.Value) })
                .ToList()
        };

        for (var x = 0; x < Mapa.Largura; x++)
        {
            for (var y = 0; y < Mapa.Altura; y++)
            {
                var indice = y * Mapa.Largura + x;
                dto.Celulas[indice] = (int)Mapa[x, y];
                dto.Explorada[indice] = Mapa.FoiExplorada(x, y);
            }
        }

        File.WriteAllText(caminho, JsonSerializer.Serialize(dto));
        AdicionarMensagem("Jogo salvo.");
    }

    public static EstadoDoJogo CarregarDe(string caminho)
    {
        var dto = JsonSerializer.Deserialize<EstadoSalvo>(File.ReadAllText(caminho))
                   ?? throw new InvalidDataException("Arquivo de save inválido.");

        var mapa = new MapaDaMasmorra(dto.Largura, dto.Altura);
        for (var x = 0; x < dto.Largura; x++)
        {
            for (var y = 0; y < dto.Altura; y++)
            {
                var indice = y * dto.Largura + x;
                mapa[x, y] = (TipoDeCelula)dto.Celulas[indice];
                if (dto.Explorada[indice])
                    mapa.MarcarExplorada(x, y);
            }
        }

        var jogador = new Jogador(new Posicao(dto.JogadorX, dto.JogadorY), dto.VidaMaximaJogador)
        {
            Vida = dto.VidaJogador,
            Ouro = dto.OuroJogador
        };

        jogador.Mochila.AddRange(dto.Mochila.Select(DeSalvo));
        if (dto.Capacete is { } dc) jogador.Capacete = DeSalvo(dc);
        if (dto.Peitoral is { } dp) jogador.Peitoral = DeSalvo(dp);
        if (dto.Pernas is { } dpr) jogador.Pernas = DeSalvo(dpr);
        if (dto.Botas is { } db) jogador.Botas = DeSalvo(db);

        var estado = new EstadoDoJogo
        {
            _largura = dto.Largura,
            _altura = dto.Altura,
            _random = new Random(),
            Mapa = mapa,
            Salas = Array.Empty<Sala>(),
            Jogador = jogador,
            Andar = dto.Andar,
            _itensNoChao = dto.ItensNoChao.ToDictionary(i => new Posicao(i.X, i.Y), i => DeSalvo(i.Item))
        };

        estado._mensagens.AddRange(dto.Mensagens);
        estado.AdicionarMensagem("Jogo carregado.");
        estado.AtualizarVisibilidade();
        return estado;
    }

    private static ItemSalvo ParaSalvo(Item item) => new() { Nome = item.Nome, Tipo = item.Tipo, Valor = item.Valor };

    private static Item DeSalvo(ItemSalvo salvo) => new(salvo.Nome, salvo.Tipo, salvo.Valor);

    private (MapaDaMasmorra Mapa, IReadOnlyList<Sala> Salas) GerarNivel()
    {
        var gerador = new GeradorDeMasmorra();
        var (mapa, salas, itens) = gerador.Gerar(_largura, _altura, _random);
        _itensNoChao = new Dictionary<Posicao, Item>(itens);
        return (mapa, salas);
    }

    private void Descer()
    {
        Andar++;
        (Mapa, Salas) = GerarNivel();
        Jogador.Posicao = Salas.Count > 0 ? Salas[0].Centro : new Posicao(_largura / 2, _altura / 2);
        AdicionarMensagem($"Você desce para o andar {Andar}.");
    }

    private void ColetarItem(Posicao posicao)
    {
        if (!_itensNoChao.TryGetValue(posicao, out var item))
            return;

        _itensNoChao.Remove(posicao);
        Mapa[posicao.X, posicao.Y] = TipoDeCelula.Chao;
        Jogador.Mochila.Add(item);
        AdicionarMensagem($"Você pega {item.Nome}.");
    }

    private void UsarConsumivel(Item item)
    {
        var antes = Jogador.Vida;
        Jogador.Vida = Math.Min(Jogador.VidaMaxima, Jogador.Vida + item.Valor);
        var curado = Jogador.Vida - antes;

        AdicionarMensagem(curado > 0
            ? $"Você usa {item.Nome} e recupera {curado} de vida."
            : $"Você usa {item.Nome}, mas já estava com a vida cheia.");
    }

    private void AtualizarVisibilidade()
    {
        CelulasVisiveis = CampoDeVisao.Calcular(Mapa, Jogador.Posicao, RaioDeVisao);
        foreach (var celula in CelulasVisiveis)
            Mapa.MarcarExplorada(celula.X, celula.Y);
    }

    private void AdicionarMensagem(string texto)
    {
        _mensagens.Add(texto);
        if (_mensagens.Count > MaximoDeMensagens)
            _mensagens.RemoveAt(0);
    }
}
