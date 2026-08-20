using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Itens;
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

        if (Mapa[alvo.X, alvo.Y] == TipoDeCelula.ArvoreFrutifera)
        {
            if (!PodeColher(alvo))
            {
                AdicionarMensagem("A árvore frutífera ainda não tem frutas para colher.");
                return false;
            }

            ColherFruta(alvo);
            AvancarTurno();
            return true;
        }

        if (Mapa[alvo.X, alvo.Y] == TipoDeCelula.Plantacao)
        {
            if (!PodeColher(alvo))
            {
                AdicionarMensagem("A plantação ainda não está pronta para colher.");
                return false;
            }

            ColherPlantacao(alvo);
            AvancarTurno();
            return true;
        }

        var monstro = _monstros.FirstOrDefault(m => m.Posicao == alvo);
        if (monstro is not null)
        {
            ResolverCombate(monstro);
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
                Ouro += OuroPorPilha;
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
        Madeira += MadeiraPorArvore;
        AdicionarMensagem($"Você corta a árvore e ganha {MadeiraPorArvore} de madeira.");
        FalarSobre(Personagem, "madeira");
    }

    private void ColherFruta(Posicao posicao)
    {
        Madeira += MadeiraPorArvoreFrutifera;
        Personagem.Mochila.Add(new Item("Fruta", TipoDeItem.Comida, ValorDaFruta));
        _proximaColheitaDisponivel[posicao] = _turno + TurnosDeRegrowthDaFruta;
        AdicionarMensagem($"Você colhe frutas e ganha {MadeiraPorArvoreFrutifera} de madeira.");
        FalarSobre(Personagem, "fruta");
    }

    // colheita unica -- ao contrario da fruta, a plantacao volta a ser Grama depois (ver TentarPlantar)
    private void ColherPlantacao(Posicao posicao)
    {
        Mapa[posicao.X, posicao.Y] = TipoDeCelula.Grama;
        _proximaColheitaDisponivel.Remove(posicao);
        Personagem.Mochila.Add(new Item("Vegetais", TipoDeItem.Comida, ValorDoVegetal));
        AdicionarMensagem("Você colhe a plantação e ganha vegetais.");
        FalarSobre(Personagem, "plantio");
    }

    // cuida so do ataque do jogador (dano no monstro, morte/loot) -- a retaliacao do monstro (se
    // sobreviver) acontece exclusivamente em MoverMonstros, chamado logo a seguir por AvancarTurno.
    // manter as duas coisas separadas evita o monstro bater duas vezes no mesmo turno (uma aqui,
    // outra em MoverMonstros, ja que ele continua adjacente depois do bump)
    private void ResolverCombate(Monstro monstro)
    {
        var dano = DanoBaseDoJogador + Personagem.AtaqueTotal;
        monstro.Vida = Math.Max(0, monstro.Vida - dano);

        if (monstro.Vida <= 0)
        {
            _monstros.Remove(monstro);
            Ouro += OuroPorMonstro;
            AdicionarMensagem($"Você derrota o monstro e ganha {OuroPorMonstro} moedas de ouro.");
            return;
        }

        AdicionarMensagem($"Você ataca o monstro e causa {dano} de dano.");
    }
}
