# RedisGUI — Configuration & Settings Reference Guide

This document provides a comprehensive specification of all configuration options available in **RedisGUI**, including `appsettings.json`, environment variables, Docker parameters, database providers, and security settings.

---

## 1. Master Configuration File (`appsettings.json`)

The server reads configuration hierarchically from `appsettings.json`, environment-specific overrides (e.g. `appsettings.Development.json` or `appsettings.Production.json`), and operating system environment variables.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "StackExchange.Redis": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=redisgui;Uid=root;Pwd=root_password;"
  },
  "DatabaseProvider": "InMemory",
  "Redis": {
    "DefaultHost": "127.0.0.1",
    "DefaultPort": 6379,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "KeepAlive": 60,
    "AllowAdmin": true,
    "AbortOnConnectFail": false,
    "DefaultConnectionId": "ae3201ab-c3f1-4594-8f80-bbffd8d5eb65"
  },
  "Jwt": {
    "Secret": "SuperSecretKeyForDevelopmentOnlyPleaseChangeInProductionEnvironment12345!",
    "Issuer": "RedisGUI",
    "Audience": "RedisGUIClient",
    "AccessTokenExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "MetricsCollector": {
    "PoolingIntervalInSec": 5,
    "Enabled": true,
    "MaxHistoryDataPoints": 100
  },
  "HealthCheck": {
    "IntervalInSeconds": 15,
    "TimeoutInSeconds": 3
  },
  "Observability": {
    "ServiceName": "RedisGUI.Server",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317",
    "ExportTelemetry": false
  }
}
```

---

## 2. Configuration Settings Breakdown

### 2.1 Database & Persistence (`DatabaseProvider`)
Defines the persistence store for user accounts, connection profiles, and audit log history:

| Key | Options | Default | Description |
| :--- | :--- | :--- | :--- |
| `DatabaseProvider` | `InMemory`, `MySQL` | `InMemory` | Storage provider for `ApplicationDbContext`. `InMemory` is ideal for zero-setup local development and container testing. `MySQL` is recommended for multi-instance production. |
| `ConnectionStrings:DefaultConnection` | Valid ADO.NET Connection String | Local MySQL string | Active connection string when `DatabaseProvider` is set to `MySQL`. |

### 2.2 Redis Client Defaults (`Redis`)
Default settings applied when connecting to Redis nodes:

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `DefaultHost` | `string` | `127.0.0.1` | Default fallback Redis host address. |
| `DefaultPort` | `int` | `6379` | Default Redis TCP port. |
| `ConnectTimeout` | `int` (ms) | `5000` | Socket connection timeout in milliseconds. |
| `SyncTimeout` | `int` (ms) | `5000` | Synchronous command timeout before throwing a `TimeoutException`. |
| `KeepAlive` | `int` (sec) | `60` | Heartbeat keep-alive ping interval in seconds. |
| `AllowAdmin` | `bool` | `true` | Allows execution of administrative commands such as `INFO`, `CONFIG GET`, `SLOWLOG`, and `FLUSHDB`. |
| `AbortOnConnectFail` | `bool` | `false` | When `false`, StackExchange.Redis reconnects in the background instead of failing immediately upon startup. |
| `DefaultConnectionId` | `string` (GUID) | `ae3201ab-...` | ID of the pre-seeded default connection profile. |

### 2.3 JWT Authentication (`Jwt`)
Controls cryptographic security tokens issued to authenticated clients:

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Secret` | `string` | *Dev string (>= 32 chars)* | HMAC-SHA256 secret key for signing tokens. **Must be overridden in production.** |
| `Issuer` | `string` | `RedisGUI` | Valid token issuer identifier. |
| `Audience` | `string` | `RedisGUIClient` | Valid token audience claim. |
| `AccessTokenExpirationInMinutes` | `int` | `60` | Lifespan of access tokens before requiring refresh. |
| `RefreshTokenExpirationInDays` | `int` | `7` | Lifespan of refresh tokens stored on the client. |

### 2.4 Live Telemetry & Observability (`MetricsCollector`, `Observability`)
Controls live metric streaming and OpenTelemetry exports:

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `MetricsCollector:PoolingIntervalInSec` | `int` | `5` | Frequency of `server.InfoAsync()` polling for SignalR telemetry streaming. |
| `MetricsCollector:Enabled` | `bool` | `true` | Globally toggles real-time metrics collection background workers. |
| `MetricsCollector:MaxHistoryDataPoints` | `int` | `100` | Maximum number of in-memory data points retained for sparkline charts. |
| `HealthCheck:IntervalInSeconds` | `int` | `15` | Periodic connection health verification interval. |
| `Observability:ExportTelemetry` | `bool` | `false` | Enables OpenTelemetry trace and metric export over OTLP. |
| `Observability:OtlpEndpoint` | `string` | `http://localhost:4317` | Target gRPC/HTTP OTLP collector address. |

---

## 3. Environment Variables (Docker & Cloud Overrides)

Any setting in `appsettings.json` can be overridden using environment variables by replacing nested colons `:` with double underscores `__`:

| Environment Variable | Equivalent JSON Key | Example Production Value |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ASPNETCORE_URLS` | Server bindings | `http://+:5000` |
| `DatabaseProvider` | `DatabaseProvider` | `MySQL` |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | `Server=db;Port=3306;Database=redisgui;Uid=app;Pwd=Secret!` |
| `Jwt__Secret` | `Jwt:Secret` | `ProductionSecretKeyMustBeVeryLongAndSecureWithRandomEntropy123!` |
| `Redis__DefaultHost` | `Redis:DefaultHost` | `redis` or `redis.cluster.local` |
| `Redis__DefaultPort` | `Redis:DefaultPort` | `6379` |
| `MetricsCollector__PoolingIntervalInSec`| `MetricsCollector:PoolingIntervalInSec`| `3` |

---

## 4. Default Seeded Credentials

When starting with an `InMemory` database, the application automatically seeds an initial administrator account and demo connection:

* **Administrator Email**: `admin@redisgui.local`
* **Administrator Password**: `AdminPassword123!`
* **Pre-configured Connection**:
  * **Name**: `Local Redis`
  * **Host**: `127.0.0.1` (or `redis` inside Docker)
  * **Port**: `6379`
  * **Database Count**: `16` (DB 0 through DB 15)
  * **Safe Mode**: Disabled (`IsReadOnly = false`)
