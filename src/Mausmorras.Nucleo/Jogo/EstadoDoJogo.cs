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
    private const int OcupantesPorCasa = 2; // uma casa e pensada pra um casal -- se sobrar alguem sozinho, tambem pode ter a sua
    private const int CustoDaMobilia = 5 * OcupantesPorCasa; // 2 camas (por casa, nao por populacao total) + o bau embutido no mesmo custo
    private const int CustoDaCasa = TamanhoDaCasa * TamanhoDaCasa + CustoDaMobilia; // 25 + 10 = 35, por casa
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
    private const int AntecedenciaParaVoltarAntesDoAnoitecer = 25; // turnos de folga antes do fim do dia -- era 10, curto demais pra cacadas que vao ate a beira do mapa (bichos so nascem perto da borda)
    private const int TemperaturaParaFogueiraDuranteACaca = 31; // logo abaixo do dia normal (32) -- dispara quase no instante em que a noite comeca a esfriar, maximizando o tempo de reacao
    private const int DuracaoDaFogueira = 150; // um pouco mais que um ciclo dia/noite completo (120 turnos)

    public const int SonoMaximo = 300;
    private const int SonoPorTurno = 1;
    private const int DanoPorSonoMaximo = 1;
    private const double LimiarSonoParaDormir = 0.2; // era 0.35 -- mais baixo que fome/frio de proposito, ja que dormir so alivia em casa, sem equivalente portatil tipo a fogueira, entao precisa de mais folga pra caminhada de volta
    private const int AlivioDoSonoDiurno = 3; // dormir de dia ainda ajuda, mas bem menos que de noite
    private const int AlivioDoSonoNoturno = 8; // dormir de noite e o que realmente recupera o sono
    private const int SonoMinimoAoDescansar = 30; // dormir a noite toda nao zera o sono de vez -- sobra um residuo, como cansaço real
    private const int EstoqueDeComidaParaCompartilhar = 2; // a partir de quantos itens de comida o cacador comeca a repassar pro outro
    private const int NumeroDeFundadores = 4; // era 2 -- vila comeca maior
    private const int VidaMaximaMinimaFundador = 22; // era 18 -- mais margem de sobrevivencia
    private const int VidaMaximaMaximaFundador = 28; // era 22
    private const int VidaMaximaMinimaCrianca = 10; // criancas comecam mais frageis que os fundadores
    private const int VidaMaximaMaximaCrianca = 14; // variacao (em vez de um valor fixo) evita que duas criancas cresçam com VidaMaxima identica -- o mesmo risco de "empate mortal" que o do..while dos fundadores ja existe pra evitar
    private const int TurnosParaCrescer = 400; // ~3.3 dias de jogo ate um filho virar adulto
    private const double ChanceDeNascimentoPorTurno = 0.03; // chance por turno quando as condicoes pra nascimento estao dadas
    private const double LimiarFomeParaBuscarComida = 0.35; // era 0.5 -- age mais cedo, sobra mais tempo de volta
    private const int PopulacaoAlvoDeBichos = 6;
    private const int RaioDeAlcanceDoBicho = 3; // distancia maxima da borda do mapa
    private const int TentativasDeNascimentoDeBichoPorDia = 3; // 1 por dia nao repunha rapido o bastante -- a populacao ia a zero em poucos dias de caca e nunca mais se recuperava, forcando cacadas cada vez mais longas ate a beira do mapa
    private const double MargemParaTrocarDeObjetivo = 0.15; // ver comentario em PensarPersonagensAutonomos -- evita trocar de objetivo por uma diferenca de severidade insignificante
    private const double SeveridadeMinimaParaInterromperPorFrio = 0.5; // frio so ignora o compromisso quando ja esta REALMENTE severo, nao so acima do limiar pessoal (que o trauma pode deixar bem baixo) -- senao alguem com muita aversao fica preso perto do fogo, nunca se afasta tempo suficiente pra caçar, e acaba morrendo de fome do lado da fogueira
    private const double LimiarVidaParaMedoDeMorrer = 0.3; // abaixo desse percentual de vida, o instinto de sobrevivencia ignora qualquer compromisso ou tarefa em andamento
    private const int DistanciaMaximaDeCacaDaCasa = 35; // acima disso, abandona a cacada/coleta e volta -- bichos so nascem perto da borda do mapa, e uma cacada longe demais pode nao terminar antes da noite cair. Valor alto de proposito: so existe pra evitar o caso extremo (perseguir um bicho ate a ponta oposta do mapa), nao pra limitar cacadas normais

    // virtudes/tracos: cada pessoa nasce com um jeito de ser que ajusta o quanto ela arrisca
    private const double AjusteDeLimiarCaseira = -0.15; // reage ao frio mais cedo, mais cautelosa
    private const double AjusteDeLimiarAventureira = 0.15; // aguenta mais frio antes de se preocupar
    private const int AjusteDeDistanciaCaseira = -10;
    private const int AjusteDeDistanciaAventureira = 15;

    // memoria: presenciar a morte de alguem da casa deixa os sobreviventes mais precavidos com a
    // mesma causa -- "meu companheiro morreu de frio, agora tenho mais medo de frio"
    private const double AjusteDeLimiarPorAversao = 0.1; // por ponto de aversao acumulada
    private const int AjusteDeDistanciaPorAversaoAoFrio = 5;

    private const double ChanceDeDesejoOcioso = 0.08; // chance por turno de expressar uma vontade cosmetica quando nao ha nada urgente nem produtivo pra fazer
    private const int ValorDaCarne = 80; // reduz esse tanto de Fome ao comer
    private const int MadeiraPorArvoreFrutifera = 2; // menos que a arvore normal (5) -- o foco aqui e a fruta
    private const int ValorDaFruta = 40; // metade da carne (80) -- alivio menor, mas nao precisa cacar
    private const int TurnosDeRegrowthDaFruta = 60; // meio dia de folga antes de poder colher de novo no mesmo pe
    private const double ChanceDeArvoreFrutiferaAoRebrotar = 0.2;

    private static readonly Posicao[] Direcoes = { Direcao.Norte, Direcao.Sul, Direcao.Leste, Direcao.Oeste };

    private readonly List<string> _mensagens = new();
    private readonly List<string> _conversas = new();
    private Dictionary<Posicao, Item> _itensNoChao = new();
    private readonly List<Item> _bau = new();
    private MapaDaMasmorra? _mapaDaVila;
    private IReadOnlyList<Sala> _salasDaVila = Array.Empty<Sala>();
    private Posicao _ultimaDirecao = Direcao.Sul;
    private int _turno;
    private bool _vilaTotalmenteExplorada;
    private bool _existeCasaNaVila;
    private bool _primeiroAbrigoConstruido;
    private int _numeroDeCasas; // quantas casas ja foram construidas -- usado pra decidir se falta gente sem casa (ver NumeroDeCasasAlvo)
    private readonly List<(Posicao Posicao, int TurnoDeExpiracao)> _fogueirasAtivas = new();
    private readonly Dictionary<Posicao, int> _proximaColheitaDisponivel = new();

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
    public int Madeira { get; private set; }
    public IEnumerable<Personagem> PersonagensNoLocalAtual =>
        LocalAtual == TipoDeLocal.Vila ? _personagens : new[] { Personagem };
    public IReadOnlyList<Bicho> BichosNoLocalAtual => LocalAtual == TipoDeLocal.Vila ? _bichos : Array.Empty<Bicho>();
    public int Andar { get; private set; } = 1;
    public TipoDeLocal LocalAtual => Andar == 0 ? TipoDeLocal.Vila : TipoDeLocal.Masmorra;
    public IReadOnlySet<Posicao> CelulasVisiveis { get; private set; } = new HashSet<Posicao>();
    public bool TodosVisiveis { get; private set; }
    public IReadOnlyList<string> Mensagens => _mensagens;
    public IReadOnlyList<string> Conversas => _conversas;
    public IReadOnlyList<Item> Bau => _bau;
    public bool Morto => Personagem.Vida <= 0;
    public int Turno => _turno;
    public bool EhDia => (_turno / TurnosPorMetadeDoDia) % 2 == 0;
    public int Dia => _turno / (TurnosPorMetadeDoDia * 2) + 1;
    public ModoDeJogo Modo { get; private set; } = ModoDeJogo.Jogando;

    public EstadoDoJogo(int largura, int altura, int? seed = null)
    {
        _largura = largura;
        _altura = altura;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();

        var spawn = PrepararVila();

        // vida maxima nunca pode empatar entre dois fundadores: como a fome sobe igual pra todo
        // mundo, um empate faria os dois morrerem no mesmo turno, sem ninguem vivo pra
        // TransferirControleAoMorrer passar o controle adiante
        var vidasMaximasUsadas = new List<int>();
        for (var i = 0; i < NumeroDeFundadores; i++)
        {
            int vidaMaxima;
            do
            {
                vidaMaxima = VidaMaximaAleatoria();
            } while (vidasMaximasUsadas.Contains(vidaMaxima));
            vidasMaximasUsadas.Add(vidaMaxima);

            _personagens.Add(new Personagem(new Posicao(spawn.X + i, spawn.Y), vidaMaxima) { Traco = TracoAleatorio() });
        }
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

    private TracoDePersonalidade TracoAleatorio() => (TracoDePersonalidade)_random.Next(3);

    private double LimiarFrioEfetivo(Personagem p)
    {
        var ajusteTraco = p.Traco switch
        {
            TracoDePersonalidade.Caseira => AjusteDeLimiarCaseira,
            TracoDePersonalidade.Aventureira => AjusteDeLimiarAventureira,
            _ => 0.0
        };
        return Math.Clamp(LimiarTemperaturaParaBuscarAbrigo + ajusteTraco - p.AversaoAoFrio * AjusteDeLimiarPorAversao, 0.1, 0.9);
    }

    private double LimiarFomeEfetivo(Personagem p) =>
        Math.Clamp(LimiarFomeParaBuscarComida - p.AversaoAFome * AjusteDeLimiarPorAversao, 0.1, 0.9);

    private double LimiarSonoEfetivo(Personagem p) =>
        Math.Clamp(LimiarSonoParaDormir - p.AversaoAoSono * AjusteDeLimiarPorAversao, 0.1, 0.9);

    private int DistanciaMaximaEfetiva(Personagem p)
    {
        var ajusteTraco = p.Traco switch
        {
            TracoDePersonalidade.Caseira => AjusteDeDistanciaCaseira,
            TracoDePersonalidade.Aventureira => AjusteDeDistanciaAventureira,
            _ => 0
        };
        var ajusteAversao = (int)(p.AversaoAoFrio * AjusteDeDistanciaPorAversaoAoFrio);
        return Math.Max(10, DistanciaMaximaDeCacaDaCasa + ajusteTraco - ajusteAversao);
    }

    // presenciar a morte de alguem da casa deixa quem sobrou mais precavido com a mesma causa
    private void RegistrarTraumaPorMorte(Personagem falecido, string causa)
    {
        var sobreviventes = _personagens.Where(o => !ReferenceEquals(o, falecido) && o.Vida > 0).ToList();
        if (sobreviventes.Count == 0)
            return;

        foreach (var sobrevivente in sobreviventes)
        {
            switch (causa)
            {
                case "frio": sobrevivente.AversaoAoFrio++; break;
                case "fome": sobrevivente.AversaoAFome++; break;
                case "sono": sobrevivente.AversaoAoSono++; break;
            }
        }

        var nomeDaCausa = causa switch { "frio" => "frio", "fome" => "fome", _ => "cansaço" };
        AdicionarMensagem(sobreviventes.Count > 1
            ? $"Depois disso, os sobreviventes ficam mais precavidos com {nomeDaCausa}."
            : $"Depois disso, quem sobrou fica mais precavido com {nomeDaCausa}.");
    }

    public bool SelecionarProximoPersonagem()
    {
        if (LocalAtual != TipoDeLocal.Vila)
            return false;

        if (_personagens.Count(p => p.Vida > 0 && !p.EhCrianca) <= 1)
            return false;

        var indice = _indiceSelecionado;
        do
        {
            indice = (indice + 1) % _personagens.Count;
        } while (_personagens[indice].Vida <= 0 || _personagens[indice].EhCrianca);

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
                for (var i = 0; i < TentativasDeNascimentoDeBichoPorDia; i++)
                    TentarNascerBicho();
            }
        }

        PensarPersonagensAutonomos();
        TentarNascerCrianca();
        AtualizarCrescimento();
        // a caca precisa ser checada ANTES do bicho se mover: se checasse depois, um bicho que
        // acabou de ser pisado por um personagem poderia escapar no mesmo turno so por ter
        // sorteado um passo pra longe antes da checagem rodar
        VerificarCacaEncontros();
        MoverBichos();
        AtualizarNecessidade(p => p.Fome, (p, v) => p.Fome = v, FomeMaxima, FomePorTurno, DanoPorFomeMaxima, "está faminta", "morreu de fome", "fome");
        // criancas ficam de fora do rastreamento de sono (ver AtualizarNecessidade) -- elas nao tem
        // como chegar sozinhas na cama, entao contariam apenas como uma exaustao inevitavel
        AtualizarNecessidade(p => p.Sono, (p, v) => p.Sono = v, SonoMaximo, SonoPorTurno, DanoPorSonoMaximo, "está com sono", "morreu de exaustão", "sono", AlivioDoSono, SonoMinimoAoDescansar, ignorarCriancas: true);
        AtualizarTemperatura();
        AtualizarFogueiras();
        TransferirControleAoMorrer();
        AtualizarVisibilidade();
    }

    private void TransferirControleAoMorrer()
    {
        if (Personagem.Vida > 0)
            return;

        var indiceVivo = _personagens.FindIndex(p => p.Vida > 0 && !p.EhCrianca);
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
                {
                    AdicionarMensagem($"{NomeDoAtor(p)} morreu de frio.");
                    RegistrarTraumaPorMorte(p, "frio");
                }
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

        if (mapaRelevante[p.Posicao.X, p.Posicao.Y] is TipoDeCelula.PisoDaCasa or TipoDeCelula.Cama or TipoDeCelula.Bau)
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

    // uma casa por casal (2 pessoas); sobrando alguem sozinho, ele tambem ganha a propria --
    // 4 pessoas = 2 casas, 5 = 3, 6 = 3, 7 = 4 ...
    private int NumeroDeCasasAlvo() =>
        (int)Math.Ceiling(_personagens.Count(p => p.Vida > 0 && !p.EhCrianca) / (double)OcupantesPorCasa);

    private void PensarPersonagensAutonomos()
    {
        if (_mapaDaVila is null)
            return;

        foreach (var p in _personagens)
        {
            if (p.Vida <= 0)
                continue;

            // criancas nao cacam/coletam nada sozinhas (alguem precisa perceber a fome delas e agir --
            // ver TentarAjudarComFome, considerado abaixo pra cada adulto), mas comem o que ja
            // estiver na propria mochila assim que um adulto colocar algo la via DarComida
            if (p.EhCrianca)
            {
                var comidaDaCrianca = p.Mochila.FirstOrDefault(it => it.Tipo == TipoDeItem.Comida);
                if (comidaDaCrianca is not null)
                {
                    ComerAlimento(p, comidaDaCrianca);
                    p.Mochila.Remove(comidaDaCrianca);
                }
                continue;
            }

            var ehControlado = ReferenceEquals(p, Personagem) && Modo == ModoDeJogo.Jogando;
            if (ehControlado || !EstaNaVila(p))
                continue;

            var fomeSeveridade = (double)p.Fome / FomeMaxima;
            var sonoSeveridade = (double)p.Sono / SonoMaximo;
            var temperaturaSeveridade = Math.Clamp((double)(TemperaturaIdeal - p.Temperatura) / (TemperaturaIdeal - TemperaturaCritica), 0, 1);

            var fomeUrgente = fomeSeveridade >= LimiarFomeEfetivo(p);
            var frioUrgente = temperaturaSeveridade >= LimiarFrioEfetivo(p);
            var sonoUrgente = sonoSeveridade >= LimiarSonoEfetivo(p);

            // "medo de morrer": com a vida criticamente baixa, o instinto de sobrevivencia da pessoa
            // ignora qualquer compromisso ou tarefa em andamento -- vira o unico foco ate se recuperar.
            // a fala so dispara na transicao (nao tinha medo -> tem medo), nao a cada turno
            var estaComMedo = (double)p.Vida / p.VidaMaxima <= LimiarVidaParaMedoDeMorrer;
            if (estaComMedo && !p.EstaComMedo)
                FalarSobre(p, "medo");
            p.EstaComMedo = estaComMedo;

            // mesmo sem nenhuma necessidade critica ainda, comecar a voltar pro abrigo com
            // antecedencia (em vez de so reagir depois que o frio ja esta severo) e o que da tempo
            // de sobra pra caminhada -- sem isso, quem esta longe demais quando a noite cai nunca
            // chega a tempo, nao importa quao cedo o frio em si dispara a busca por abrigo
            var turnosParaAnoitecer = TurnosPorMetadeDoDia - (_turno % TurnosPorMetadeDoDia);
            var anoitecerIminente = EhDia && turnosParaAnoitecer <= AntecedenciaParaVoltarAntesDoAnoitecer && !EstaProtegidoDoFrio(p);
            // a severidade cresce conforme a noite se aproxima -- fraca assim que a janela abre (nao
            // interrompe uma cacada ja em andamento por causa de algo que so vai acontecer daqui a
            // muitos turnos), mas vira praticamente inevitavel perto do fim, senao uma fome genuina
            // teria prioridade e ignoraria o aviso ate ser tarde demais pra voltar andando
            var severidadeAnoitecer = anoitecerIminente
                ? 1.0 - (double)turnosParaAnoitecer / AntecedenciaParaVoltarAntesDoAnoitecer
                : 0.0;

            // cooperacao: uma crianca com fome urgente sempre conta como responsabilidade de qualquer
            // adulto por perto (ela nao consegue se alimentar sozinha), e agora um ADULTO com medo de
            // morrer de fome tambem -- ninguem deveria morrer de fome sozinho tendo companhia por perto
            // com comida sobrando. sem isso, so seria atendida quando nenhum adulto tivesse mais nada a fazer
            var alguemPrecisandoDeComida = _personagens.FirstOrDefault(c =>
                !ReferenceEquals(c, p) && c.Vida > 0 && (double)c.Fome / FomeMaxima >= LimiarFomeParaBuscarComida &&
                (c.EhCrianca || (double)c.Vida / c.VidaMaxima <= LimiarVidaParaMedoDeMorrer));
            var severidadeDeQuemPrecisa = alguemPrecisandoDeComida is not null ? (double)alguemPrecisandoDeComida.Fome / FomeMaxima : 0.0;

            // "compromisso de acao": uma vez que a pessoa comeca a perseguir uma necessidade, ela so
            // troca de alvo se outra ficar CLARAMENTE mais severa (margem abaixo). Sem isso, quando
            // duas necessidades sobem juntas com severidade parecida (ex: fome e sono), a escolha
            // reavaliada do zero a cada turno fica alternando de alvo sem nunca completar nenhum
            // caminho -- as duas acabam batendo no maximo ao mesmo tempo e a pessoa morre por uma
            // delas mesmo com recursos (caca, abrigo) disponiveis o tempo todo
            var candidatos = new List<(string Rotulo, double Severidade, Action Acao)>();
            if (fomeUrgente) candidatos.Add(("fome", fomeSeveridade, () => TentarResolverFome(p)));
            if (frioUrgente) candidatos.Add(("frio", temperaturaSeveridade, () => TentarBuscarAbrigo(p)));
            // sono so entra na lista se ja existir casa pra ir -- caso contrario TentarDormir nao
            // acha nenhuma Cama e nao faz nada
            if (sonoUrgente && _existeCasaNaVila) candidatos.Add(("sono", sonoSeveridade, () => TentarDormir(p)));
            if (anoitecerIminente) candidatos.Add(("anoitecer", severidadeAnoitecer, () => TentarBuscarAbrigo(p)));
            if (alguemPrecisandoDeComida is not null) candidatos.Add(("cooperacao", severidadeDeQuemPrecisa, () => TentarAjudarComFome(p, alguemPrecisandoDeComida!)));

            Action? acaoVencedora = null;

            if (candidatos.Count > 0)
            {
                var maisSevero = candidatos.OrderByDescending(c => c.Severidade).First();
                var candidatoAtual = candidatos.FirstOrDefault(c => c.Rotulo == p.ObjetivoAtual);

                // frio (quando REALMENTE severo) e o aviso antecipado de anoitecer nunca ficam presos
                // atras de um compromisso. "medo de morrer" tambem nao -- com a vida criticamente baixa,
                // a propria sobrevivencia (fome/frio/sono) nunca fica presa atras de um compromisso antigo
                var ehUrgenciaDeFrio = maisSevero.Rotulo == "anoitecer"
                    || (maisSevero.Rotulo == "frio" && maisSevero.Severidade >= SeveridadeMinimaParaInterromperPorFrio)
                    || (estaComMedo && maisSevero.Rotulo is "fome" or "frio" or "sono");

                var escolhido = !ehUrgenciaDeFrio && candidatoAtual.Acao is not null
                    && maisSevero.Severidade - candidatoAtual.Severidade < MargemParaTrocarDeObjetivo
                    ? candidatoAtual
                    : maisSevero;

                p.ObjetivoAtual = escolhido.Rotulo;
                acaoVencedora = escolhido.Acao;
            }
            else
            {
                p.ObjetivoAtual = null;
            }

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
                    TentarObterMadeira(p, CustoDaCasa, TentarConstruirAutonomamente);
                else
                    TentarObterMadeira(p, CustoDaFogueira, TentarConstruirFogueiraAutonomamente);
            }
            else
            {
                // divisao de trabalho: com a casa pronta, nao faz mais sentido todo mundo ficar atras
                // de madeira ao mesmo tempo (agora e um estoque so, nao precisam duplicar esforco) --
                // so quem estiver com a fome mais alta do grupo (mesmo que ainda nao seja urgente) cai
                // pra cacar com antecedencia, guardando comida pra quando precisar de verdade; os
                // outros cuidam de manter fogueiras por perto. generalizado pra qualquer numero de
                // adultos (nao so 2): compara contra o grupo inteiro, nao so contra "o outro"
                // empate de fome e o estado DEFAULT logo apos a casa ficar pronta (todos sobem fome
                // no mesmo ritmo ate alguem comer algo diferente) -- por isso o desempate por indice e
                // essencial: sem ele, empate faz todo mundo decidir "eu caco" no mesmo turno, ninguem
                // sobra pra cuidar de fogueira, e a divisao de trabalho nunca chega a valer
                //
                // o numero de cacadores escala com o grupo (mesma proporcao de OcupantesPorCasa): com
                // so 1 cacador fixo, 4 pessoas dividem a comida de 1 so caçador e a fome pessoal de
                // quase todo mundo fica cronicamente alta, sem ninguem sobrando pra tarefas de menor
                // prioridade (como construir a segunda casa) -- madeira nunca sobra pra isso
                var adultosNaVila = _personagens.Where(o => o.Vida > 0 && !o.EhCrianca && EstaNaVila(o)).ToList();
                var numeroDeCacadoresAlvo = (int)Math.Ceiling(adultosNaVila.Count / (double)OcupantesPorCasa);
                var maisFamintosDoGrupo = adultosNaVila
                    .OrderByDescending(o => o.Fome)
                    .ThenByDescending(o => _personagens.IndexOf(o))
                    .Take(numeroDeCacadoresAlvo)
                    .ToHashSet();
                var deveriaCacarPreventivamente = maisFamintosDoGrupo.Contains(p);

                if (deveriaCacarPreventivamente)
                    TentarCacarPreventivamente(p);
                else if (NumeroDeCasasAlvo() > _numeroDeCasas)
                    TentarObterMadeira(p, CustoDaCasa, TentarConstruirAutonomamente);
                else if (!EstaProtegidoDoFrio(p))
                    TentarObterMadeira(p, CustoDaFogueira, TentarConstruirFogueiraAutonomamente);
                else if (_random.NextDouble() < ChanceDeDesejoOcioso)
                    ExpressarDesejo(p);
            }
        }
    }

    private static readonly string[] DesejosOciosos = { "explorar", "conversar", "descansar" };

    // vontade puramente cosmetica, so preenche o tempo ocioso quando nao ha nada urgente nem
    // produtivo pra fazer -- nao muda posicao nem necessidades, so da uma fala e um rotulo
    private void ExpressarDesejo(Personagem p)
    {
        var desejo = DesejosOciosos[_random.Next(DesejosOciosos.Length)];
        p.DesejoAtual = desejo;
        FalarSobre(p, $"desejo_{desejo}");
    }

    private void TentarNascerCrianca()
    {
        if (_mapaDaVila is null || !_existeCasaNaVila)
            return;

        // um filho de cada vez -- evita sobrecarregar os pais com cuidado infantil simultaneo
        if (_personagens.Any(c => c.Vida > 0 && c.EhCrianca))
            return;

        var adultos = _personagens.Where(p => p.Vida > 0 && !p.EhCrianca && EstaNaVila(p)).ToList();

        for (var i = 0; i < adultos.Count; i++)
        {
            for (var j = i + 1; j < adultos.Count; j++)
            {
                var a = adultos[i];
                var b = adultos[j];

                if (!EstaAdjacente(a.Posicao, b.Posicao) || !EstavelParaTerFilho(a) || !EstavelParaTerFilho(b))
                    continue;

                // a crianca nasce protegida do frio -- sem isso ela dependeria de alguem "carrega-la"
                // pra dentro de casa, o que essa versao nao modela; exigir que pelo menos um dos pais
                // ja esteja num lugar protegido garante que o nascimento so acontece num lugar seguro
                var posicaoProtegida = EstaProtegidoDoFrio(a) ? a.Posicao : EstaProtegidoDoFrio(b) ? b.Posicao : (Posicao?)null;
                if (posicaoProtegida is not { } posicaoNascimento)
                    continue;

                if (_random.NextDouble() >= ChanceDeNascimentoPorTurno)
                    return;

                var crianca = new Personagem(posicaoNascimento, _random.Next(VidaMaximaMinimaCrianca, VidaMaximaMaximaCrianca + 1)) { EhCrianca = true, Traco = TracoAleatorio() };
                _personagens.Add(crianca);
                AdicionarMensagem("Uma criança nasce na vila!");
                FalarSobre(a, "nascimento");
                return;
            }
        }
    }

    private bool EstavelParaTerFilho(Personagem p)
    {
        var fomeSeveridade = (double)p.Fome / FomeMaxima;
        var sonoSeveridade = (double)p.Sono / SonoMaximo;
        var temperaturaSeveridade = Math.Clamp((double)(TemperaturaIdeal - p.Temperatura) / (TemperaturaIdeal - TemperaturaCritica), 0, 1);

        return fomeSeveridade < LimiarFomeParaBuscarComida
            && sonoSeveridade < LimiarSonoParaDormir
            && temperaturaSeveridade < LimiarTemperaturaParaBuscarAbrigo;
    }

    private void AtualizarCrescimento()
    {
        foreach (var p in _personagens)
        {
            if (p.Vida <= 0 || !p.EhCrianca)
                continue;

            p.Idade++;
            if (p.Idade >= TurnosParaCrescer)
            {
                p.EhCrianca = false;
                AdicionarMensagem($"{NomeDoAtor(p)} cresceu e virou adulta.");
            }
        }
    }

    // cobre tanto uma crianca faminta quanto um adulto com medo de morrer de fome -- ver
    // alguemPrecisandoDeComida em PensarPersonagensAutonomos
    private void TentarAjudarComFome(Personagem ajudante, Personagem alvo)
    {
        var comida = ajudante.Mochila.FirstOrDefault(it => it.Tipo == TipoDeItem.Comida);
        if (comida is not null)
        {
            if (EstaAdjacente(ajudante.Posicao, alvo.Posicao))
            {
                DarComida(ajudante, alvo);
                return;
            }

            var passoAteAlvo = Caminho.ProximoPasso(_mapaDaVila!, ajudante.Posicao, pos => pos == alvo.Posicao);
            if (passoAteAlvo is { } destinoAlvo)
                ajudante.Posicao = destinoAlvo;
            return;
        }

        var arvoreFrutiferaAdjacente = ProcurarArvoreFrutiferaAdjacente(ajudante.Posicao);
        if (arvoreFrutiferaAdjacente is { } arvoreFruta)
        {
            ColherArvoreAutonomamente(ajudante, arvoreFruta);
            return;
        }

        if (TentarProtegerDoFrioDuranteExpedicao(ajudante))
            return;

        // uma cacada nao pode arrastar a pessoa pra tao longe de casa que a volta antes da noite
        // vire inviavel -- melhor abandonar a comida da vez do que apostar a propria sobrevivencia
        if (DistanteDemaisDaCasa(ajudante))
        {
            TentarBuscarAbrigo(ajudante);
            return;
        }

        var passoAteBicho = Caminho.ProximoPasso(_mapaDaVila!, ajudante.Posicao, pos => _bichos.Any(b => b.Posicao == pos));
        if (passoAteBicho is { } destinoBicho)
            ajudante.Posicao = destinoBicho;
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

        // uma arvore frutifera bem do lado e uma fonte de comida mais segura que perseguir um bicho --
        // nao desvia o caminho pra ela, so aproveita se ja estiver por perto nesse exato momento
        var arvoreFrutiferaAdjacente = ProcurarArvoreFrutiferaAdjacente(p.Posicao);
        if (arvoreFrutiferaAdjacente is { } arvoreFruta)
        {
            ColherArvoreAutonomamente(p, arvoreFruta);
            return;
        }

        // uma cacada pode levar pra bem longe de qualquer abrigo por muitos turnos seguidos, e a
        // temperatura despenca rapido demais (18 graus a 2/turno = so uns 9 turnos de folga) pra dar
        // tempo de reagir do zero -- se ja esta esfriando e desprotegido, desvia pra arvore mais
        // proxima e depois acende a fogueira ali mesmo, sem esperar a fome ceder prioridade pro frio
        if (TentarProtegerDoFrioDuranteExpedicao(p))
            return;

        // uma cacada nao pode arrastar a pessoa pra tao longe de casa que a volta antes da noite
        // vire inviavel -- melhor abandonar a comida da vez do que apostar a propria sobrevivencia
        if (DistanteDemaisDaCasa(p))
        {
            TentarBuscarAbrigo(p);
            return;
        }

        var passo = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, pos => _bichos.Any(b => b.Posicao == pos));
        if (passo is { } destino)
            p.Posicao = destino;
    }

    // extraido de TentarResolverFome -- qualquer expedicao que possa levar pra longe de casa por
    // muitos turnos (cacada, cacada preventiva, busca de comida pra crianca) precisa da MESMA rede
    // de seguranca, senao a pessoa so descobre que esta desprotegida quando ja e tarde demais
    // agora que existem varias casas espalhadas (uma por casal), nao ha mais um unico ponto fixo
    // de "casa" pra comparar distancia -- procura o abrigo mais proximo de verdade a cada checagem
    private bool DistanteDemaisDaCasa(Personagem p)
    {
        if (_mapaDaVila is null)
            return false;

        var distanciaMinima = int.MaxValue;
        for (var x = 0; x < _mapaDaVila.Largura; x++)
        {
            for (var y = 0; y < _mapaDaVila.Altura; y++)
            {
                if (_mapaDaVila[x, y] is not (TipoDeCelula.PisoDaCasa or TipoDeCelula.Cama or TipoDeCelula.Bau))
                    continue;

                var distancia = Math.Abs(p.Posicao.X - x) + Math.Abs(p.Posicao.Y - y);
                if (distancia < distanciaMinima)
                    distanciaMinima = distancia;
            }
        }

        return distanciaMinima != int.MaxValue && distanciaMinima > DistanciaMaximaEfetiva(p);
    }

    private bool TentarProtegerDoFrioDuranteExpedicao(Personagem p)
    {
        if (p.Temperatura > TemperaturaParaFogueiraDuranteACaca || EstaProtegidoDoFrio(p))
            return false;

        if (Madeira >= CustoDaFogueira)
        {
            if (ConstruirFogueiraSePossivel(p))
                return true;
        }
        else
        {
            var arvoreAdjacente = ProcurarArvoreAdjacente(p.Posicao);
            if (arvoreAdjacente is { } arvore)
            {
                ColherArvoreAutonomamente(p, arvore);
                return true;
            }

            var passoParaArvore = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, EstaAdjacenteAArvore);
            if (passoParaArvore is { } destinoArvore)
            {
                p.Posicao = destinoArvore;
                return true;
            }
        }

        return false;
    }

    private void TentarCacarPreventivamente(Personagem p)
    {
        // "previsao de futuro": nao basta cacar so pra si -- se ja tem comida de sobra (mais do
        // que o outro), vale mais repassar agora, enquanto o outro ainda nao esta com fome, do
        // que so ir empilhando na propria mochila
        var outro = _personagens.FirstOrDefault(o => !ReferenceEquals(o, p) && o.Vida > 0 && !o.EhCrianca && EstaNaVila(o));
        if (outro is not null && ContarComida(p) >= EstoqueDeComidaParaCompartilhar && ContarComida(p) > ContarComida(outro))
        {
            if (EstaAdjacente(p.Posicao, outro.Posicao))
            {
                DarComida(p, outro);
                return;
            }

            var passoAteOutro = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, pos => pos == outro.Posicao);
            if (passoAteOutro is { } destinoOutro)
            {
                p.Posicao = destinoOutro;
                return;
            }
        }

        var arvoreFrutiferaAdjacente = ProcurarArvoreFrutiferaAdjacente(p.Posicao);
        if (arvoreFrutiferaAdjacente is { } arvoreFruta)
        {
            ColherArvoreAutonomamente(p, arvoreFruta);
            return;
        }

        if (TentarProtegerDoFrioDuranteExpedicao(p))
            return;

        // uma cacada nao pode arrastar a pessoa pra tao longe de casa que a volta antes da noite
        // vire inviavel -- melhor abandonar a comida da vez do que apostar a propria sobrevivencia
        if (DistanteDemaisDaCasa(p))
        {
            TentarBuscarAbrigo(p);
            return;
        }

        var passo = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, pos => _bichos.Any(b => b.Posicao == pos));
        if (passo is { } destino)
            p.Posicao = destino;
    }

    private static int ContarComida(Personagem p) => p.Mochila.Count(it => it.Tipo == TipoDeItem.Comida);

    private static bool EstaAdjacente(Posicao a, Posicao b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) <= 1;

    private void DarComida(Personagem doador, Personagem receptor)
    {
        var comida = doador.Mochila.FirstOrDefault(it => it.Tipo == TipoDeItem.Comida);
        if (comida is null)
            return;

        doador.Mochila.Remove(comida);
        receptor.Mochila.Add(comida);
        AdicionarMensagem($"{NomeDoAtor(doador)} dá {comida.Nome} {ParaQuem(receptor)}.");
        FalarSobre(doador, "comida_dada");
    }

    private string ParaQuem(Personagem p) =>
        ReferenceEquals(p, Personagem) ? "para você" : $"para a Pessoa {_personagens.IndexOf(p) + 1}";

    private void TentarBuscarAbrigo(Personagem p)
    {
        if (EstaProtegidoDoFrio(p))
            return;

        // uma caminhada longa demais ate o abrigo mais proximo pode nunca terminar a tempo -- se tem
        // (ou consegue) madeira, uma fogueira bem onde a pessoa esta e sempre mais segura e mais
        // rapida do que apostar numa volta pra casa, entao vira a resposta padrao ao frio, nao um
        // ultimo recurso; a casa/vila tem varias fogueiras espalhadas com o tempo por causa disso.
        // mesma rede de seguranca usada durante cacadas/coletas (TentarProtegerDoFrioDuranteExpedicao)
        // -- sem ela, ficar sem madeira no estoque no momento errado deixava a unica opcao ser andar,
        // e uma casa longe demais podia nunca ser alcancada a tempo
        if (TentarProtegerDoFrioDuranteExpedicao(p))
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

        return EstaPertoDeFogueira(mapaRelevante, p.Posicao) ||
            mapaRelevante[p.Posicao.X, p.Posicao.Y] is TipoDeCelula.PisoDaCasa or TipoDeCelula.Cama or TipoDeCelula.Bau;
    }

    private void TentarDormir(Personagem p)
    {
        var passo = Caminho.ProximoPasso(_mapaDaVila!, p.Posicao, pos => _mapaDaVila![pos.X, pos.Y] == TipoDeCelula.Cama);
        if (passo is { } destino)
            p.Posicao = destino;
    }

    private void TentarObterMadeira(Personagem p, int custoNecessario, Action<Personagem> construir)
    {
        if (Madeira >= custoNecessario)
        {
            construir(p);
            return;
        }

        var arvoreAdjacente = ProcurarArvoreAdjacente(p.Posicao);
        if (arvoreAdjacente is { } arvore)
        {
            ColherArvoreAutonomamente(p, arvore);
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
            if (_mapaDaVila!.DentroDosLimites(vizinho.X, vizinho.Y) && EhArvoreColhivel(vizinho))
                return vizinho;
        }

        return null;
    }

    private bool EhArvoreColhivel(Posicao pos)
    {
        var tipo = _mapaDaVila![pos.X, pos.Y];
        if (tipo == TipoDeCelula.Arvore)
            return true;

        return tipo == TipoDeCelula.ArvoreFrutifera && PodeColherFruta(pos);
    }

    private bool PodeColherFruta(Posicao posicao) =>
        !_proximaColheitaDisponivel.TryGetValue(posicao, out var turnoDisponivel) || _turno >= turnoDisponivel;

    private Posicao? ProcurarArvoreFrutiferaAdjacente(Posicao pos)
    {
        foreach (var d in Direcoes)
        {
            var vizinho = pos + d;
            if (_mapaDaVila!.DentroDosLimites(vizinho.X, vizinho.Y) && _mapaDaVila[vizinho.X, vizinho.Y] == TipoDeCelula.ArvoreFrutifera && PodeColherFruta(vizinho))
                return vizinho;
        }

        return null;
    }

    private bool EstaAdjacenteAArvore(Posicao pos) => ProcurarArvoreAdjacente(pos) is not null;

    private void ColherArvoreAutonomamente(Personagem p, Posicao arvore)
    {
        if (_mapaDaVila![arvore.X, arvore.Y] == TipoDeCelula.ArvoreFrutifera)
        {
            Madeira += MadeiraPorArvoreFrutifera;
            p.Mochila.Add(new Item("Fruta", TipoDeItem.Comida, ValorDaFruta));
            _proximaColheitaDisponivel[arvore] = _turno + TurnosDeRegrowthDaFruta;
            AdicionarMensagem($"{NomeDoAtor(p)} colhe frutas e ganha {MadeiraPorArvoreFrutifera} de madeira.");
            FalarSobre(p, "fruta");
            return;
        }

        _mapaDaVila[arvore.X, arvore.Y] = TipoDeCelula.Grama;
        Madeira += MadeiraPorArvore;
        AdicionarMensagem($"{NomeDoAtor(p)} corta uma árvore e ganha {MadeiraPorArvore} de madeira.");
        FalarSobre(p, "madeira");
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
            Madeira -= CustoDaCasa;
            AdicionarMensagem($"{NomeDoAtor(p)} constrói uma casa.");
            FalarSobre(p, "casa");
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
            Madeira -= CustoDaFogueira;
            AdicionarMensagem($"{NomeDoAtor(p)} acende uma fogueira.");
            FalarSobre(p, "fogueira");
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
            FalarSobre(cacador, "caca");
        }
    }

    private int AlivioDoSono(Personagem p) => EstaDescansando(p) ? (EhDia ? AlivioDoSonoDiurno : AlivioDoSonoNoturno) : 0;

    private bool EstaDescansando(Personagem p)
    {
        var mapaRelevante = ReferenceEquals(p, Personagem) ? Mapa : _mapaDaVila;
        return mapaRelevante is not null && mapaRelevante[p.Posicao.X, p.Posicao.Y] == TipoDeCelula.Cama;
    }

    private void AtualizarNecessidade(Func<Personagem, int> obter, Action<Personagem, int> definir, int maximo, int incremento, int dano, string mensagemNoMaximo, string mensagemDeMorte, string causaDeTrauma, Func<Personagem, int>? alivio = null, int minimo = 0, bool ignorarCriancas = false)
    {
        for (var i = 0; i < _personagens.Count; i++)
        {
            var p = _personagens[i];
            if (p.Vida <= 0 || (ignorarCriancas && p.EhCrianca))
                continue;

            var valor = obter(p);

            if (valor < maximo)
            {
                var mudanca = incremento - (alivio?.Invoke(p) ?? 0);
                // alivio pode deixar a mudanca negativa (reducao ativa), entao precisa de piso alem do teto
                definir(p, Math.Clamp(valor + mudanca, minimo, maximo));
                if (obter(p) == maximo)
                    AdicionarMensagem($"A Pessoa {i + 1} {mensagemNoMaximo}.");
            }
            else
            {
                var vidaAntes = p.Vida;
                p.Vida = Math.Max(0, p.Vida - dano);
                if (vidaAntes > 0 && p.Vida == 0)
                {
                    AdicionarMensagem($"A Pessoa {i + 1} {mensagemDeMorte}.");
                    RegistrarTraumaPorMorte(p, causaDeTrauma);
                }
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
                    _mapaDaVila[x, y] = _random.NextDouble() < ChanceDeArvoreFrutiferaAoRebrotar ? TipoDeCelula.ArvoreFrutifera : TipoDeCelula.Arvore;
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
            // um personagem morto nao enxerga nada -- so os vivos contribuem area visivel
            foreach (var p in PersonagensNoLocalAtual.Where(p => p.Vida > 0))
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

    private static readonly Dictionary<string, string[]> FalasPorEvento = new()
    {
        ["madeira"] = new[] { "Mais um pouco de lenha pra gente.", "Essa árvore ainda tinha o que dar." },
        ["fruta"] = new[] { "Que fruta gostosa, quer um pedaço?", "Essa árvore está sendo generosa com a gente." },
        ["caca"] = new[] { "Consegui carne! Hoje a gente come bem.", "Essa caça não vai ser fácil de esquecer." },
        ["comida"] = new[] { "Ufa, já estou bem melhor.", "Isso mata a fome por um bom tempo." },
        ["casa"] = new[] { "Agora sim, um lar de verdade.", "Isso vai nos proteger bem melhor." },
        ["fogueira"] = new[] { "Essa fogueira vai esquentar a gente direitinho.", "Um pouco de calor já ajuda bastante." },
        ["comida_dada"] = new[] { "Toma, guarda isso pra você.", "Toma, você também deve estar com fome." },
        ["nascimento"] = new[] { "Vamos cuidar bem dessa criança.", "Nossa família está crescendo." },
        ["desejo_explorar"] = new[] { "Tenho vontade de ir explorar um pouco.", "Queria ver o que tem masmorra adentro." },
        ["desejo_conversar"] = new[] { "Vem cá, deixa eu te contar uma coisa.", "Só queria bater um papo." },
        ["desejo_descansar"] = new[] { "Acho que vou só sentar um pouco.", "Não custa nada relaxar um instante." },
        ["medo"] = new[] { "Eu não quero morrer, preciso me cuidar.", "Isso está ficando perigoso demais pra mim." },
    };

    private void FalarSobre(Personagem p, string evento)
    {
        var opcoes = FalasPorEvento[evento];
        var fala = opcoes[_random.Next(opcoes.Length)];
        AdicionarConversa($"{NomeDoAtor(p)}: {fala}");
    }

    private void AdicionarConversa(string texto)
    {
        _conversas.Add(texto);
        if (_conversas.Count > MaximoDeMensagens)
            _conversas.RemoveAt(0);
    }
}
