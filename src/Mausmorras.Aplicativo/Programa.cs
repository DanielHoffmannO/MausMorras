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
    // dimensoes de tela ainda invalidas (ArgumentOutOfRangeException ao auto-dimensionar o dialogo)
    Application.AddTimeout(TimeSpan.Zero, () =>
    {
        var opcao = MessageBox.Query(Application.Instance, "Mausmorras", "Encontrado um jogo salvo. Continuar?", "Continuar", "Novo jogo");
        if (opcao == 0 && visaoDoMapa.CarregarSemConfirmar())
            AtualizarPaineis();

        return false;
    });
}

Application.Run(janela, errorHandler: ex =>
{
    // rede de seguranca: sem isso, uma excecao nao tratada durante o loop de eventos derrubava o
    // processo inteiro e podia deixar o terminal do jogador preso em alternate-screen (cursor
    // escondido, sem eco) ate um "reset" manual, ja que Application.Shutdown() nunca era alcancado
    Console.Error.WriteLine($"[Mausmorras] Erro inesperado durante a execução: {ex}");
    return true;
});
Application.Shutdown();
