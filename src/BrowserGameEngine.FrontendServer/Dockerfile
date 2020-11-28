#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["BrowserGameEngine.FrontendServer/BrowserGameEngine.FrontendServer.csproj", "BrowserGameEngine.FrontendServer/"]
COPY ["BrowserGameEngine.GameDefinition.SCO/BrowserGameEngine.GameDefinition.SCO.csproj", "BrowserGameEngine.GameDefinition.SCO/"]
COPY ["BrowserGameEngine.GameModel/BrowserGameEngine.GameModel.csproj", "BrowserGameEngine.GameModel/"]
COPY ["BrowserGameEngine.GameDefinition/BrowserGameEngine.GameDefinition.csproj", "BrowserGameEngine.GameDefinition/"]
COPY ["BrowserGameEngine.BlazorClient/BrowserGameEngine.BlazorClient.csproj", "BrowserGameEngine.BlazorClient/"]
COPY ["BrowserGameEngine.Shared/BrowserGameEngine.ViewModels.csproj", "BrowserGameEngine.Shared/"]
COPY ["BrowserGameEngine.Persistence/BrowserGameEngine.Persistence.csproj", "BrowserGameEngine.Persistence/"]
COPY ["BrowserGameEngine.StatefulGameServer/BrowserGameEngine.StatefulGameServer.csproj", "BrowserGameEngine.StatefulGameServer/"]
RUN dotnet restore "BrowserGameEngine.FrontendServer/BrowserGameEngine.FrontendServer.csproj"
COPY . .
WORKDIR "/src/BrowserGameEngine.FrontendServer"
RUN dotnet build "BrowserGameEngine.FrontendServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BrowserGameEngine.FrontendServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BrowserGameEngine.FrontendServer.dll"]