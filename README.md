# Mausmorras

Um jogo de terminal em português: metade roguelike de masmorra, metade sim de colônia. Você controla uma vila de fundadores que precisam sobreviver — caçar, plantar, construir casas, ter filhos, fugir do frio — e pode descer pra uma masmorra pra buscar ouro e itens.

## Instalação (passo a passo, sem economizar detalhe)

### 1. Instale o .NET 10

O jogo precisa do **SDK do .NET 10** (não é só o "runtime", tem que ser o SDK).

**Windows:**
1. Acesse https://dotnet.microsoft.com/download/dotnet/10.0
2. Baixe o instalador "SDK" pra Windows (x64) e execute
3. Abra o Prompt de Comando (cmd) ou PowerShell

**Linux (Ubuntu/Debian):**
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```
Depois adicione ao PATH (cole isso no terminal, e também no final do arquivo `~/.bashrc` pra não precisar repetir):
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$PATH
```

**macOS:**
1. Acesse https://dotnet.microsoft.com/download/dotnet/10.0
2. Baixe o instalador "SDK" pra macOS e execute

Pra confirmar que instalou certo, feche e abra o terminal de novo e rode:
```bash
dotnet --version
```
Se aparecer um número tipo `10.0.x`, deu certo.

### 2. Baixe o projeto

Se você recebeu uma pasta com o jogo, só entre nela pelo terminal:
```bash
cd caminho/para/MausMorras
```

### 3. Rode o jogo

Dentro da pasta do projeto:
```bash
dotnet run --project src/Mausmorras.Aplicativo
```

Na primeira vez vai demorar um pouco (baixando dependências). Da segunda em diante é rápido.

Pronto — o jogo abre direto no terminal.

> **Dica pra quem não curte digitar comando toda vez:** crie um atalho.
> - Linux/macOS: dentro da pasta do projeto, rode `echo 'dotnet run --project src/Mausmorras.Aplicativo' > jogar.sh && chmod +x jogar.sh`. Depois é só abrir o terminal na pasta e rodar `./jogar.sh`.
> - Windows: crie um arquivo `jogar.bat` com o conteúdo `dotnet run --project src\Mausmorras.Aplicativo` e dê duplo clique nele.

## Como jogar

| Tecla | Ação |
|---|---|
| Setas (↑ ↓ ← →) | Mover o personagem |
| `Tab` | Trocar de personagem controlado (quando há mais de um fundador vivo) |
| `Espaço` | Alternar entre modo Jogando e modo Observador (a vila continua vivendo sozinha) |
| `I` | Abrir/fechar o inventário |
| `M` | Mostrar/esconder o minimapa |
| `F5` | Salvar jogo |
| `F9` | Carregar jogo |
| `Esc` | Fechar janela aberta (inventário) ou sair do jogo |

Dentro do inventário: setas para navegar, `Tab` troca de coluna, `Enter` usa/equipa o item selecionado, `Delete` descarta, `I` ou `Esc` fecha.

## Se der algum erro

- **"comando 'dotnet' não encontrado"**: o passo 1 não terminou certo, ou o terminal precisa ser reaberto depois de instalar. Feche e abra o terminal de novo.
- **Erro de compilação estranho**: confirme com `dotnet --version` que é a versão 10.x — versões mais antigas do .NET não rodam esse projeto.
- Qualquer outro problema, rode `dotnet build Mausmorras.slnx` na pasta raiz do projeto e leia a mensagem de erro — ela costuma dizer exatamente o que falta.
