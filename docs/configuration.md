# RedisGUI — Configuration & Settings Reference Guide

This document describes all configuration sections used by **RedisGUI.Server** (`appsettings.json`, `appsettings.Development.json`, environment variable overrides, and container defaults).

---

## 1. Master Configuration Structure (`appsettings.json`)

ASP.NET Core loads configuration hierarchically from `appsettings.json`, `appsettings.{Environment}.json`, and environment variables (replacing `:` with `__`):

```json
{
  "Database": {
    "IsInMemory": false,
    "ConnectionStringMSQL": "",
    "ConnectionString": "Server=localhost;Port=3306;Database=redisgui;Uid=root;Pwd=your_password;"
  },
  "Cache": {
    "KeyPrefix": "",
    "Duration": "00:05:00",
    "CachedTables": [
      "AnonymousRedisConnection",
      "RedisConnectionWithCredentials"
    ],
    "IgnoredTables": []
  },
  "Cryptography": {
    "SecurityKey": "CHANGE_TO_SECURE_ENCRYPTION_KEY_IN_PRODUCTION!"
  },
  "JwtSettings": {
    "SecretKey": "CHANGE_TO_SECURE_32+_CHAR_JWT_SECRET_IN_PRODUCTION!",
    "Issuer": "RedisGUI",
    "Audience": "RedisGUI.Client",
    "ExpirationMinutes": 1440
  },
  "MetricsCollector": {
    "PoolingIntervalInSec": 5
  },
  "Redis": {
    "ConnectionManager": {
      "MaxConnections": 50,
      "ConnectionTimeout": "00:00:15",
      "SyncTimeout": "00:00:10",
      "IdleTimeout": "00:05:00",
      "EvictionInterval": "00:00:30",
      "DrainTimeout": "00:00:02",
      "EnableDockerFallback": false
    }
  },
  "BackgroundJobs": {
    "WorkerCount": 2,
    "QueueCapacity": 100,
    "MaxRetainedJobs": 512,
    "MaxActiveParameterBytes": 67108864,
    "ResultRetention": "00:30:00",
    "CleanupInterval": "00:05:00"
  }
}
```

---

## 2. Configuration Sections Breakdown

### 2.1 Database & Persistence (`Database`)
Bound to `DatabaseConfiguration`:

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Database:IsInMemory` | `bool` | `false` | Uses Entity Framework In-Memory storage when `true`, or MySQL 8.x with automatic EF Core migrations when `false`. |
| `Database:ConnectionString` | `string` | `""` | Active MySQL connection string when `IsInMemory` is `false`. |

### 2.2 EF Core Query Cache (`Cache`)
Bound to `CacheConfiguration` (backed by **FusionCache** + SQL command interceptor with parameter-aware hashing):

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Cache:KeyPrefix` | `string` | `""` | Optional prefix for query cache entries. |
| `Cache:Duration` | `TimeSpan` | `00:10:00` (`appsettings`: `00:05:00`) | Expiration TTL for cached table queries. |
| `Cache:CachedTables` | `string[]` | `[]` (`appsettings`: `AnonymousRedisConnection`, `RedisConnectionWithCredentials`) | Tables eligible for transparent L2 caching. |
| `Cache:IgnoredTables` | `string[]` | `[]` | Tables explicitly excluded from query caching. |

### 2.3 Cryptography & JWT Authentication (`Cryptography`, `JwtSettings`)
Bound to `CryptographyConfiguration` and `JwtSettings` (with data-annotation validation):

| Key | Type | Default / Constraint | Description |
| :--- | :--- | :--- | :--- |
| `Cryptography:SecurityKey` | `string` | `[Required]` | AES key used to encrypt/decrypt stored Redis passwords. **Must be overridden in production.** |
| `JwtSettings:SecretKey` | `string` | `[Required]`, `[MinLength(32)]` | HMAC-SHA256 signing secret for JWT bearer tokens & SignalR hub auth. |
| `JwtSettings:Issuer` | `string` | `[Required]` (`RedisGUI`) | Expected token `iss` claim. |
| `JwtSettings:Audience` | `string` | `[Required]` (`RedisGUI.Client`) | Expected token `aud` claim. |
| `JwtSettings:ExpirationMinutes` | `int` | `60` (`appsettings`: `1440`) | JWT access token lifetime in minutes. |

### 2.4 Live Telemetry (`MetricsCollector`)
Bound to `MetricsCollectorOptions`:

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `MetricsCollector:PoolingIntervalInSec` | `int` | `1` (`appsettings`: `5`) | Interval (seconds) between `INFO` scrapes broadcast by `RedisMetricsBackgroundJob` to `/hub/metrics` subscribers. |

