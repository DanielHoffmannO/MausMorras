using Mausmorras.Aplicativo.Renderizacao;
using Mausmorras.Nucleo.Jogo;

Application.Init();

const int larguraDoMundo = 220;
const int alturaDoMundo = 110;

var caminhoDoSave = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".mausmorras_save.json");

var estado = new EstadoDoJogo(larguraDoMundo, alturaDoMundo);

var janela = new Window
{
    Title = "Mausmorras (setas ou WASD move, I inventário, M minimapa, C constrói, F fogueira, P planta, Tab troca pessoa, Espaço alterna modo, F5 salva, F9 carrega, Esc sai)"
};

var visaoDoMapa = new VisaoDoMapa(estado, caminhoDoSave, larguraDoMundo, alturaDoMundo);
var painelStatus = new PainelStatus(() => visaoDoMapa.Estado) { Y = 0 };
var painelMensagens = new PainelMensagens(() => visaoDoMapa.Estado) { Y = Pos.AnchorEnd(6), Width = Dim.Percent(60) };
var painelConversa = new PainelConversa(() => visaoDoMapa.Estado) { X = Pos.Right(painelMensagens), Y = Pos.AnchorEnd(6), Width = Dim.Fill() };
var miniMapa = new MiniMapa(() => visaoDoMapa.Estado) { X = Pos.AnchorEnd(MiniMapa.LarguraTotal), Y = Pos.Bottom(painelStatus) };
var painelInventario = new PainelInventario(() => visaoDoMapa.Estado) { Visible = false, X = Pos.Center(), Y = Pos.Center() };

visaoDoMapa.Y = Pos.Bottom(painelStatus);
visaoDoMapa.Height = Dim.Fill(6);

void AtualizarPaineis()
{
    painelStatus.SetNeedsDraw();
    painelMensagens.SetNeedsDraw();
    painelConversa.SetNeedsDraw();
    miniMapa.SetNeedsDraw();
}

visaoDoMapa.AoAtualizar = AtualizarPaineis;

visaoDoMapa.AoAbrirInventario = () =>
{
    painelInventario.Visible = true;
    painelInventario.SetNeedsDraw();
    painelInventario.SetFocus();
};

visaoDoMapa.AoAlternarMiniMapa = () =>
{
    miniMapa.Visible = !miniMapa.Visible;
    miniMapa.SetNeedsDraw();
    visaoDoMapa.SetNeedsDraw();
};

painelInventario.AoAtualizarOutros = AtualizarPaineis;

painelInventario.AoFechar = () =>
{
    painelInventario.Visible = false;
    visaoDoMapa.SetFocus();
    AtualizarPaineis();
};

janela.Add(painelStatus, visaoDoMapa, miniMapa, painelMensagens, painelConversa, painelInventario);

if (File.Exists(caminhoDoSave))
{
    // adiado pro primeiro tick do loop principal em vez de chamado direto aqui -- um MessageBox
    // chamado antes da janela ter feito o primeiro layout via Application.Run trava o driver com
    // dimensoes de tela ainda invalidas (ArgumentOutOfRangeException ao auto-dimensionar o dialogo).
    // mesmo adiado, o tamanho reportado pelo driver pode continuar transitorio por mais alguns
    // ticks (ex: cliente web via ttyd, que so acerta o tamanho real depois de terminar de negociar
    // a conexao) -- entao espera parecer plausivel antes de tentar, com tentativas limitadas
    const int larguraMinimaParaDialogo = 40;
    const int alturaMinimaParaDialogo = 10;
    var tentativasRestantes = 20;

    void TentarMostrarDialogoDeBoot()
    {
        tentativasRestantes--;
        var tela = Application.Instance.Screen;
        var tamanhoPlausivel = tela.Width >= larguraMinimaParaDialogo && tela.Height >= alturaMinimaParaDialogo;

        if (!tamanhoPlausivel && tentativasRestantes > 0)
        {
            Application.AddTimeout(TimeSpan.FromMilliseconds(100), () => { TentarMostrarDialogoDeBoot(); return false; });
            return;
        }

        try
        {
            var opcao = MessageBox.Query(Application.Instance, "Mausmorras", "Encontrado um jogo salvo. Continuar?", "Continuar", "Novo jogo");
            if (opcao == 0 && visaoDoMapa.CarregarSemConfirmar())
                AtualizarPaineis();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // ultimo recurso -- se mesmo assim o tamanho reportado pelo driver enganar o calculo de
            // layout do dialogo, nao trava o jogo em loop: so segue direto pro novo jogo
            Console.Error.WriteLine($"[Mausmorras] Não foi possível mostrar o diálogo de boot (tela {tela.Width}x{tela.Height}): {ex.Message}");
        }
    }

    Application.AddTimeout(TimeSpan.Zero, () => { TentarMostrarDialogoDeBoot(); return false; });
}

var contadorDeErros = 0;
var ultimoErroEm = DateTime.MinValue;
const int maximoDeErrosConsecutivos = 5;
var janelaDeErroConsecutivo = TimeSpan.FromSeconds(2);

try
{
    Application.Run(janela, errorHandler: ex =>
    {
        // rede de seguranca: sem isso, uma excecao nao tratada durante o loop de eventos derrubava o
        // processo inteiro e podia deixar o terminal do jogador preso em alternate-screen (cursor
        // escondido, sem eco) ate um "reset" manual, ja que Application.Shutdown() nunca era alcancado
        var agora = DateTime.UtcNow;
        contadorDeErros = agora - ultimoErroEm <= janelaDeErroConsecutivo ? contadorDeErros + 1 : 1;
        ultimoErroEm = agora;

        Console.Error.WriteLine($"[Mausmorras] Erro inesperado durante a execução ({contadorDeErros}/{maximoDeErrosConsecutivos}): {ex}");

        if (contadorDeErros < maximoDeErrosConsecutivos)
            return true;

        // circuito de seguranca -- sem isso, um erro que se repete a cada iteracao do loop principal
        // (ex: um calculo de layout que nunca se resolve sozinho) cuspia a mesma excecao pra sempre
        // em vez de desistir de forma clara. devolver false relanca a excecao (ver finally abaixo
        // pra garantir que o terminal e restaurado mesmo assim)
        Console.Error.WriteLine($"[Mausmorras] {maximoDeErrosConsecutivos} erros em menos de {janelaDeErroConsecutivo.TotalSeconds:0}s -- encerrando em vez de continuar em loop.");
        return false;
    });
}
finally
{
    Application.Shutdown();
}
