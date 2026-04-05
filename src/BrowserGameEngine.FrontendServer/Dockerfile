FROM node:22-alpine AS react-build
WORKDIR /app
COPY ReactClient/package.json ReactClient/package-lock.json* ./
RUN npm ci
COPY ReactClient/ ./
ARG VITE_DEV_AUTH=true
ENV VITE_DEV_AUTH=$VITE_DEV_AUTH
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Restore dependencies in a separate layer so they are cached across source-only changes
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BrowserGameEngine.FrontendServer/BrowserGameEngine.FrontendServer.csproj", "BrowserGameEngine.FrontendServer/"]
COPY ["BrowserGameEngine.GameDefinition.SCO/BrowserGameEngine.GameDefinition.SCO.csproj", "BrowserGameEngine.GameDefinition.SCO/"]
COPY ["BrowserGameEngine.GameModel/BrowserGameEngine.GameModel.csproj", "BrowserGameEngine.GameModel/"]
COPY ["BrowserGameEngine.GameDefinition/BrowserGameEngine.GameDefinition.csproj", "BrowserGameEngine.GameDefinition/"]
COPY ["BrowserGameEngine.Shared/BrowserGameEngine.ViewModels.csproj", "BrowserGameEngine.Shared/"]
COPY ["BrowserGameEngine.Persistence/BrowserGameEngine.Persistence.csproj", "BrowserGameEngine.Persistence/"]
COPY ["BrowserGameEngine.Persistence.S3/BrowserGameEngine.Persistence.S3.csproj", "BrowserGameEngine.Persistence.S3/"]
COPY ["BrowserGameEngine.StatefulGameServer/BrowserGameEngine.StatefulGameServer.csproj", "BrowserGameEngine.StatefulGameServer/"]
RUN dotnet restore "BrowserGameEngine.FrontendServer/BrowserGameEngine.FrontendServer.csproj"
COPY . .
COPY --from=react-build /app/dist BrowserGameEngine.FrontendServer/wwwroot/
WORKDIR "/src/BrowserGameEngine.FrontendServer"
# Publish directly; dotnet publish implies build, so a separate build step is redundant
ARG COMMIT_SHA=dev
RUN dotnet publish "BrowserGameEngine.FrontendServer.csproj" -c Release --no-restore -o /app/publish /p:InformationalVersion=$COMMIT_SHA

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BrowserGameEngine.FrontendServer.dll"]
