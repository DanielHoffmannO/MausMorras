using Mausmorras.Aplicativo.Renderizacao;
using Mausmorras.Nucleo.Jogo;

Application.Init();

var caminhoDoSave = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".mausmorras_save.json");

var estado = new EstadoDoJogo(largura: 220, altura: 110);

var janela = new Window
{
    Title = "Mausmorras (setas ou WASD para mover, F5 salva, F9 carrega, Esc para sair)"
};

var visaoDoMapa = new VisaoDoMapa(estado, caminhoDoSave);
var painelStatus = new PainelStatus(() => visaoDoMapa.Estado) { Y = 0 };
var painelMensagens = new PainelMensagens(() => visaoDoMapa.Estado) { Y = Pos.AnchorEnd(6) };

visaoDoMapa.Y = Pos.Bottom(painelStatus);
visaoDoMapa.Height = Dim.Fill(6);

visaoDoMapa.AoAtualizar = () =>
{
    painelStatus.SetNeedsDraw();
    painelMensagens.SetNeedsDraw();
};

janela.Add(painelStatus);
janela.Add(visaoDoMapa);
janela.Add(painelMensagens);

Application.Run(janela);
Application.Shutdown();
