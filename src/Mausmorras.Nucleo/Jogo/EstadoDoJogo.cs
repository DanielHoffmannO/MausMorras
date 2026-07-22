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
    private const int LarguraDaVila = 70;
    private const int AlturaDaVila = 35;
    private const int TurnosPorMetadeDoDia = 60;
    private const int RaioDeVisaoNoiteNaVila = 6;
    private const int MadeiraPorArvore = 5;
    private const int TamanhoDaCasa = 5;
    private const double ChanceDeRebrotaPorCelula = 0.01;

    private readonly List<string> _mensagens = new();
    private Dictionary<Posicao, Item> _itensNoChao = new();
    private MapaDaMasmorra? _mapaDaVila;
    private IReadOnlyList<Sala> _salasDaVila = Array.Empty<Sala>();
    private Posicao _ultimaDirecao = Direcao.Sul;
    private int _turno;

    private int _largura;
    private int _altura;
    private Random _random = new();

    public MapaDaMasmorra Mapa { get; private set; } = null!;
    public IReadOnlyList<Sala> Salas { get; private set; } = Array.Empty<Sala>();
    public Jogador Jogador { get; private set; } = null!;
    public int Andar { get; private set; } = 1;
    public TipoDeLocal LocalAtual => Andar == 0 ? TipoDeLocal.Vila : TipoDeLocal.Masmorra;
    public IReadOnlySet<Posicao> CelulasVisiveis { get; private set; } = new HashSet<Posicao>();
    public IReadOnlyList<string> Mensagens => _mensagens;
    public bool Morto => Jogador.Vida <= 0;
    public int Turno => _turno;
    public bool EhDia => (_turno / TurnosPorMetadeDoDia) % 2 == 0;

    public EstadoDoJogo(int largura, int altura, int? seed = null)
    {
        _largura = largura;
        _altura = altura;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();

        var inicio = PrepararVila();
        Jogador = new Jogador(inicio);
        AdicionarMensagem("Você acorda na vila.");
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

        if (Mapa[alvo.X, alvo.Y] == TipoDeCelula.Arvore)
        {
            CortarArvore(alvo);
            AvancarTurno();
            return true;
        }

        if (!Mapa.EhCaminhavel(alvo))
            return false;

        Jogador.Posicao = alvo;
        _ultimaDirecao = delta;

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

            case TipoDeCelula.EntradaMasmorra:
                EntrarNaMasmorra();
                break;

            case TipoDeCelula.SaidaParaVila:
                EntrarNaVila();
                break;
        }

        AvancarTurno();
        return true;
    }

    public bool TentarConstruir()
    {
        if (Morto || LocalAtual != TipoDeLocal.Vila)
            return false;

        var area = CalcularAreaDaCasa(Jogador.Posicao, _ultimaDirecao);

        if (!AreaLivreParaConstrucao(area))
        {
            AdicionarMensagem("Não há espaço livre suficiente para construir aqui.");
            return false;
        }

        var custo = area.Largura * area.Altura;
        if (Jogador.Madeira < custo)
        {
            AdicionarMensagem($"Madeira insuficiente para construir (precisa de {custo}).");
            return false;
        }

        ConstruirCasa(area, Jogador.Posicao + _ultimaDirecao);
        Jogador.Madeira -= custo;
        AdicionarMensagem("Você constrói uma casa.");
        AvancarTurno();
        return true;
    }

    private static Sala CalcularAreaDaCasa(Posicao jogador, Posicao direcao)
    {
        var metade = TamanhoDaCasa / 2;

        if (direcao == Direcao.Sul)
            return new Sala(jogador.X - metade, jogador.Y + 1, TamanhoDaCasa, TamanhoDaCasa);

        if (direcao == Direcao.Norte)
            return new Sala(jogador.X - metade, jogador.Y - TamanhoDaCasa, TamanhoDaCasa, TamanhoDaCasa);

        if (direcao == Direcao.Leste)
            return new Sala(jogador.X + 1, jogador.Y - metade, TamanhoDaCasa, TamanhoDaCasa);

        return new Sala(jogador.X - TamanhoDaCasa, jogador.Y - metade, TamanhoDaCasa, TamanhoDaCasa);
    }

    private bool AreaLivreParaConstrucao(Sala area)
    {
        for (var x = area.X; x < area.X + area.Largura; x++)
            for (var y = area.Y; y < area.Y + area.Altura; y++)
                if (!Mapa.DentroDosLimites(x, y) || Mapa[x, y] != TipoDeCelula.Grama)
                    return false;

        return true;
    }

    private void ConstruirCasa(Sala area, Posicao porta)
    {
        for (var x = area.X; x < area.X + area.Largura; x++)
            for (var y = area.Y; y < area.Y + area.Altura; y++)
                Mapa[x, y] = TipoDeCelula.Casa;

        for (var x = area.X + 1; x < area.X + area.Largura - 1; x++)
            for (var y = area.Y + 1; y < area.Y + area.Altura - 1; y++)
                Mapa[x, y] = TipoDeCelula.Chao;

        Mapa[porta.X, porta.Y] = TipoDeCelula.Porta;
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
            MadeiraJogador = Jogador.Madeira,
            Turno = _turno,
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
            Ouro = dto.OuroJogador,
            Madeira = dto.MadeiraJogador
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
            _turno = dto.Turno,
            Mapa = mapa,
            Salas = Array.Empty<Sala>(),
            Jogador = jogador,
            Andar = dto.Andar,
            _itensNoChao = dto.ItensNoChao.ToDictionary(i => new Posicao(i.X, i.Y), i => DeSalvo(i.Item))
        };

        if (dto.Andar == 0)
        {
            estado._mapaDaVila = mapa;
            estado._salasDaVila = new[] { new Sala(dto.JogadorX, dto.JogadorY, 1, 1) };
        }

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

    private Posicao PrepararVila()
    {
        if (_mapaDaVila is null)
        {
            var (mapaGerado, salasGeradas, _) = new GeradorDeVila().Gerar(LarguraDaVila, AlturaDaVila, _random);
            _mapaDaVila = mapaGerado;
            _salasDaVila = salasGeradas;
        }

        Mapa = _mapaDaVila;
        Salas = _salasDaVila;
        Andar = 0;
        _itensNoChao = new Dictionary<Posicao, Item>();
        return _salasDaVila.Count > 0 ? _salasDaVila[0].Centro : new Posicao(LarguraDaVila / 2, AlturaDaVila / 2);
    }

    private void EntrarNaVila()
    {
        Jogador.Posicao = PrepararVila();
        AdicionarMensagem("Você retorna à vila.");
    }

    private void EntrarNaMasmorra()
    {
        Andar = 1;
        (Mapa, Salas) = GerarNivel();
        var spawn = Salas.Count > 0 ? Salas[0].Centro : new Posicao(_largura / 2, _altura / 2);
        Mapa[spawn.X, spawn.Y] = TipoDeCelula.SaidaParaVila;
        Jogador.Posicao = spawn;
        AdicionarMensagem("Você entra na masmorra escura.");
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

    private void CortarArvore(Posicao posicao)
    {
        Mapa[posicao.X, posicao.Y] = TipoDeCelula.Grama;
        Jogador.Madeira += MadeiraPorArvore;
        AdicionarMensagem($"Você corta a árvore e ganha {MadeiraPorArvore} de madeira.");
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

    private void AvancarTurno()
    {
        var eraDia = EhDia;
        _turno++;

        if (LocalAtual == TipoDeLocal.Vila && eraDia != EhDia)
        {
            AdicionarMensagem(EhDia ? "O dia amanhece sobre a vila." : "A noite cai sobre a vila.");
            if (EhDia)
                RegenerarArvores();
        }

        AtualizarVisibilidade();
    }

    private void RegenerarArvores()
    {
        if (_mapaDaVila is null)
            return;

        for (var x = 0; x < _mapaDaVila.Largura; x++)
        {
            for (var y = 0; y < _mapaDaVila.Altura; y++)
            {
                if (_mapaDaVila[x, y] == TipoDeCelula.Grama && _random.NextDouble() < ChanceDeRebrotaPorCelula)
                    _mapaDaVila[x, y] = TipoDeCelula.Arvore;
            }
        }
    }

    private void AtualizarVisibilidade()
    {
        if (LocalAtual == TipoDeLocal.Vila && EhDia)
        {
            var visiveis = new HashSet<Posicao>();
            for (var x = 0; x < Mapa.Largura; x++)
            {
                for (var y = 0; y < Mapa.Altura; y++)
                {
                    var posicao = new Posicao(x, y);
                    visiveis.Add(posicao);
                    Mapa.MarcarExplorada(x, y);
                }
            }

            CelulasVisiveis = visiveis;
            return;
        }

        var raio = LocalAtual == TipoDeLocal.Vila ? RaioDeVisaoNoiteNaVila : RaioDeVisao;
        CelulasVisiveis = CampoDeVisao.Calcular(Mapa, Jogador.Posicao, raio);
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
