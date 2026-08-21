using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Geracao;
using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public sealed partial class EstadoDoJogo
{
    private (MapaDaMasmorra Mapa, IReadOnlyList<Sala> Salas) GerarNivel()
    {
        var gerador = new GeradorDeMasmorra();
        var (mapa, salas, itens) = gerador.Gerar(_largura, _altura, Andar, _random);
        _itensNoChao = new Dictionary<Posicao, Item>(itens);
        GerarMonstros(mapa, salas);
        return (mapa, salas);
    }

    // escala com Andar (nunca influenciado antes -- primeira fonte de dificuldade progressiva do
    // jogo); pula Salas[0] (a sala onde o jogador surge) pra nao emboscar ninguem na entrada
    private void GerarMonstros(MapaDaMasmorra mapa, IReadOnlyList<Sala> salas)
    {
        _monstros = new List<Monstro>();
        if (salas.Count <= 1)
            return;

        var salasParaMonstros = salas.Skip(1).ToList();
        var numeroDeMonstros = Math.Min(salasParaMonstros.Count, NumeroBaseDeMonstros + Andar / AndaresPorIncrementoDeMonstro);
        var vidaMaxima = VidaBaseDoMonstro + Andar * IncrementoDeVidaPorAndar;
        var dano = DanoBaseDoMonstro + Andar * IncrementoDeDanoPorAndar;

        for (var i = 0; i < numeroDeMonstros; i++)
        {
            var sala = salasParaMonstros[_random.Next(salasParaMonstros.Count)];
            for (var tentativa = 0; tentativa < 20; tentativa++)
            {
                var pos = new Posicao(_random.Next(sala.X + 1, sala.X + sala.Largura - 1), _random.Next(sala.Y + 1, sala.Y + sala.Altura - 1));
                if (!mapa.EhCaminhavel(pos) || _monstros.Any(m => m.Posicao == pos))
                    continue;

                var tipo = EscolherTipoDeMonstro(_random, Andar);
                var (vidaAjustada, danoAjustado) = AjustarPorTipoDeMonstro(vidaMaxima, dano, tipo);
                _monstros.Add(new Monstro(pos, vidaAjustada, danoAjustado, tipo));
                break;
            }
        }
    }

    // andares rasos so tem Comum -- variedade (e o risco extra dela) so aparece conforme desce
    private static TipoDeMonstro EscolherTipoDeMonstro(Random random, int andar)
    {
        var (pesoComum, pesoResistente, pesoFeroz) = andar switch
        {
            <= 2 => (100, 0, 0),
            <= 5 => (60, 25, 15),
            _ => (35, 35, 30)
        };

        return SorteioPonderado.EscolherIndice(random, pesoComum, pesoResistente, pesoFeroz) switch
        {
            0 => TipoDeMonstro.Comum,
            1 => TipoDeMonstro.Resistente,
            _ => TipoDeMonstro.Feroz
        };
    }

    private static (int Vida, int Dano) AjustarPorTipoDeMonstro(int vidaBase, int danoBase, TipoDeMonstro tipo) => tipo switch
    {
        TipoDeMonstro.Resistente => (
            Math.Max(1, (int)Math.Round(vidaBase * MultiplicadorVidaResistente)),
            Math.Max(1, (int)Math.Round(danoBase * MultiplicadorDanoResistente))),
        TipoDeMonstro.Feroz => (
            Math.Max(1, (int)Math.Round(vidaBase * MultiplicadorVidaFeroz)),
            Math.Max(1, (int)Math.Round(danoBase * MultiplicadorDanoFeroz))),
        _ => (vidaBase, danoBase)
    };

    private Posicao PrepararVila()
    {
        if (_mapaDaVila is null)
        {
            var (mapaGerado, salasGeradas, _) = new GeradorDeVila().Gerar(LarguraDaVila, AlturaDaVila, _random, NumeroDeFundadores);
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
        Personagem.Posicao = PrepararVila();
        AdicionarMensagem("Você retorna à vila.");
    }

    private void EntrarNaMasmorra()
    {
        Andar = 1;
        (Mapa, Salas) = GerarNivel();
        var spawn = Salas.Count > 0 ? Salas[0].Centro : new Posicao(_largura / 2, _altura / 2);
        Mapa[spawn.X, spawn.Y] = TipoDeCelula.SaidaParaVila;
        Personagem.Posicao = spawn;
        AdicionarMensagem("Você entra na masmorra escura.");
    }

    private void Descer()
    {
        Andar++;
        (Mapa, Salas) = GerarNivel();
        var spawn = Salas.Count > 0 ? Salas[0].Centro : new Posicao(_largura / 2, _altura / 2);
        Mapa[spawn.X, spawn.Y] = TipoDeCelula.SaidaParaVila;
        Personagem.Posicao = spawn;
        AdicionarMensagem($"Você desce para o andar {Andar}.");
    }
}
