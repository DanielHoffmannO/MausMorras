using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public sealed partial class EstadoDoJogo
{
    public bool TentarMoverPersonagem(Posicao delta)
    {
        if (Morto || Modo != ModoDeJogo.Jogando)
            return false;

        var alvo = Personagem.Posicao + delta;

        if (Mapa[alvo.X, alvo.Y] == TipoDeCelula.Arvore)
        {
            CortarArvore(alvo);
            AvancarTurno();
            return true;
        }

        if (!Mapa.EhCaminhavel(alvo))
            return false;

        if (_personagens.Any(p => !ReferenceEquals(p, Personagem) && p.Posicao == alvo))
            return false;

        Personagem.Posicao = alvo;
        _ultimaDirecao = delta;

        switch (Mapa[alvo.X, alvo.Y])
        {
            case TipoDeCelula.Grama:
                Mapa[alvo.X, alvo.Y] = TipoDeCelula.Chao;
                break;

            case TipoDeCelula.Ouro:
                Mapa[alvo.X, alvo.Y] = TipoDeCelula.Chao;
                Personagem.Ouro += OuroPorPilha;
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

    private void ColetarItem(Posicao posicao)
    {
        if (!_itensNoChao.TryGetValue(posicao, out var item))
            return;

        _itensNoChao.Remove(posicao);
        Mapa[posicao.X, posicao.Y] = TipoDeCelula.Chao;
        Personagem.Mochila.Add(item);
        AdicionarMensagem($"Você pega {item.Nome}.");
    }

    private void CortarArvore(Posicao posicao)
    {
        Mapa[posicao.X, posicao.Y] = TipoDeCelula.Grama;
        Personagem.Madeira += MadeiraPorArvore;
        AdicionarMensagem($"Você corta a árvore e ganha {MadeiraPorArvore} de madeira.");
    }
}
