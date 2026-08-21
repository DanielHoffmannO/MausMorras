namespace Mausmorras.Nucleo.Geracao;

// pick ponderado de 3 opcoes -- usado tanto pro tier de item (GeradorDeMasmorra) quanto pro tipo de
// monstro (EstadoDoJogo.Transicoes), que tinham cada um sua propria copia do mesmo calculo
public static class SorteioPonderado
{
    public static int EscolherIndice(Random random, int peso1, int peso2, int peso3)
    {
        var sorteio = random.Next(peso1 + peso2 + peso3);
        if (sorteio < peso1) return 0;
        return sorteio < peso1 + peso2 ? 1 : 2;
    }
}
