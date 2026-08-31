# build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Mausmorras.Aplicativo/Mausmorras.Aplicativo.csproj -c Release -o /app

# runtime -- expoe o jogo via navegador usando ttyd (serve um terminal real por websocket/xterm.js),
# sem precisar mudar nada do jogo em si (ele so precisa de um terminal de verdade pra rodar)
FROM mcr.microsoft.com/dotnet/runtime:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends ttyd \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV TERM=xterm-256color
EXPOSE 7681
ENTRYPOINT ["ttyd", "-p", "7681", "-W", "dotnet", "Mausmorras.Aplicativo.dll"]
