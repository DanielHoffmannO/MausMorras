using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Geracao;
using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public sealed partial class EstadoDoJogo
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
    private const int DistanciaDaCasaAoPersonagem = 2; // 1 bloco de folga + a parede da casa
    private const double ChanceDeRebrotaPorCelula = 0.01;
    public const int FomeMaxima = 300; // ~2,5 dias de jogo (1 dia = 120 turnos)
    private const int FomePorTurno = 1;
    private const int DanoPorFomeMaxima = 1;
    public const int TemperaturaIdeal = 33;
    public const int TemperaturaCritica = 15; // abaixo disso, comeca a tomar dano
    private const int TemperaturaAmbienteDia = 32; // de dia, quase nao ha frio de verdade
    private const int TemperaturaAmbienteNoite = 10; // de noite, esfria de verdade
    private const int TemperaturaAmbienteCasa = 30;
    private const int TemperaturaAmbienteFogueira = 40;
    private const int TaxaDeTrocaDeCalor = 2; // quanto a temperatura anda por turno em direcao ao ambiente
    private const int DanoPorTemperaturaCritica = 1;
    private const double LimiarTemperaturaParaBuscarAbrigo = 0.35;
    private const int RaioDaFogueira = 2; // raio de efeito, nao precisa estar EM cima
    private const int CustoDaFogueira = 5; // uma arvore so -- a temperatura cai rapido demais (18 graus / 2 por turno) pra exigir duas
    private const int RaioDeVisaoDaFogueira = 4; // menor que a visão noturna das pessoas (6) -- ilumina, não enxerga tudo
    private const int AntecedenciaParaVoltarAntesDoAnoitecer = 10; // turnos de folga antes do fim do dia
    private const double SeveridadeDoAnoitecerIminente = 0.2; // so vence fome/sono ainda bem baixos, nao interrompe algo em andamento
    private const int TemperaturaParaFogueiraDuranteACaca = 31; // logo abaixo do dia normal (32) -- dispara quase no instante em que a noite comeca a esfriar, maximizando o tempo de reacao
    private const int DuracaoDaFogueira = 150; // um pouco mais que um ciclo dia/noite completo (120 turnos)

    public const int SonoMaximo = 300;
    private const int SonoPorTurno = 1;
    private const int DanoPorSonoMaximo = 1;
    private const double LimiarSonoParaDormir = 0.2; // era 0.35 -- mais baixo que fome/frio de proposito, ja que dormir so alivia em casa, sem equivalente portatil tipo a fogueira, entao precisa de mais folga pra caminhada de volta
    private const int AlivioDoSonoAoDescansar = 5;
    private const int VidaMaximaMinimaFundador = 22; // era 18 -- mais margem de sobrevivencia
    private const int VidaMaximaMaximaFundador = 28; // era 22
    private const double LimiarFomeParaBuscarComida = 0.35; // era 0.5 -- age mais cedo, sobra mais tempo de volta
    private const int PopulacaoAlvoDeBichos = 6;
    private const int RaioDeAlcanceDoBicho = 3; // distancia maxima da borda do mapa
    private const int ValorDaCarne = 80; // reduz esse tanto de Fome ao comer

    private static readonly Posicao[] Direcoes = { Direcao.Norte, Direcao.Sul, Direcao.Leste, Direcao.Oeste };

    private readonly List<string> _mensagens = new();
    private Dictionary<Posicao, Item> _itensNoChao = new();
    private MapaDaMasmorra? _mapaDaVila;
    private IReadOnlyList<Sala> _salasDaVila = Array.Empty<Sala>();
    private Posicao _ultimaDirecao = Direcao.Sul;
    private int _turno;
    private bool _vilaTotalmenteExplorada;
    private bool _existeCasaNaVila;
    private bool _primeiroAbrigoConstruido;
    private readonly List<(Posicao Posicao, int TurnoDeExpiracao)> _fogueirasAtivas = new();

    private int _largura;
    private int _altura;
    private Random _random = new();

    private List<Personagem> _personagens = new();
    private int _indiceSelecionado;
    private List<Bicho> _bichos = new();

    public MapaDaMasmorra Mapa { get; private set; } = null!;
    public IReadOnlyList<Sala> Salas { get; private set; } = Array.Empty<Sala>();
    public Personagem Personagem => _personagens[_indiceSelecionado];
    public IReadOnlyList<Personagem> Personagens => _personagens;
    public int IndiceSelecionado => _indiceSelecionado;
    public IEnumerable<Personagem> PersonagensNoLocalAtual =>
        LocalAtual == TipoDeLocal.Vila ? _personagens : new[] { Personagem };
    public IReadOnlyList<Bicho> BichosNoLocalAtual => LocalAtual == TipoDeLocal.Vila ? _bichos : Array.Empty<Bicho>();
    public int Andar { get; private set; } = 1;
    public TipoDeLocal LocalAtual => Andar == 0 ? TipoDeLocal.Vila : TipoDeLocal.Masmorra;
    public IReadOnlySet<Posicao> CelulasVisiveis { get; private set; } = new HashSet<Posicao>();
    public bool TodosVisiveis { get; private set; }
    public IReadOnlyList<string> Mensagens => _mensagens;
    public bool Morto => Personagem.Vida <= 0;
    public int Turno => _turno;
    public bool EhDia => (_turno / TurnosPorMetadeDoDia) % 2 == 0;
    public ModoDeJogo Modo { get; private set; } = ModoDeJogo.Jogando;

    public EstadoDoJogo(int largura, int altura, int? seed = null)
    {
        _largura = largura;
        _altura = altura;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();

        var spawn = PrepararVila();

        // vida maxima nunca pode empatar entre os dois: como a fome sobe igual pra todo
        // mundo, um empate faria os dois morrerem no mesmo turno, sem ninguem vivo pra
        // TransferirControleAoMorrer passar o controle adiante
        var vidaMaximaFundador1 = VidaMaximaAleatoria();
        int vidaMaximaFundador2;
        do
        {
            vidaMaximaFundador2 = VidaMaximaAleatoria();
        } while (vidaMaximaFundador2 == vidaMaximaFundador1);

        _personagens.Add(new Personagem(spawn, vidaMaximaFundador1));
        _personagens.Add(new Personagem(spawn + Direcao.Leste, vidaMaximaFundador2));
        _indiceSelecionado = 0;

        for (var i = 0; i < PopulacaoAlvoDeBichos; i++)
            TentarNascerBicho();

        AdicionarMensagem("Você acorda na vila.");
        AtualizarVisibilidade();
    }

    private EstadoDoJogo()
    {
    }

    private int VidaMaximaAleatoria() => _random.Next(VidaMaximaMinimaFundador, VidaMaximaMaximaFundador + 1);

    public bool SelecionarProximoPersonagem()
    {
        if (LocalAtual != TipoDeLocal.Vila)
            return false;

        if (_personagens.Count(p => p.Vida > 0) <= 1)
            return false;

        var indice = _indiceSelecionado;
        do
        {
            indice = (indice + 1) % _personagens.Count;
        } while (_personagens[indice].Vida <= 0);

        _indiceSelecionado = indice;
        AdicionarMensagem("Você assume o controle de outra pessoa.");
        AtualizarVisibilidade();
        return true;
    }

    public void AlternarModo() =>
        Modo = Modo == ModoDeJogo.Jogando ? ModoDeJogo.Observador : ModoDeJogo.Jogando;

    public void AvancarTurno()
    {
        var eraDia = EhDia;
        _turno++;

        if (LocalAtual == TipoDeLocal.Vila && eraDia != EhDia)
        {
            AdicionarMensagem(EhDia ? "O dia amanhece sobre a vila." : "A noite cai sobre a vila.");
            if (EhDia)
            {
                RegenerarArvores();
                TentarNascerBicho();
            }
        }

        PensarPersonagensAutonomos();
        // a caca precisa ser checada ANTES do bicho se mover: se checasse depois, um bicho que
        // acabou de ser pisado por um personagem poderia escapar no mesmo turno so por ter
        // sorteado um passo pra longe antes da checagem rodar
        VerificarCacaEncontros();
        MoverBichos();
        AtualizarNecessidade(p => p.Fome, (p, v) => p.Fome = v, FomeMaxima, FomePorTurno, DanoPorFomeMaxima, "está faminta", "morreu de fome");
        AtualizarNecessidade(p => p.Sono, (p, v) => p.Sono = v, SonoMaximo, SonoPorTurno, DanoPorSonoMaximo, "está com sono", "morreu de exaustão", AlivioDoSono);
        AtualizarTemperatura();
        AtualizarFogueiras();
        TransferirControleAoMorrer();
        AtualizarVisibilidade();
    }

    private void TransferirControleAoMorrer()
    {
        if (Personagem.Vida > 0)
            return;

        var indiceVivo = _personagens.FindIndex(p => p.Vida > 0);
        if (indiceVivo < 0)
            return; // ninguém vivo — fim de jogo fica pra outra fase

        if (LocalAtual != TipoDeLocal.Vila)
        {
            PrepararVila(); // troca Mapa/Salas/Andar pra vila; quem sobrou ja esta la, nao reatribui Posicao
            AdicionarMensagem("Seu controle retorna à vila.");
        }

        _indiceSelecionado = indiceVivo;
    }

    private void AtualizarTemperatura()
    {
        var ambienteBase = EhDia ? TemperaturaAmbienteDia : TemperaturaAmbienteNoite;

        for (var i = 0; i < _personagens.Count; i++)
        {
            var p = _personagens[i];
            if (p.Vida <= 0)
                continue;

            var ambiente = AmbienteEfetivo(p, ambienteBase);
            var mudanca = Math.Clamp(ambiente - p.Temperatura, -TaxaDeTrocaDeCalor, TaxaDeTrocaDeCalor);
            p.Temperatura += mudanca;

            if (p.Temperatura < TemperaturaCritica)
            {
                var vidaAntes = p.Vida;
                p.Vida = Math.Max(0, p.Vida - DanoPorTemperaturaCritica);
                if (vidaAntes > 0 && p.Vida == 0)
                    AdicionarMensagem($"{NomeDoAtor(p)} morreu de frio.");
            }
        }
    }

    private int AmbienteEfetivo(Personagem p, int ambienteBase)
    {
        var mapaRelevante = ReferenceEquals(p, Personagem) ? Mapa : _mapaDaVila;
        if (mapaRelevante is null)
            return ambienteBase;

        if (EstaPertoDeFogueira(mapaRelevante, p.Posicao))
            return TemperaturaAmbienteFogueira;

        if (mapaRelevante[p.Posicao.X, p.Posicao.Y] == TipoDeCelula.PisoDaCasa)
            return TemperaturaAmbienteCasa;

        return ambienteBase;
    }

    private bool EstaPertoDeFogueira(MapaDaMasmorra mapa, Posicao p)
    {
        for (var dx = -RaioDaFogueira; dx <= RaioDaFogueira; dx++)
        {
            for (var dy = -RaioDaFogueira; dy <= RaioDaFogueira; dy++)
            {
                var x = p.X + dx;
                var y = p.Y + dy;
                if (mapa.DentroDosLimites(x, y) && mapa[x, y] == TipoDeCelula.Fogueira)
                    return true;
            }
        }

        return false;
    }

    // o selecionado so tem posicao valida no mapa da vila se estiver de fato la;
    // se estiver na masmorra (possivel entrar em Observador de la dentro), as coordenadas
    // dele nao correspondem ao mapa da vila mesmo que numericamente coincidam com algo de la
    private bool EstaNaVila(Personagem p) => !ReferenceEquals(p, Personagem) || LocalAtual == TipoDeLocal.Vila;

    private void PensarPersonagensAutonomos()
    {
        if (_mapaDaVila is null)
            return;

        foreach (var p in _personagens)
        {
            if (p.Vida <= 0)
                continue;

            var ehControlado = ReferenceEquals(p, Personagem) && Modo == ModoDeJogo.Jogando;
            if (ehControlado || !EstaNaVila(p))
                continue;

            var fomeSeveridade = (double)p.Fome / FomeMaxima;
            var sonoSeveridade = (double)p.Sono / SonoMaximo;
            var temperaturaSeveridade = Math.Clamp((double)(TemperaturaIdeal - p.Temperatura) / (TemperaturaIdeal - TemperaturaCritica), 0, 1);

            var fomeUrgente = fomeSeveridade >= LimiarFomeParaBuscarComida;
            var frioUrgente = temperaturaSeveridade >= LimiarTemperaturaParaBuscarAbrigo;
            var sonoUrgente = sonoSeveridade >= LimiarSonoParaDormir;

            // mesmo sem nenhuma necessidade critica ainda, comecar a voltar pro abrigo com
            // antecedencia (em vez de so reagir depois que o frio ja esta severo) e o que da tempo
            // de sobra pra caminhada -- sem isso, quem esta longe demais quando a noite cai nunca
            // chega a tempo, nao importa quao cedo o frio em si dispara a busca por abrigo
            var turnosParaAnoitecer = TurnosPorMetadeDoDia - (_turno % TurnosPorMetadeDoDia);
            var anoitecerIminente = EhDia && turnosParaAnoitecer <= AntecedenciaParaVoltarAntesDoAnoitecer && !EstaProtegidoDoFrio(p);

            var severidadeVencedora = -1.0;
            Action? acaoVencedora = null;

            void Considerar(bool urgente, double severidade, Action acao)
            {
                if (urgente && severidade >= severidadeVencedora)
                {
                    severidadeVencedora = severidade;
                    acaoVencedora = acao;
                }
            }

            Considerar(fomeUrgente, fomeSeveridade, () => TentarResolverFome(p));
            Considerar(frioUrgente, temperaturaSeveridade, () => TentarBuscarAbrigo(p));
            // sono so pode vencer a prioridade se ja existir casa pra ir -- caso contrario TentarDormir
            // nao acha nenhum PisoDaCasa e nao faz nada, e se fome e sono empatarem no maximo (ambos
            // severidade 1.0) o sono venceria o empate pra sempre, travando ate a fome e a construcao
            // da casa (que so roda quando NENHUMA necessidade vence a prioridade nesse turno)
            Considerar(sonoUrgente && _existeCasaNaVila, sonoSeveridade, () => TentarDormir(p));
            Considerar(anoitecerIminente, SeveridadeDoAnoitecerIminente, () => TentarBuscarAbrigo(p));

            if (acaoVencedora is not null)
                acaoVencedora.Invoke();
            else if (!_existeCasaNaVila)
            {
                // uma vez que o primeiro abrigo qualquer (fogueira) ja tenha existido, a prioridade
                // vira definitivamente a casa permanente -- se isso reagisse ao estado ATUAL de
                // _fogueirasAtivas (que zera quando ela expira), toda vez que a fogueira apagasse
                // quem estivesse economizando pra casa desviaria a madeira acumulada pra reconstruir
                // a fogueira de novo, e a casa nunca teria a chance de juntar os 25 necessarios
                if (_primeiroAbrigoConstruido)
                    TentarObterMadeira(p, TamanhoDaCasa * TamanhoDaCasa, TentarConstruirAutonomamente);
                else
                    TentarObterMadeira(p, CustoDaFogueira, TentarConstruirFogueiraAutonomamente);
            }
            else if (!EstaProtegidoDoFrio(p))
            {
                // ocioso e longe de qualquer abrigo -- espalha uma fogueira aqui mesmo em vez de so
                // guardar madeira parada. Isso vai deixando fogueiras pelo caminho que o casal
                // realmente percorre (caça, sono), cobrindo areas que a casa unica nunca alcancaria
                TentarObterMadeira(p, CustoDaFogueira, TentarConstruirFogueiraAutonomamente);
            }
        }
    }

    private void TentarResolverFome(Personagem p)
    {
        var comida = p.Mochila.FirstOrDefault(it => it.Tipo == TipoDeItem.Comida);
        if (comida is not null)
        {
            ComerAlimento(p, comida);
            p.Mochila.Remove(comida);
            return;
        }

        // uma cacada pode levar pra bem longe de qualquer abrigo por muitos turnos seguidos, e a
        // temperatura despenca rapido demais (18 graus a 2/turno = so uns 9 turnos de folga) pra dar
        // tempo de reagir do zero -- se ja esta esfriando e desprotegido, desvia pra arvore mais
        // proxima e depois acende a fogueira ali mesmo, sem esperar a fome ceder prioridade pro frio
        if (p.Temperatura <= TemperaturaParaFogueiraDuranteACaca && !EstaProtegidoDoFrio(p))
        {
            if (p.Madeira >= CustoDaFogueira)
            {
                if (ConstruirFogueiraSePossivel(p))
                    return;
            }
            else
            {
                var arvoreAdjacente = ProcurarArvoreAdjacente(p.Posicao);
                if (arvoreAdjacente is { } arvore)
                {
                    CortarArvoreAutonomamente(p, arvore);
                    return;
                }

                var passoParaArvore = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, EstaAdjacenteAArvore);
                if (passoParaArvore is { } destinoArvore)
                {
                    p.Posicao = destinoArvore;
                    return;
                }
            }
        }

        var passo = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, pos => _bichos.Any(b => b.Posicao == pos));
        if (passo is { } destino)
            p.Posicao = destino;
    }

    private void TentarBuscarAbrigo(Personagem p)
    {
        if (EstaProtegidoDoFrio(p))
            return;

        // uma caminhada longa demais ate o abrigo mais proximo pode nunca terminar a tempo -- se
        // sobra madeira na mochila, uma fogueira bem onde a pessoa esta e sempre mais segura e mais
        // rapida do que apostar numa volta pra casa, entao vira a resposta padrao ao frio, nao um
        // ultimo recurso; a casa/vila tem varias fogueiras espalhadas com o tempo por causa disso
        if (p.Madeira >= CustoDaFogueira && ConstruirFogueiraSePossivel(p))
            return;

        var passo = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, pos =>
            _mapaDaVila![pos.X, pos.Y] == TipoDeCelula.PisoDaCasa || EstaPertoDeFogueira(_mapaDaVila, pos));
        if (passo is { } destino)
            p.Posicao = destino;
    }

    private bool EstaProtegidoDoFrio(Personagem p)
    {
        var mapaRelevante = ReferenceEquals(p, Personagem) ? Mapa : _mapaDaVila;
        if (mapaRelevante is null)
            return false;

        return EstaPertoDeFogueira(mapaRelevante, p.Posicao) || mapaRelevante[p.Posicao.X, p.Posicao.Y] == TipoDeCelula.PisoDaCasa;
    }

    private void TentarDormir(Personagem p)
    {
        var passo = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, pos => _mapaDaVila![pos.X, pos.Y] == TipoDeCelula.PisoDaCasa);
        if (passo is { } destino)
            p.Posicao = destino;
    }

    private void TentarObterMadeira(Personagem p, int custoNecessario, Action<Personagem> construir)
    {
        if (p.Madeira >= custoNecessario)
        {
            construir(p);
            return;
        }

        var arvoreAdjacente = ProcurarArvoreAdjacente(p.Posicao);
        if (arvoreAdjacente is { } arvore)
        {
            CortarArvoreAutonomamente(p, arvore);
            return;
        }

        var passo = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, EstaAdjacenteAArvore);
        if (passo is { } destino)
            p.Posicao = destino;
    }

    private Posicao? ProcurarArvoreAdjacente(Posicao pos)
    {
        foreach (var d in Direcoes)
        {
            var vizinho = pos + d;
            if (_mapaDaVila!.DentroDosLimites(vizinho.X, vizinho.Y) && _mapaDaVila[vizinho.X, vizinho.Y] == TipoDeCelula.Arvore)
                return vizinho;
        }

        return null;
    }

    private bool EstaAdjacenteAArvore(Posicao pos) => ProcurarArvoreAdjacente(pos) is not null;

    private void CortarArvoreAutonomamente(Personagem p, Posicao arvore)
    {
        _mapaDaVila![arvore.X, arvore.Y] = TipoDeCelula.Grama;
        p.Madeira += MadeiraPorArvore;
        AdicionarMensagem($"{NomeDoAtor(p)} corta uma árvore e ganha {MadeiraPorArvore} de madeira.");
    }

    private void TentarConstruirAutonomamente(Personagem p)
    {
        foreach (var direcao in Direcoes)
        {
            var area = CalcularAreaDaCasa(p.Posicao, direcao);
            var portaExterna = CalcularPosicaoNaDirecao(p.Posicao, direcao, 1);
            var portaNaParede = CalcularPosicaoNaDirecao(p.Posicao, direcao, DistanciaDaCasaAoPersonagem);

            if (!AreaLivreParaConstrucao(_mapaDaVila!, area) || !TerrenoConstruivel(_mapaDaVila!, portaExterna) || !AreaLivreDePersonagens(area, portaExterna, p))
                continue;

            ConstruirCasa(_mapaDaVila!, area, portaNaParede, portaExterna);
            p.Madeira -= TamanhoDaCasa * TamanhoDaCasa;
            AdicionarMensagem($"{NomeDoAtor(p)} constrói uma casa.");
            return;
        }
    }

    private void TentarConstruirFogueiraAutonomamente(Personagem p) => ConstruirFogueiraSePossivel(p);

    private bool ConstruirFogueiraSePossivel(Personagem p)
    {
        foreach (var direcao in Direcoes)
        {
            var posicao = CalcularPosicaoNaDirecao(p.Posicao, direcao, 1);
            if (!TerrenoConstruivel(_mapaDaVila!, posicao) || PosicaoOcupadaPorOutroPersonagem(posicao, p))
                continue;

            ConstruirFogueira(_mapaDaVila!, posicao);
            p.Madeira -= CustoDaFogueira;
            AdicionarMensagem($"{NomeDoAtor(p)} acende uma fogueira.");
            return true;
        }

        return false;
    }

    private void AtualizarFogueiras()
    {
        for (var i = _fogueirasAtivas.Count - 1; i >= 0; i--)
        {
            if (_turno < _fogueirasAtivas[i].TurnoDeExpiracao)
                continue;

            var posicao = _fogueirasAtivas[i].Posicao;
            _fogueirasAtivas.RemoveAt(i);

            if (_mapaDaVila is not null && _mapaDaVila[posicao.X, posicao.Y] == TipoDeCelula.Fogueira)
                _mapaDaVila[posicao.X, posicao.Y] = TipoDeCelula.Chao;

            AdicionarMensagem("Uma fogueira se apaga.");
        }
    }

    private string NomeDoAtor(Personagem p) =>
        ReferenceEquals(p, Personagem) ? "Você" : $"A Pessoa {_personagens.IndexOf(p) + 1}";

    private void MoverBichos()
    {
        if (_mapaDaVila is null)
            return;

        foreach (var bicho in _bichos)
        {
            var direcao = Direcoes[_random.Next(Direcoes.Length)];
            var alvo = bicho.Posicao + direcao;

            if (_mapaDaVila.EhCaminhavel(alvo) && DistanciaDaBorda(alvo) <= RaioDeAlcanceDoBicho)
                bicho.Posicao = alvo;
        }
    }

    private int DistanciaDaBorda(Posicao p) =>
        Math.Min(Math.Min(p.X, _mapaDaVila!.Largura - 1 - p.X), Math.Min(p.Y, _mapaDaVila.Altura - 1 - p.Y));

    private void TentarNascerBicho()
    {
        if (_mapaDaVila is null || _bichos.Count >= PopulacaoAlvoDeBichos)
            return;

        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var x = _random.Next(_mapaDaVila.Largura);
            var y = _random.Next(_mapaDaVila.Altura);
            var pos = new Posicao(x, y);

            if (DistanciaDaBorda(pos) > RaioDeAlcanceDoBicho || !_mapaDaVila.EhCaminhavel(pos))
                continue;
            if (_bichos.Any(b => b.Posicao == pos))
                continue;

            _bichos.Add(new Bicho(pos));
            return;
        }
    }

    private void VerificarCacaEncontros()
    {
        for (var i = _bichos.Count - 1; i >= 0; i--)
        {
            var bicho = _bichos[i];
            var cacador = _personagens.FirstOrDefault(p => p.Vida > 0 && EstaNaVila(p) && p.Posicao == bicho.Posicao);
            if (cacador is null)
                continue;

            _bichos.RemoveAt(i);
            cacador.Mochila.Add(new Item("Carne", TipoDeItem.Comida, ValorDaCarne));
            AdicionarMensagem($"{NomeDoAtor(cacador)} caça um animal e ganha carne.");
        }
    }

    private int AlivioDoSono(Personagem p) => EstaDescansando(p) ? AlivioDoSonoAoDescansar : 0;

    private bool EstaDescansando(Personagem p)
    {
        var mapaRelevante = ReferenceEquals(p, Personagem) ? Mapa : _mapaDaVila;
        return mapaRelevante is not null && mapaRelevante[p.Posicao.X, p.Posicao.Y] == TipoDeCelula.PisoDaCasa;
    }

    private void AtualizarNecessidade(Func<Personagem, int> obter, Action<Personagem, int> definir, int maximo, int incremento, int dano, string mensagemNoMaximo, string mensagemDeMorte, Func<Personagem, int>? alivio = null)
    {
        for (var i = 0; i < _personagens.Count; i++)
        {
            var p = _personagens[i];
            var valor = obter(p);

            if (valor < maximo)
            {
                var mudanca = incremento - (alivio?.Invoke(p) ?? 0);
                // alivio pode deixar a mudanca negativa (reducao ativa), entao precisa de piso 0 alem do teto
                definir(p, Math.Clamp(valor + mudanca, 0, maximo));
                if (obter(p) == maximo)
                    AdicionarMensagem($"A Pessoa {i + 1} {mensagemNoMaximo}.");
            }
            else
            {
                var vidaAntes = p.Vida;
                p.Vida = Math.Max(0, p.Vida - dano);
                if (vidaAntes > 0 && p.Vida == 0)
                    AdicionarMensagem($"A Pessoa {i + 1} {mensagemDeMorte}.");
            }
        }
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
            TodosVisiveis = true;

            // a exploração é permanente: só precisamos marcar o mapa inteiro uma única vez,
            // não a cada passo — daí em diante isso vira um no-op O(1).
            if (!_vilaTotalmenteExplorada)
            {
                for (var x = 0; x < Mapa.Largura; x++)
                    for (var y = 0; y < Mapa.Altura; y++)
                        Mapa.MarcarExplorada(x, y);

                _vilaTotalmenteExplorada = true;
            }

            return;
        }

        TodosVisiveis = false;
        var raio = LocalAtual == TipoDeLocal.Vila ? RaioDeVisaoNoiteNaVila : RaioDeVisao;

        var visiveis = new HashSet<Posicao>();
        if (Modo == ModoDeJogo.Observador)
        {
            foreach (var p in PersonagensNoLocalAtual)
                visiveis.UnionWith(CampoDeVisao.Calcular(Mapa, p.Posicao, raio));
        }
        else
        {
            visiveis.UnionWith(CampoDeVisao.Calcular(Mapa, Personagem.Posicao, raio));
        }

        if (LocalAtual == TipoDeLocal.Vila)
            foreach (var fogueira in _fogueirasAtivas)
                visiveis.UnionWith(CampoDeVisao.Calcular(Mapa, fogueira.Posicao, RaioDeVisaoDaFogueira));

        CelulasVisiveis = visiveis;

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
