using Mausmorras.Nucleo.Entidades;
using Mausmorras.Nucleo.Geracao;
using Mausmorras.Nucleo.Mapa;

namespace Mausmorras.Nucleo.Jogo;

public sealed partial class EstadoDoJogo
{
    public bool TentarConstruir()
    {
        if (Morto || Modo != ModoDeJogo.Jogando || LocalAtual != TipoDeLocal.Vila)
            return false;

        var area = CalcularAreaDaCasa(Personagem.Posicao, _ultimaDirecao);
        var portaExterna = CalcularPosicaoNaDirecao(Personagem.Posicao, _ultimaDirecao, 1);

        if (!AreaLivreParaConstrucao(Mapa, area) || !TerrenoConstruivel(Mapa, portaExterna) || !AreaLivreDePersonagens(area, portaExterna, Personagem))
        {
            AdicionarMensagem("Não há espaço livre suficiente para construir aqui.");
            return false;
        }

        var custo = area.Largura * area.Altura;
        if (Personagem.Madeira < custo)
        {
            AdicionarMensagem($"Madeira insuficiente para construir (precisa de {custo}).");
            return false;
        }

        var portaNaParede = CalcularPosicaoNaDirecao(Personagem.Posicao, _ultimaDirecao, DistanciaDaCasaAoPersonagem);
        ConstruirCasa(Mapa, area, portaNaParede, portaExterna);
        Personagem.Madeira -= custo;
        AdicionarMensagem("Você constrói uma casa.");
        AvancarTurno();
        return true;
    }

    public (Sala Area, Posicao PortaExterna, bool Valida) ObterPreviaDeConstrucao()
    {
        var area = CalcularAreaDaCasa(Personagem.Posicao, _ultimaDirecao);
        var portaExterna = CalcularPosicaoNaDirecao(Personagem.Posicao, _ultimaDirecao, 1);
        var custo = area.Largura * area.Altura;
        var valida = LocalAtual == TipoDeLocal.Vila && AreaLivreParaConstrucao(Mapa, area) && TerrenoConstruivel(Mapa, portaExterna)
                     && Personagem.Madeira >= custo && AreaLivreDePersonagens(area, portaExterna, Personagem);
        return (area, portaExterna, valida);
    }

    private bool AreaLivreDePersonagens(Sala area, Posicao portaExterna, Personagem construtor) =>
        !_personagens.Any(p => !ReferenceEquals(p, construtor) && EstaNaVila(p) &&
            (ContemPosicao(area, p.Posicao) || p.Posicao == portaExterna));

    private static bool ContemPosicao(Sala area, Posicao p) =>
        p.X >= area.X && p.X < area.X + area.Largura && p.Y >= area.Y && p.Y < area.Y + area.Altura;

    private static Sala CalcularAreaDaCasa(Posicao personagem, Posicao direcao)
    {
        var metade = TamanhoDaCasa / 2;

        if (direcao == Direcao.Sul)
            return new Sala(personagem.X - metade, personagem.Y + DistanciaDaCasaAoPersonagem, TamanhoDaCasa, TamanhoDaCasa);

        if (direcao == Direcao.Norte)
            return new Sala(personagem.X - metade, personagem.Y - DistanciaDaCasaAoPersonagem - TamanhoDaCasa + 1, TamanhoDaCasa, TamanhoDaCasa);

        if (direcao == Direcao.Leste)
            return new Sala(personagem.X + DistanciaDaCasaAoPersonagem, personagem.Y - metade, TamanhoDaCasa, TamanhoDaCasa);

        return new Sala(personagem.X - DistanciaDaCasaAoPersonagem - TamanhoDaCasa + 1, personagem.Y - metade, TamanhoDaCasa, TamanhoDaCasa);
    }

    private static Posicao CalcularPosicaoNaDirecao(Posicao personagem, Posicao direcao, int distancia) =>
        new(personagem.X + direcao.X * distancia, personagem.Y + direcao.Y * distancia);

    private bool AreaLivreParaConstrucao(MapaDaMasmorra mapa, Sala area)
    {
        for (var x = area.X; x < area.X + area.Largura; x++)
            for (var y = area.Y; y < area.Y + area.Altura; y++)
                if (!TerrenoConstruivel(mapa, new Posicao(x, y)))
                    return false;

        return true;
    }

    private bool TerrenoConstruivel(MapaDaMasmorra mapa, Posicao p) =>
        mapa.DentroDosLimites(p.X, p.Y) && mapa[p.X, p.Y] is TipoDeCelula.Grama or TipoDeCelula.Chao;

    private void ConstruirCasa(MapaDaMasmorra mapa, Sala area, Posicao portaNaParede, Posicao portaExterna)
    {
        for (var x = area.X; x < area.X + area.Largura; x++)
            for (var y = area.Y; y < area.Y + area.Altura; y++)
                mapa[x, y] = TipoDeCelula.Casa;

        for (var x = area.X + 1; x < area.X + area.Largura - 1; x++)
            for (var y = area.Y + 1; y < area.Y + area.Altura - 1; y++)
                mapa[x, y] = TipoDeCelula.PisoDaCasa;

        mapa[portaNaParede.X, portaNaParede.Y] = TipoDeCelula.Porta;
        mapa[portaExterna.X, portaExterna.Y] = TipoDeCelula.Porta;
        _existeCasaNaVila = true;
        _primeiroAbrigoConstruido = true;
    }

    private void ConstruirFogueira(MapaDaMasmorra mapa, Posicao posicao)
    {
        mapa[posicao.X, posicao.Y] = TipoDeCelula.Fogueira;
        _fogueirasAtivas.Add((posicao, _turno + DuracaoDaFogueira));
        _primeiroAbrigoConstruido = true;
    }

    public bool TentarConstruirFogueira()
    {
        if (Morto || Modo != ModoDeJogo.Jogando || LocalAtual != TipoDeLocal.Vila)
            return false;

        var posicao = CalcularPosicaoNaDirecao(Personagem.Posicao, _ultimaDirecao, 1);

        if (!TerrenoConstruivel(Mapa, posicao) || PosicaoOcupadaPorOutroPersonagem(posicao, Personagem))
        {
            AdicionarMensagem("Não há espaço livre suficiente para construir aqui.");
            return false;
        }

        if (Personagem.Madeira < CustoDaFogueira)
        {
            AdicionarMensagem($"Madeira insuficiente para construir (precisa de {CustoDaFogueira}).");
            return false;
        }

        ConstruirFogueira(Mapa, posicao);
        Personagem.Madeira -= CustoDaFogueira;
        AdicionarMensagem("Você constrói uma fogueira.");
        AvancarTurno();
        return true;
    }

    public (Posicao Posicao, bool Valida) ObterPreviaDeFogueira()
    {
        var posicao = CalcularPosicaoNaDirecao(Personagem.Posicao, _ultimaDirecao, 1);
        var valida = LocalAtual == TipoDeLocal.Vila && TerrenoConstruivel(Mapa, posicao)
                     && Personagem.Madeira >= CustoDaFogueira && !PosicaoOcupadaPorOutroPersonagem(posicao, Personagem);
        return (posicao, valida);
    }

    private bool PosicaoOcupadaPorOutroPersonagem(Posicao p, Personagem construtor) =>
        _personagens.Any(pe => !ReferenceEquals(pe, construtor) && EstaNaVila(pe) && pe.Posicao == p);
}
