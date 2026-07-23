using Mausmorras.Nucleo.Geracao;
using Mausmorras.Nucleo.Itens;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public sealed partial class EstadoDoJogo
{
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