### 2.5 Lock-Free Redis Connection Pool (`Redis:ConnectionManager`)
Bound to `RedisConnectionManagerOptions`:

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Redis:ConnectionManager:MaxConnections` | `int` | `50` | Maximum concurrent pooled `ConnectionMultiplexer` entries. |
| `Redis:ConnectionManager:ConnectionTimeout` | `TimeSpan` | `00:00:05` (`appsettings`: `00:00:15`) | Socket connect timeout. |
| `Redis:ConnectionManager:IdleTimeout` | `TimeSpan` | `00:05:00` | Inactivity window before idle connections become eligible for eviction. |
| `Redis:ConnectionManager:EvictionInterval` | `TimeSpan` | `00:00:30` | Sweep cadence for stale/idle connections. |
| `Redis:ConnectionManager:DrainTimeout` | `TimeSpan` | `00:00:02` | Grace period after eviction/replacement before multiplexer disposal so active requests drain cleanly. |
| `Redis:ConnectionManager:EnableDockerFallback` | `bool` | `false` | When `true`, allows local-dev fallback from Docker hostname `redis` to `127.0.0.1` (`false` by default for strict fail-fast behavior). |

### 2.6 Background Jobs Coordinator (`BackgroundJobs`)
Bound to `BackgroundJobOptions` (data-annotation validated on startup):

| Key | Type | Range / Default | Description |
| :--- | :--- | :--- | :--- |
| `BackgroundJobs:WorkerCount` | `int` | `1–32` (default `2`) | Concurrent worker loops draining queued background jobs (`bulk-keys`, `redis-export`, `redis-import`, `load-test`, `seed-data`). |
| `BackgroundJobs:QueueCapacity` | `int` | `1–10000` (default `100`) | Bounded in-memory queue capacity (`Channel<QueuedJob>`). |
| `BackgroundJobs:MaxRetainedJobs` | `int` | `1–50000` (default `512`) | Maximum retained completed/active job records before oldest terminal entries are pruned. |
| `BackgroundJobs:MaxActiveParameterBytes` | `long` | `1 KB–1 GB` (default `64 MB`) | Total serialized parameter memory quota across queued/running jobs. |
| `BackgroundJobs:ResultRetention` | `TimeSpan` | `00:30:00` | Retention lifetime for completed job results/export downloads (protected by active download leases). |
| `BackgroundJobs:CleanupInterval` | `TimeSpan` | `00:05:00` | Periodic cleanup sweep cadence for expired terminal job artifacts. |

---

## 3. Environment Variable Overrides & Seed Defaults

| Environment Variable | Purpose / Target Setting | Example Production Value |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Runtime hosting environment | `Production` |
| `ASPNETCORE_URLS` | Kestrel HTTP bindings | `http://+:8080` |
| `Database__IsInMemory` | `Database:IsInMemory` | `false` |
| `Database__ConnectionString` | `Database:ConnectionString` | `Server=mysql;Port=3306;Database=redisgui;Uid=redisgui;Pwd=Secret!` |
| `Cryptography__SecurityKey` | `Cryptography:SecurityKey` | `<32+-char-random-secret>` |
| `JwtSettings__SecretKey` | `JwtSettings:SecretKey` | `<32+-char-jwt-signing-secret>` |
| `BackgroundJobs__WorkerCount` | `BackgroundJobs:WorkerCount` | `4` |
| `Redis__DefaultHost` | Default seeded Redis host when no connections exist | `redis` or `127.0.0.1` |
| `Redis__DefaultPort` | Default seeded Redis port when no connections exist | `6379` |
| `Redis__ConnectionManager__EnableDockerFallback` | `Redis:ConnectionManager:EnableDockerFallback` | `false` |

### Default Seeded Credentials & Connection
When starting against an empty database, `ApplicationBuilderExtensions.SeedDefaultUserAndConnection` creates:
- **Administrator Email**: `admin@redisgui.local`
- **Administrator Password**: `AdminPassword123!`
- **Default Connection**: `Local Redis` (`Redis__DefaultHost` / `127.0.0.1`, `Redis__DefaultPort` / `6379`, Safe Mode disabled).

### Integration Test Environment Variables
- `REDISGUI_TEST_REDIS` — Standalone Redis endpoint for integration suites (`127.0.0.1:16391,allowAdmin=true`; `RedisDiagnosticsIntegrationTests` expects `127.0.0.1:16391`).
- `REDISGUI_TEST_MYSQL` — MySQL 8.x test connection string (`Server=127.0.0.1;Port=13306;Database=redisgui_test;User=root;Password=ci-password`).
- `REDISGUI_TEST_CLUSTER` — Redis Cluster endpoints (`...,allowAdmin=true`) for primary traversal and Cluster restore tests.
- `REDISGUI_TEST_CLUSTER_FAILOVER`, `REDISGUI_TEST_CLUSTER_FAILOVER_PRIMARY`, `REDISGUI_TEST_CLUSTER_FAILOVER_REPLICA` — Disposable cluster configuration for testing scan cursor invalidation after `CLUSTER FAILOVER`.
