using System.Text.Json;
using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Geracao;
using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;
using Mausmorras.Nucleo.Persistencia;

namespace Mausmorras.Nucleo.Jogo;

public sealed partial class EstadoDoJogo
{
    public void Salvar(string caminho)
    {
        var dto = new EstadoSalvo
        {
            Largura = Mapa.Largura,
            Altura = Mapa.Altura,
            Andar = Andar,
            Turno = _turno,
            Modo = Modo,
            Madeira = Madeira,
            Personagens = _personagens.Select(ParaSalvoPersonagem).ToList(),
            IndiceSelecionado = _indiceSelecionado,
            Celulas = new int[Mapa.Largura * Mapa.Altura],
            Explorada = new bool[Mapa.Largura * Mapa.Altura],
            Mensagens = _mensagens.ToList(),
            ItensNoChao = _itensNoChao
                .Select(kv => new ItemNoChaoSalvo { X = kv.Key.X, Y = kv.Key.Y, Item = ParaSalvo(kv.Value) })
                .ToList(),
            Bichos = _bichos.Select(b => new BichoSalvo { X = b.Posicao.X, Y = b.Posicao.Y }).ToList(),
            FogueirasAtivas = _fogueirasAtivas.Select(f => new FogueiraAtivaSalva { X = f.Posicao.X, Y = f.Posicao.Y, TurnoDeExpiracao = f.TurnoDeExpiracao }).ToList(),
            PrimeiroAbrigoConstruido = _primeiroAbrigoConstruido,
            ColheitasPendentes = _proximaColheitaDisponivel.Select(kv => new ColheitaPendenteSalva { X = kv.Key.X, Y = kv.Key.Y, TurnoDisponivel = kv.Value }).ToList(),
            Bau = _bau.Select(ParaSalvo).ToList()
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

        List<Personagem> personagens;
        int indiceSelecionado;

        if (dto.Personagens.Count > 0)
        {
            personagens = dto.Personagens.Select(DeSalvoPersonagem).ToList();
            indiceSelecionado = Math.Clamp(dto.IndiceSelecionado, 0, personagens.Count - 1);
        }
        else
        {
            // save antigo (pre-multi-personagem): migra o unico personagem dos campos achatados legados
            var legado = new Personagem(new Posicao(dto.JogadorX, dto.JogadorY), dto.VidaMaximaJogador)
            {
                Vida = dto.VidaJogador,
                Ouro = dto.OuroJogador
            };

            legado.Mochila.AddRange(dto.Mochila.Select(DeSalvo));
            if (dto.Capacete is { } dc) legado.Capacete = DeSalvo(dc);
            if (dto.Peitoral is { } dp) legado.Peitoral = DeSalvo(dp);
            if (dto.Pernas is { } dpr) legado.Pernas = DeSalvo(dpr);
            if (dto.Botas is { } db) legado.Botas = DeSalvo(db);

            personagens = new List<Personagem> { legado };
            indiceSelecionado = 0;
        }

        var estado = new EstadoDoJogo
        {
            _largura = dto.Largura,
            _altura = dto.Altura,
            _random = new Random(),
            _turno = dto.Turno,
            Modo = dto.Modo,
            // ordem de fallback: estoque compartilhado novo -> formato antigo de personagem unico ->
            // formato intermediario (madeira por personagem, ja removido de PersonagemSalvo mas ainda
            // capturado via MadeiraLegado so pra nao perder o valor salvo de quem carregar esse save
            Madeira = dto.Madeira != 0 ? dto.Madeira : dto.MadeiraJogador != 0 ? dto.MadeiraJogador : dto.Personagens.Sum(p => p.MadeiraLegado),
            Mapa = mapa,
            Salas = Array.Empty<Sala>(),
            _personagens = personagens,
            _indiceSelecionado = indiceSelecionado,
            Andar = dto.Andar,
            _itensNoChao = dto.ItensNoChao.ToDictionary(i => new Posicao(i.X, i.Y), i => DeSalvo(i.Item)),
            _bichos = dto.Bichos.Select(b => new Bicho(new Posicao(b.X, b.Y))).ToList()
        };

        if (dto.Andar == 0)
        {
            estado._mapaDaVila = mapa;
            var spawnDeRetorno = estado.Personagem.Posicao;
            estado._salasDaVila = new[] { new Sala(spawnDeRetorno.X, spawnDeRetorno.Y, 1, 1) };
        }

        if (estado._mapaDaVila is not null)
        {
            estado._existeCasaNaVila = ExisteCasaNoMapa(estado._mapaDaVila);
            estado._fogueirasAtivas.AddRange(dto.FogueirasAtivas.Select(f => (new Posicao(f.X, f.Y), f.TurnoDeExpiracao)));

            // saves de antes dessa feature podem ter fogueira construída manualmente sem estar na lista --
            // sem isso ela nunca expiraria nem contaria pra iluminação, já que ambas dependem de _fogueirasAtivas
            var posicoesConhecidas = estado._fogueirasAtivas.Select(f => f.Posicao).ToHashSet();
            for (var x = 0; x < estado._mapaDaVila.Largura; x++)
                for (var y = 0; y < estado._mapaDaVila.Altura; y++)
                    if (estado._mapaDaVila[x, y] == TipoDeCelula.Fogueira && !posicoesConhecidas.Contains(new Posicao(x, y)))
                        estado._fogueirasAtivas.Add((new Posicao(x, y), estado._turno + DuracaoDaFogueira));

            // saves de antes desse campo existir nao tem PrimeiroAbrigoConstruido salvo (vem false) --
            // se ja existe casa ou fogueira nesse ponto, o bootstrap claramente ja passou
            estado._primeiroAbrigoConstruido = dto.PrimeiroAbrigoConstruido || estado._existeCasaNaVila || estado._fogueirasAtivas.Count > 0;

            foreach (var c in dto.ColheitasPendentes)
                estado._proximaColheitaDisponivel[new Posicao(c.X, c.Y)] = c.TurnoDisponivel;

            estado._bau.AddRange(dto.Bau.Select(DeSalvo));
        }

        estado._mensagens.AddRange(dto.Mensagens);
        estado.AdicionarMensagem("Jogo carregado.");
        estado.AtualizarVisibilidade();
        return estado;
    }

    private static ItemSalvo ParaSalvo(Item item) => new() { Nome = item.Nome, Tipo = item.Tipo, Valor = item.Valor };

    private static Item DeSalvo(ItemSalvo salvo) => new(salvo.Nome, salvo.Tipo, salvo.Valor);

    private static bool ExisteCasaNoMapa(MapaDaMasmorra mapa)
    {
        for (var x = 0; x < mapa.Largura; x++)
            for (var y = 0; y < mapa.Altura; y++)
                if (mapa[x, y] == TipoDeCelula.PisoDaCasa)
                    return true;

        return false;
    }

    private static PersonagemSalvo ParaSalvoPersonagem(Personagem p) => new()
    {
        X = p.Posicao.X,
        Y = p.Posicao.Y,
        Vida = p.Vida,
        VidaMaxima = p.VidaMaxima,
        Ouro = p.Ouro,
        Fome = p.Fome,
        Temperatura = p.Temperatura,
        Sono = p.Sono,
        Mochila = p.Mochila.Select(ParaSalvo).ToList(),
        Capacete = p.Capacete is { } c ? ParaSalvo(c) : null,
        Peitoral = p.Peitoral is { } pe ? ParaSalvo(pe) : null,
        Pernas = p.Pernas is { } pr ? ParaSalvo(pr) : null,
        Botas = p.Botas is { } b ? ParaSalvo(b) : null
    };

    private static Personagem DeSalvoPersonagem(PersonagemSalvo s)
    {
        var p = new Personagem(new Posicao(s.X, s.Y), s.VidaMaxima) { Vida = s.Vida, Ouro = s.Ouro, Fome = s.Fome, Temperatura = s.Temperatura, Sono = s.Sono };
        p.Mochila.AddRange(s.Mochila.Select(DeSalvo));
        if (s.Capacete is { } c) p.Capacete = DeSalvo(c);
        if (s.Peitoral is { } pe) p.Peitoral = DeSalvo(pe);
        if (s.Pernas is { } pr) p.Pernas = DeSalvo(pr);
        if (s.Botas is { } b) p.Botas = DeSalvo(b);
        return p;
    }
}
