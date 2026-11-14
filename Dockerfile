# ==========================================
# Stage 1: Build Angular 22 Client Application
# ==========================================
FROM node:22-alpine AS client-build
WORKDIR /app/client

# Cache dependencies
COPY src/RedisGUI.Client/package*.json ./
RUN npm ci

# Copy client source code and build production bundle
COPY src/RedisGUI.Client/ ./
RUN npm run build -- --configuration production

# ==========================================
# Stage 2: Build & Publish .NET 10 Backend
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dotnet-build
WORKDIR /src

# Copy MSBuild properties and package definitions required for TargetFramework and CPM
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY RedisGUI.slnx ./
COPY src/RedisGUI.Shared/RedisGUI.Shared.csproj src/RedisGUI.Shared/
COPY src/RedisGUI.Domain/RedisGUI.Domain.csproj src/RedisGUI.Domain/
COPY src/RedisGUI.Application/RedisGUI.Application.csproj src/RedisGUI.Application/
COPY src/RedisGUI.Infrastructure/RedisGUI.Infrastructure.csproj src/RedisGUI.Infrastructure/
COPY src/RedisGUI.Server/RedisGUI.Server.csproj src/RedisGUI.Server/

RUN dotnet restore src/RedisGUI.Server/RedisGUI.Server.csproj /p:BuildingInsideDocker=true

# Copy all source files
COPY src/ src/

# Copy compiled Angular SPA to Server's wwwroot
COPY --from=client-build /app/client/dist/RedisGUI.Client/browser/ src/RedisGUI.Server/wwwroot/

# Publish Server in Release mode
WORKDIR /src/src/RedisGUI.Server
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:BuildingInsideDocker=true

# ==========================================
# Stage 3: Production Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=dotnet-build /app/publish .

ENTRYPOINT ["dotnet", "RedisGUI.Server.dll"]
