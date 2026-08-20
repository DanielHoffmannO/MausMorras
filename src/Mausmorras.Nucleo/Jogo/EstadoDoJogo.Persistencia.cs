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
            Ouro = Ouro,
            Personagens = _personagens.Select(ParaSalvoPersonagem).ToList(),
            IndiceSelecionado = _indiceSelecionado,
            Mensagens = _mensagens.ToList(),
            ItensNoChao = _itensNoChao
                .Select(kv => new ItemNoChaoSalvo { X = kv.Key.X, Y = kv.Key.Y, Item = ParaSalvo(kv.Value) })
                .ToList(),
            Bichos = _bichos.Select(b => new BichoSalvo { X = b.Posicao.X, Y = b.Posicao.Y }).ToList(),
            Monstros = _monstros.Select(m => new MonstroSalvo { X = m.Posicao.X, Y = m.Posicao.Y, Vida = m.Vida, VidaMaxima = m.VidaMaxima, Dano = m.Dano, Tipo = m.Tipo }).ToList(),
            FogueirasAtivas = _fogueirasAtivas.Select(f => new FogueiraAtivaSalva { X = f.Posicao.X, Y = f.Posicao.Y, TurnoDeExpiracao = f.TurnoDeExpiracao }).ToList(),
            PrimeiroAbrigoConstruido = _primeiroAbrigoConstruido,
            NumeroDeCasas = _numeroDeCasas,
            ColheitasPendentes = _proximaColheitaDisponivel.Select(kv => new ColheitaPendenteSalva { X = kv.Key.X, Y = kv.Key.Y, TurnoDisponivel = kv.Value }).ToList(),
            Bau = _bau.Select(ParaSalvo).ToList()
        };

        (dto.Celulas, dto.Explorada) = SerializarMapa(Mapa);

        // a vila e o unico mapa que sobrevive por toda a sessao (a masmorra sempre regenera ao
        // entrar/descer) -- precisa ser serializada mesmo quando nao e o mapa "atual"
        if (_mapaDaVila is not null)
        {
            (dto.CelulasDaVila, dto.ExploradaDaVila) = ReferenceEquals(_mapaDaVila, Mapa)
                ? (dto.Celulas, dto.Explorada)
                : SerializarMapa(_mapaDaVila);
        }

        File.WriteAllText(caminho, JsonSerializer.Serialize(dto));
        AdicionarMensagem("Jogo salvo.");
    }

    public static EstadoDoJogo CarregarDe(string caminho)
    {
        var dto = JsonSerializer.Deserialize<EstadoSalvo>(File.ReadAllText(caminho))
                   ?? throw new InvalidDataException("Arquivo de save inválido.");

        var mapa = DesserializarMapa(dto.Largura, dto.Altura, dto.Celulas, dto.Explorada);

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
                Nome = NomesDisponiveis[Random.Shared.Next(NomesDisponiveis.Length)]
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
            // ordem de fallback (mesma pra Madeira e Ouro, ambos ja foram por-personagem antes de
            // virar estoque compartilhado): estoque novo -> formato antigo de personagem unico ->
            // formato intermediario (por personagem, ja removido do DTO atual mas ainda capturado
            // via campo legado so pra nao perder o valor salvo de quem carregar esse save)
            Madeira = ResgatarEstoqueLegado(dto.Madeira, dto.MadeiraJogador, dto.Personagens.Select(p => p.MadeiraLegado)),
            Ouro = ResgatarEstoqueLegado(dto.Ouro, dto.OuroJogador, dto.Personagens.Select(p => p.Ouro)),
            Mapa = mapa,
            Salas = Array.Empty<Sala>(),
            _personagens = personagens,
            _indiceSelecionado = indiceSelecionado,
            Andar = dto.Andar,
            _itensNoChao = dto.ItensNoChao.ToDictionary(i => new Posicao(i.X, i.Y), i => DeSalvo(i.Item)),
            _bichos = dto.Bichos.Select(b => new Bicho(new Posicao(b.X, b.Y))).ToList(),
            _monstros = dto.Monstros.Select(m => new Monstro(new Posicao(m.X, m.Y), m.VidaMaxima, m.Dano, m.Tipo) { Vida = m.Vida }).ToList()
        };

        if (dto.Andar == 0)
        {
            estado._mapaDaVila = mapa;
            var spawnDeRetorno = estado.Personagem.Posicao;
            estado._salasDaVila = new[] { new Sala(spawnDeRetorno.X, spawnDeRetorno.Y, 1, 1) };
        }
        else if (dto.CelulasDaVila.Length == LarguraDaVila * AlturaDaVila)
        {
            // save feito de dentro da masmorra -- antes disso a vila nunca era serializada nesse caso,
            // e uma vila inteiramente nova era gerada no carregamento seguinte (casas/fogueiras/bau perdidos)
            estado._mapaDaVila = DesserializarMapa(LarguraDaVila, AlturaDaVila, dto.CelulasDaVila, dto.ExploradaDaVila);
        }

        if (estado._mapaDaVila is not null)
        {
            estado._existeCasaNaVila = ExisteCasaNoMapa(estado._mapaDaVila);
            // saves de antes dessa feature (uma casa so, nao salvava contagem) nao tem NumeroDeCasas --
            // se ja existe casa nesse ponto, assume pelo menos 1 (nunca havia mais de uma antes disso)
            estado._numeroDeCasas = dto.NumeroDeCasas > 0 ? dto.NumeroDeCasas : estado._existeCasaNaVila ? 1 : 0;
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

    // estoque compartilhado -> formato antigo achatado -> soma por-personagem, nessa ordem de fallback
    private static int ResgatarEstoqueLegado(int atual, int legadoAchatado, IEnumerable<int> legadoPorPersonagem) =>
        atual != 0 ? atual : legadoAchatado != 0 ? legadoAchatado : legadoPorPersonagem.Sum();

    private static (int[] Celulas, bool[] Explorada) SerializarMapa(MapaDaMasmorra mapa)
    {
        var celulas = new int[mapa.Largura * mapa.Altura];
        var explorada = new bool[mapa.Largura * mapa.Altura];
        for (var x = 0; x < mapa.Largura; x++)
        {
            for (var y = 0; y < mapa.Altura; y++)
            {
                var indice = y * mapa.Largura + x;
                celulas[indice] = (int)mapa[x, y];
                explorada[indice] = mapa.FoiExplorada(x, y);
            }
        }

        return (celulas, explorada);
    }

    private static MapaDaMasmorra DesserializarMapa(int largura, int altura, int[] celulas, bool[] explorada)
    {
        var mapa = new MapaDaMasmorra(largura, altura);
        for (var x = 0; x < largura; x++)
        {
            for (var y = 0; y < altura; y++)
            {
                var indice = y * largura + x;
                mapa[x, y] = (TipoDeCelula)celulas[indice];
                if (explorada[indice])
                    mapa.MarcarExplorada(x, y);
            }
        }

        return mapa;
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
        Nome = p.Nome,
        X = p.Posicao.X,
        Y = p.Posicao.Y,
        Vida = p.Vida,
        VidaMaxima = p.VidaMaxima,
        Fome = p.Fome,
        Temperatura = p.Temperatura,
        Sono = p.Sono,
        EhCrianca = p.EhCrianca,
        Idade = p.Idade,
        Traco = (int)p.Traco,
        AversaoAoFrio = p.AversaoAoFrio,
        AversaoAFome = p.AversaoAFome,
        AversaoAoSono = p.AversaoAoSono,
        TurnoDeRetornoDaExpedicao = p.TurnoDeRetornoDaExpedicao,
        AversaoAExpedicao = p.AversaoAExpedicao,
        Mochila = p.Mochila.Select(ParaSalvo).ToList(),
        Capacete = p.Capacete is { } c ? ParaSalvo(c) : null,
        Peitoral = p.Peitoral is { } pe ? ParaSalvo(pe) : null,
        Pernas = p.Pernas is { } pr ? ParaSalvo(pr) : null,
        Botas = p.Botas is { } b ? ParaSalvo(b) : null,
        Arma = p.Arma is { } a ? ParaSalvo(a) : null
    };

    private static Personagem DeSalvoPersonagem(PersonagemSalvo s)
    {
        var p = new Personagem(new Posicao(s.X, s.Y), s.VidaMaxima)
        {
            Nome = s.Nome ?? NomesDisponiveis[Random.Shared.Next(NomesDisponiveis.Length)],
            Vida = s.Vida, Fome = s.Fome, Temperatura = s.Temperatura, Sono = s.Sono,
            EhCrianca = s.EhCrianca, Idade = s.Idade,
            Traco = (TracoDePersonalidade)s.Traco, AversaoAoFrio = s.AversaoAoFrio, AversaoAFome = s.AversaoAFome, AversaoAoSono = s.AversaoAoSono,
            TurnoDeRetornoDaExpedicao = s.TurnoDeRetornoDaExpedicao, AversaoAExpedicao = s.AversaoAExpedicao
        };
        p.Mochila.AddRange(s.Mochila.Select(DeSalvo));
        if (s.Capacete is { } c) p.Capacete = DeSalvo(c);
        if (s.Peitoral is { } pe) p.Peitoral = DeSalvo(pe);
        if (s.Pernas is { } pr) p.Pernas = DeSalvo(pr);
        if (s.Botas is { } b) p.Botas = DeSalvo(b);
        if (s.Arma is { } a) p.Arma = DeSalvo(a);
        return p;
    }
}
