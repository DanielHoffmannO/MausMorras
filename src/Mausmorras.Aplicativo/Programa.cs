using Mausmorras.Aplicativo.Renderizacao;
using Mausmorras.Nucleo.Jogo;

Application.Init();

var caminhoDoSave = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".mausmorras_save.json");

var estado = new EstadoDoJogo(largura: 220, altura: 110);

var janela = new Window
{
    Title = "Mausmorras (setas ou WASD move, I inventário, M minimapa, C constrói, Tab troca pessoa, Espaço alterna modo, F5 salva, F9 carrega, Esc sai)"
};

var visaoDoMapa = new VisaoDoMapa(estado, caminhoDoSave);
var painelStatus = new PainelStatus(() => visaoDoMapa.Estado) { Y = 0 };
var painelMensagens = new PainelMensagens(() => visaoDoMapa.Estado) { Y = Pos.AnchorEnd(6) };
var miniMapa = new MiniMapa(() => visaoDoMapa.Estado) { X = Pos.AnchorEnd(MiniMapa.LarguraTotal), Y = Pos.Bottom(painelStatus) };
var painelInventario = new PainelInventario(() => visaoDoMapa.Estado) { Visible = false, X = Pos.Center(), Y = Pos.Center() };

visaoDoMapa.Y = Pos.Bottom(painelStatus);
visaoDoMapa.Height = Dim.Fill(6);

void AtualizarPaineis()
{
    painelStatus.SetNeedsDraw();
    painelMensagens.SetNeedsDraw();
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

janela.Add(painelStatus, visaoDoMapa, miniMapa, painelMensagens, painelInventario);

Application.Run(janela);
Application.Shutdown();
