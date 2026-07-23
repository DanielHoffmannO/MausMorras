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

        if (!AreaLivreParaConstrucao(area) || !TerrenoConstruivel(portaExterna) || !AreaLivreDePersonagens(area, portaExterna))
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
        ConstruirCasa(area, portaNaParede, portaExterna);
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
        var valida = LocalAtual == TipoDeLocal.Vila && AreaLivreParaConstrucao(area) && TerrenoConstruivel(portaExterna)
                     && Personagem.Madeira >= custo && AreaLivreDePersonagens(area, portaExterna);
        return (area, portaExterna, valida);
    }

    private bool AreaLivreDePersonagens(Sala area, Posicao portaExterna) =>
        !_personagens.Any(p => !ReferenceEquals(p, Personagem) &&
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

    private bool AreaLivreParaConstrucao(Sala area)
    {
        for (var x = area.X; x < area.X + area.Largura; x++)
            for (var y = area.Y; y < area.Y + area.Altura; y++)
                if (!TerrenoConstruivel(new Posicao(x, y)))
                    return false;

        return true;
    }

    private bool TerrenoConstruivel(Posicao p) =>
        Mapa.DentroDosLimites(p.X, p.Y) && Mapa[p.X, p.Y] is TipoDeCelula.Grama or TipoDeCelula.Chao;

    private void ConstruirCasa(Sala area, Posicao portaNaParede, Posicao portaExterna)
    {
        for (var x = area.X; x < area.X + area.Largura; x++)
            for (var y = area.Y; y < area.Y + area.Altura; y++)
                Mapa[x, y] = TipoDeCelula.Casa;

        for (var x = area.X + 1; x < area.X + area.Largura - 1; x++)
            for (var y = area.Y + 1; y < area.Y + area.Altura - 1; y++)
                Mapa[x, y] = TipoDeCelula.Chao;

        Mapa[portaNaParede.X, portaNaParede.Y] = TipoDeCelula.Porta;
        Mapa[portaExterna.X, portaExterna.Y] = TipoDeCelula.Porta;
    }
}
