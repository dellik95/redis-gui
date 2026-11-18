# RedisGUI — Docker Deployment & Containerization Guide

This guide describes how to build, run, and verify **RedisGUI** using Docker and Docker Compose across development, CI, and production environments.

---

## 1. Quickstart with Docker Compose

Launch the full **RedisGUI** stack (ASP.NET Core 11 backend + Angular 22 SPA, Redis 7.4, and MySQL 8.4):

```bash
docker compose up -d --build
```

Once healthy, navigate to:
- **Web UI & API**: [http://localhost:5000](http://localhost:5000)
- **Redis Server**: `localhost:6379`
- **MySQL Database**: `localhost:3306`

### Default Seeded Credentials
- **Email**: `admin@redisgui.local`
- **Password**: `AdminPassword123!`

---

## 2. Docker Compose Architecture (`docker-compose.yml`)

The included `docker-compose.yml` provisions three interconnected services on the `redisgui-network` bridge network:

1. **`redis` (`redis:7.4-alpine`)**: Persistent Redis instance (`--appendonly yes`, volume `redis_data`, port `6379`) with `redis-cli ping` healthcheck.
2. **`mysql` (`mysql:8.4`)**: Persistent metadata & RBAC store (`redisgui` database, volume `mysql_data`, port `3306`) with `mysqladmin ping` healthcheck.
3. **`app` (`Dockerfile`)**: Multi-stage production container exposing port `8080` internally (mapped to `5000:8080` on the host), waiting on both `redis` and `mysql` healthchecks.

### Key Container Environment Variables

```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:8080
Database__IsInMemory: "false"
Database__ConnectionString: Server=mysql;Port=3306;Database=redisgui;Uid=redisgui;Pwd=redisguipass;
JwtSettings__SecretKey: <min-32-char-production-secret>
JwtSettings__Issuer: RedisGUI
JwtSettings__Audience: RedisGUI.Client
JwtSettings__ExpirationMinutes: 1440
Cryptography__SecurityKey: <encryption-key>
Redis__DefaultHost: redis
Redis__DefaultPort: 6379
```

On first startup with `Database__IsInMemory=false`, `ApplicationBuilderExtensions.EnsureDatabaseReady` automatically retries database readiness, applies all EF Core MySQL migrations, and seeds the default administrator account and initial Redis connection.

---

## 3. Multi-Stage `Dockerfile`

The root `Dockerfile` produces a lean self-contained production image in three stages:

1. **Stage 1 (`node:26-alpine` — `client-build`)**: Installs npm dependencies via `npm ci` and builds the Angular 22 production bundle (`npm run build -- --configuration production`).
2. **Stage 2 (`mcr.microsoft.com/dotnet/sdk:11.0` — `dotnet-build`)**: Copies `Directory.Build.props`, `Directory.Packages.props`, code analysis rulesets, and C# `.csproj` manifests, restores NuGet dependencies, embeds the compiled Angular SPA into `src/RedisGUI.Server/wwwroot/`, and publishes `RedisGUI.Server` in `Release` mode.
3. **Stage 3 (`mcr.microsoft.com/dotnet/aspnet:11.0` — `runtime`)**: Copies only the published `/app/publish` artifacts and launches `dotnet RedisGUI.Server.dll` on port `8080`.

---

## 4. Health Probes & Useful Commands

### Readiness & Liveness Endpoints
- `GET /health` — ASP.NET Core & database liveness check (returns `200 Healthy` when operational).
- `GET /health/ready` — Readiness probe endpoint for container orchestrators.

*(Browser navigation requests with `Sec-Fetch-Dest: document` to `/health` are automatically routed to `/index.html` so the Angular Health Monitoring page works seamlessly at `/health`.)*

### Useful Docker Commands

```bash
# Build production image directly
docker build -t redisgui:local -f Dockerfile .

# Stream application logs
docker compose logs -f app

# Stream Redis or MySQL logs
docker compose logs -f redis mysql

# Stop stack cleanly
docker compose down

# Stop and remove persistent volumes
docker compose down -v
```
