using System.Text.Json;
using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Geracao;
using Mausmorras.Nucleo.Mapa;
using Mausmorras.Nucleo.Persistencia;

namespace Mausmorras.Nucleo.Jogo;

public sealed class EstadoDoJogo
{
    private const int RaioDeVisao = 10;
    private const int MaximoDeMensagens = 200;

    private readonly List<string> _mensagens = new();

    private int _largura;
    private int _altura;
    private Random _random = new();

    public MapaDaMasmorra Mapa { get; private set; } = null!;
    public IReadOnlyList<Sala> Salas { get; private set; } = Array.Empty<Sala>();
    public Jogador Jogador { get; private set; } = null!;
    public int Andar { get; private set; } = 1;
    public IReadOnlySet<Posicao> CelulasVisiveis { get; private set; } = new HashSet<Posicao>();
    public IReadOnlyList<string> Mensagens => _mensagens;

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
        var alvo = Jogador.Posicao + delta;
        if (!Mapa.EhCaminhavel(alvo))
            return false;

        Jogador.Posicao = alvo;

        if (Mapa[alvo.X, alvo.Y] == TipoDeCelula.Grama)
            Mapa[alvo.X, alvo.Y] = TipoDeCelula.Chao;

        if (Mapa[alvo.X, alvo.Y] == TipoDeCelula.Escada)
            Descer();

        AtualizarVisibilidade();
        return true;
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
            Celulas = new int[Mapa.Largura * Mapa.Altura],
            Explorada = new bool[Mapa.Largura * Mapa.Altura],
            Mensagens = _mensagens.ToList()
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

        var estado = new EstadoDoJogo
        {
            _largura = dto.Largura,
            _altura = dto.Altura,
            _random = new Random(),
            Mapa = mapa,
            Salas = Array.Empty<Sala>(),
            Jogador = new Jogador(new Posicao(dto.JogadorX, dto.JogadorY)),
            Andar = dto.Andar
        };

        estado._mensagens.AddRange(dto.Mensagens);
        estado.AdicionarMensagem("Jogo carregado.");
        estado.AtualizarVisibilidade();
        return estado;
    }

    private (MapaDaMasmorra Mapa, IReadOnlyList<Sala> Salas) GerarNivel()
    {
        var gerador = new GeradorDeMasmorra();
        return gerador.Gerar(_largura, _altura, _random);
    }

    private void Descer()
    {
        Andar++;
        (Mapa, Salas) = GerarNivel();
        Jogador.Posicao = Salas.Count > 0 ? Salas[0].Centro : new Posicao(_largura / 2, _altura / 2);
        AdicionarMensagem($"Você desce para o andar {Andar}.");
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
