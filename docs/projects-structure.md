# RedisGUI — Solution & Projects Structure Breakdown

The **RedisGUI** solution (`RedisGUI.slnx`) is organized into clean architectural layers, with source code under `src/` and automated NUnit unit and integration test suites under `test/`. All .NET projects target **.NET 11** (`net11.0`) with central package management (`Directory.Packages.props`) and shared analyzers (`Directory.Build.props`).

---

## Solution Overview

```text
RedisGUI.slnx
│
├── Directory.Build.props              # Shared .NET 11 compiler & StyleCop/Roslyn analyzer rules
├── Directory.Packages.props           # Central Package Management (CPM) across all projects
├── Dockerfile / docker-compose.yml    # Multi-stage Angular 22 + .NET 11 containerization & MySQL/Redis stack
│
├── build/
│   ├── Code Analysis/                 # Code Analysis Rules.ruleset, StyleCop.json, CustomDictionary.xml
│   └── RedisGUI.Docker/               # Docker Compose Visual Studio project
│
├── docs/                              # Architecture, diagrams, features, configuration, landing site & screenshots
│
├── src/
│   ├── RedisGUI.Domain/               # Pure DDD Entities, Value Objects, Domain Errors, Contracts
│   ├── RedisGUI.Shared/               # Lock-free In-Process Mediator, Primitives (Result, Error, PagedResult)
│   ├── RedisGUI.Application/          # CQRS Commands/Queries, FluentValidation, Behaviors, Job Handlers
│   ├── RedisGUI.Infrastructure/       # Redis Engine (StackExchange.Redis), EF Core + FusionCache, Background Jobs, SignalR Hubs
│   ├── RedisGUI.Server/               # ASP.NET Core 11 Minimal APIs, Problem Details Middleware, OpenTelemetry, SPA Host
│   └── RedisGUI.Client/               # Angular 22 Frontend SPA (Signals, Signal Forms, CDK Virtual Scroll, Material 22, Tailwind)
│
└── test/
    ├── RedisGUI.Shared.Tests/         # Unit tests for Result, ValueObject, PagedResult & Lock-free Mediator (NUnit)
    ├── RedisGUI.Domain.Tests/         # Unit tests for Entities, Value Objects, Guards & Domain Errors (NUnit)
    ├── RedisGUI.Application.Tests/    # CQRS Handler, Validator, Pipeline & Background Job Handler Tests (NUnit + Moq)
    ├── RedisGUI.Infrastructure.Tests/ # Cryptography, EF/MySQL Cache, Connection Manager, Cluster & Binary-Key Integration Tests
    └── RedisGUI.Server.Tests/         # WebApplicationFactory HTTP, Auth, Problem Details, Transfer & Background Job API Tests
```

---

## Detailed Breakdown by Project

### 1. `RedisGUI.Domain` (Inner Core — Zero External Dependencies)
Contains pure domain knowledge, business invariants, and service/repository abstractions:
- **Entities & Aggregates** (`Connection/`, `Users/`, `Audit/`):
  - `RedisConnection` abstract aggregate with `AnonymousRedisConnection` and `RedisConnectionWithCredentials`, supporting ownership (`ConnectionOwnerId`), Safe Mode (`IsReadOnly`), and cluster-aware connection string construction.
  - `User` aggregate with RBAC roles (`UserRole`: `Admin`, `Standard`, `ReadOnly`), activation state, and password updates.
  - `AuditLog` entity with typed `AuditAction`, `AuditMetadata`, and auditable marker contracts (`IAuditableContracts`).
- **Strongly-Typed Value Objects** (`Primitives/ValueObject`): `ConnectionHost`, `ConnectionPort`, `ConnectionName`, `DatabaseNumber`, `ConnectionCredentials`, `ConnectionHealth`, `ConnectionOwnerId`, `Email`, `PasswordHash`, `UserProfile`.
- **Redis & Telemetry Contracts** (`Connection/`, `Redis/`, `RedisMetrics/`):
  - `IRedisConnectionService`, `IRedisKeyService`, `IRedisValueService`, `IRedisCliService`, `IRedisDiagnosticsService`, `IRedisLoadTestService`, `IRedisSeedDataService`, `IRedisSnapshotService`, `IRedisTopologyRegistry` (`RedisServerMode`: `Unknown`, `Standalone`, `Cluster`, `Sentinel`).
  - Cursor-based pagination & binary-safe key identity models (`RedisKeyScanPage`, `RedisKeyMetadata`, `RedisValue`, `RedisValueType`, `PagedRedisValues`).
  - Domain exceptions and error catalog (`Errors/DomainErrors.cs`, `Exceptions/ErrorException.cs`, `Guards/Ensure.cs`).
- **Domain Events**: `ConnectionCreatedDomainEvent`, `ConnectionEstablishedDomainEvent`, `ConnectionAvailabilityChanged`, `UserRegisteredDomainEvent`, `UserLoggedInDomainEvent`, `UserPasswordChangedDomainEvent`, `UserDeactivatedDomainEvent`.

---

### 2. `RedisGUI.Shared` (Cross-Cutting Primitives & Lock-Free Mediator)
Zero-bloat shared building blocks used across all application and infrastructure layers:
- **Lock-Free In-Process Mediator** (`Mediator/`):
  - `Mediator`, `MediatorBuilder`, `HandlerRegistry`, `PipelineRegistry`, `NotificationRegistry`.
  - `PipelineRegistry` maintains registered open and closed pipeline behaviors using lock-free CAS array swaps (`Interlocked` / copy-on-write arrays) alongside `ConcurrentDictionary` type caches for allocation-light dispatch.
  - `NotificationRegistry` supports configurable `Parallel` and `Sequential` domain event dispatching.
- **Functional Primitives** (`Primitives/`, `Extensions/`):
  - Discriminated `Result` / `Result<T>` railway-oriented monads (`BindAsync`, `Map`, `Ensure`).
  - Structured `Error`, `ValidationFailureError`, `PagedResult<T>`, `ValueObject`, and async stream utilities (`AsyncEnumerableExtensions`).

---

### 3. `RedisGUI.Application` (Use-Case & Business Orchestration Layer)
Implements all application workflows grouped vertically by domain feature:
- **Core Infrastructure & Abstractions** (`Core/`, `Exceptions/`):
  - Pipeline Behaviors (`Behaviors/`): `ValidationBehavior` (`FluentValidation`), `LoggingBehavior`, and `AuditingBehavior` (driven by `[Auditable]` attribute).
  - Application Service Contracts (`Core/Services/`): `IConnectionAccessService`, `ICurrentUserService`, `IBackgroundJobHandler`, `IExportSessionFactory`, `IRedisMessagingOperations`.
- **Connections Feature** (`Connections/`):
  - `CreateConnection`, `UpdateConnection`, `DeleteConnection`, `CheckConnection`, `ToggleReadOnly`, `GetAllConnections`, `GetConnectionById`, `GetConnectionHealth`, `GetServerConfig`, `UpdateServerConfig`, `GetServerInfo`. Every mutating and inspection query enforces connection ownership & authorization via `IConnectionAccessService`.
- **Redis Operations & Data Transfer** (`Redis/`):
  - `Keys/`: `ScanRedisKeys` (cursor pagination + cluster awareness), `GetAllRedisValues`, `GetRedisValue` & `SetRedisValue` / `UpdateRedisValue` / `DeleteRedisKey` / `RenameKey` / `DuplicateKey` / `GetKeyMemoryUsage` (all supporting UTF-8 strings AND binary-safe Base64 `KeyBase64` keys), `GetKeyspaceInfo`, `BulkUpdateTtl`, `DeleteMultipleKeys`, `DeleteKeysByPattern`, `BulkOperations/` (`BulkKeyJobHandler`).
  - `DataTransfer/`: Streaming export/import (`ExportRedisDataQuery`, `ImportRedisDataCommand`) and background job handlers (`ExportRedisDataJobHandler`, `ImportRedisDataJobHandler`).
  - `Diagnostics/`: `GetSlowlogQuery`, `RunLoadTestCommand`, `RunStreamingLoadTestCommand`, `LoadTestJobHandler`.
  - `Tools/`: `ExecuteCliCommand`, `SeedSampleDataCommand`, `SeedSampleDataJobHandler`.
  - `Messaging/`: `GetChannelQuery`, `GetStreamQuery`, `PublishMessageCommand`, `MessagingConnectionAccess`.
- **Identity, Audit & Telemetry** (`Users/`, `Audit/`, `RedisMetrics/`):
  - `LoginUserCommand`, `RegisterUserCommand`, `GetAuditLogsQuery`, `GetRedisMetricsQuery`.

---

### 4. `RedisGUI.Infrastructure` (Data Persistence, Redis Engine, Background Jobs & SignalR)
Houses concrete implementations and runtime infrastructure:
- **Entity Framework Core + L2 Query Cache** (`Persistence/`):
  - `ApplicationDbContext` supporting both **MySQL 8.4** migrations (`Migrations/`) and **EF InMemory**.
  - Transparent SQL interceptor caching (`CachingInterceptor`, `DbCommandInterceptorProcessor`, parameter-aware `CacheKeyProvider` using SHA-256 over SQL text + typed `DbParameter` values, and `InMemoryTableRowsDataReader`) powered by **FusionCache**.
  - Domain event dispatch interceptor (`DomainEventsDispatcherInterceptor`), repositories (`RedisConnectionRepository`, `UserRepository`, `AuditLogRepository`, `UnitOfWork`).
- **Lock-Free Connection Pooling & Topology Registry** (`Redis/ConnectionManager/`, `Redis/`):
  - `RedisConnectionManager`: Lock-free `ConnectionEntry` state machine (`Volatile`, `Interlocked.CompareExchange`, `LockFreeCounter`) managing pooled `ConnectionMultiplexer` instances, dynamic config fingerprint invalidation (`RedisConfigurationSanitizer`), idle eviction, and drain timers.
  - `RedisTopologyRegistry`: `ConcurrentDictionary<Guid, RedisServerMode>` tracking runtime Standalone/Cluster/Sentinel detection.
  - `RedisServerResolver`: Primary/replica node resolution across standalone and cluster topologies.
- **Redis Services & Strategy/Factory Implementations** (`Redis/`):
  - `TypeHandlers/`: Strategy & factory (`IRedisTypeHandler`, `RedisTypeHandlerFactory`) for `String`, `Hash`, `List`, `Set`, `SortedSet`, and `Stream`.
  - `Services/`: `RedisKeyService` + `RedisKeyScan` (multi-primary cluster scanning + Base64 binary key support + HMAC-signed cluster continuation cursor tokens catching topology failovers), `RedisValueService`, `RedisDiagnosticsService` (`AllSections` + `CLUSTER NODES` parsing), `RedisLoadTestService` (`LoadTestOperations`, `BenchmarkResultBuilder`), `RedisSnapshotService`, `RedisSeedDataService` (`KeyNamingStrategies`, `SeedDataStrategies`), `RedisCliService`, `RedisMessagingService` + `RedisMessagingOperations` + `RedisMessagingMetricsCollector`.
  - `Common/`: `CommandLineTokenizer`, `RedisPatternMatcher`, `RedisRespFormatter`, `RedisInfoParser`.
- **Extensible Background Jobs & Live Hub** (`BackgroundJobs/`, `SignalR/`):
  - `BackgroundJobManager`: Bounded multi-worker `Channel<QueuedJob>` scheduler dispatching jobs dynamically via keyed/registered `IBackgroundJobHandler` implementations, emitting MPSC status/progress events (`JobEvent`), download leases to guard in-flight artifacts, per-owner quotas, and `RedisGUI.BackgroundJobs` OpenTelemetry meters.
  - `BackgroundJobSubscriptionRegistry`: Lock-free registry tracking active SignalR watchers per job and pushing `AccessRevoked` events when connection access changes.
  - Background Hosted Services: `RedisMetricsBackgroundJob` (sub-second live metrics pump) and `ConnectionHealthMonitor`.
  - SignalR Hubs (`SignalR/Hubs/`):
    - `BackgroundJobsHub` (`/hub/jobs`, `[Authorize]`): Job lifecycle, progress, completion, and `AccessRevoked`.
    - `RedisMetricsHub` (`/hub/metrics`): Live telemetry streaming via `NotificationService`.
    - `KeyChangeHub` (`/hub/keychange`): Lock-free key change notifications via `KeyChangeSubscriptionManager` & `KeyChangeNotificationService`.
    - `MessagingHub` (`/hub/messaging`): Live Redis Pub/Sub & Stream watcher with Cluster DB 0 awareness and `CanAccessAsync` checks.
- **Security & Configuration** (`Authentication/`, `Authorization/`, `Cryptography/`, `Configuration/`):
  - JWT bearer tokens (`JwtTokenGenerator`, `JwtSettings`), BCrypt password hashing (`PasswordHasher`), AES payload encryption (`PasswordEncryptor`), ownership authorization (`ConnectionAccessService`, `ConnectionOwnershipRequirement`), and runtime options (`DatabaseConfiguration`, `CacheConfiguration`, `MetricsCollectorOptions`, `BackgroundJobOptions`).

---

### 5. `RedisGUI.Server` (Web API Host & SPA Delivery)
ASP.NET Core 11 host configuring Minimal API endpoints, SignalR hubs, and middleware:
- **Modular Minimal APIs** (`Endpoints/`): Auto-discovered `IEndpoint` implementations under versioned `api/v1` route groups (`AuthenticationEndpoints`, `Connections/`, `Keys/`, `Jobs/BackgroundJobsEndpoint`, `Messaging/MessagingEndpoint`, `Diagnostics/GetSlowlogEndpoint`, `Tools/ExecuteCliEndpoint`, `Audit/GetAuditLogsEndpoint`). Includes query-safe key routes (`storage/{connectionId:guid}/key`) supporting arbitrary/binary Redis keys without path encoding issues.
- **Unified HTTP Problem Details** (`Extensions/ErrorResultExtensions.cs`, `Middleware/GlobalExceptionHandlerMiddleware.cs`): Semantic mapping of domain error codes to RFC 9457 `ProblemDetails` status codes (`400`, `401`, `403`, `404`, `408`, `409`, `502`, `503`, `500`) across both `Result` failures and `ErrorException`.
- **Pipeline, Rate Limiting & Telemetry** (`Middleware/`, `Observability/`, `Logging/`, `OpenApi/`, `Extensions/`):
  - `CorrelationIdMiddleware`, `RequestLoggingMiddleware`, fixed-window rate limiters (`auth`, `sensitive`), Brotli/Gzip response compression, Swagger/OpenAPI (`ConfigureSwaggerOptions`), Serilog logging, and OpenTelemetry tracing + metrics (`RedisGUI.*`).
  - `ApplicationBuilderExtensions.EnsureDatabaseReady`: Automatic MySQL EF Core migration retry loop and default admin/connection seeding (`admin@redisgui.local`).
  - `Program.cs`: Maps `/hub/metrics`, `/hub/keychange`, `/hub/messaging`, `/hub/jobs`, `/health`, `/health/ready`, and Angular SPA fallback (`wwwroot/`).

---

### 6. `RedisGUI.Client` (Angular 22 Modern SPA)
Standalone-component Angular 22 + Material 22 + Tailwind frontend powered by Signals and Signal Stores (`@ngrx/signals`):
- **Core Services & Stores** (`src/app/core/`):
  - Signal Stores (`core/stores/`): `AuthStore`, `ConnectionsStore`, `RedisKeysStore` (binary-safe `KeyBase64` key tracking, `serverMode`, cluster DB 0 normalization, incremental scan cursor state), `BackgroundJobsStore` (shared job tracking & unread notification state), `SideNavStore` (session-scoped `/home` fallback & reload persistence), `AuditStore`, `ServerInfoStore`, `BreadcrumbStore`, `SectionActionsStore`, `LoadingStore`, `SignalRStore`.
  - Services (`core/services/`): `BackgroundJobsService` (SignalR `/hub/jobs` with initial retry & `AccessRevoked` handling), `KeyChangeHubService`, `MetricsHubService`, `AuthService` (multi-store logout cleanup), interceptors (`auth.interceptor`, `error.interceptor`), and Strategy `value-handlers/` for Redis data types.
- **Shared Components & Design System** (`src/app/shared/`):
  - `JobNotificationCenterComponent`: Global header popover tracking active and recent background jobs (`Stop`, `Refresh`, `Download`, `Details`, `Dismiss`, `Clear finished`).
  - `RedisCliDrawerComponent`, `CommandPaletteComponent` (`Ctrl + K`), `TreeViewListComponent` (configurable delimiter tree), `JsonEditorComponent` (Monaco integration), `StatCountCardComponent`, `StatSummaryCardComponent`, `MetricSparklineCardComponent`, and reusable UI primitives (`shared/ui/`).
- **Feature Modules** (`src/app/features/`):
  - `auth/`: Sign-in / sign-up views with animated backdrop.
  - `dashboard/`: Server telemetry overview, connection cards with `Cluster` / `Protected` badges, and `EditConnectionComponent`.
  - `storage-management/`:
    - `StorageValuesListComponent`: Angular CDK `*cdkVirtualFor` virtual scrolling table with auto-pagination, glob pattern filtering, type counters, `BIN` binary-key badges, Cluster DB 0 locking, Tree Explorer, Bulk Actions (`BulkJobProgressComponent`), `ImportDialogComponent`, `ExportDialogComponent`, `LoadTestDialogComponent`, `SeedDataDialogComponent`, `SlowlogDialogComponent`, and `AddKeyComponent`.
    - `EditKeyComponent` & `StorageValueEditorComponent`: Dedicated editors for String/JSON, Hash, List, Set, SortedSet, and Stream.
    - `StorageResourcesInfoComponent`: Live telemetry pill strip.
  - `messaging-inspector/`: Live Redis Pub/Sub monitor and Stream inspector + real-time charts.
  - `health-monitoring/`, `audit-logs/`, `info/`: Connection health matrix, compliance audit logs, and server info views.

---

## 7. Automated Test Suites (`test/`)

All .NET test projects use **NUnit**, **FluentAssertions**, and **Moq** (with **Karma/Jasmine** for Angular frontend specs):

| Project | Framework | Scope Covered |
| :--- | :--- | :--- |
| `RedisGUI.Shared.Tests` | NUnit | `Result` railway chaining, `Error`, `PagedResult`, `ValueObject`, `MediatorBuilder`, lock-free `PipelineRegistry`, `NotificationRegistry` & behavior execution. |
| `RedisGUI.Domain.Tests` | NUnit | Domain aggregates (`RedisConnection`, `AuditLog`, `User`), value-object boundary validation, `Ensure` guards, `PagedRedisValues`, and `ErrorException`. |
| `RedisGUI.Application.Tests` | NUnit + Moq | CQRS command/query handlers, connection ownership/authorization enforcement, FluentValidation validators, behaviors, and background job handlers (`BulkKey`, `Export`, `Import`, `LoadTest`, `SeedSampleData`). |
| `RedisGUI.Infrastructure.Tests` | NUnit | AES encryption, JWT settings, MySQL schema & parameter-aware `CacheKeyProvider`, lock-free `RedisConnectionManager`, `BackgroundJobManager` queue/identity/quotas, standalone Redis & Cluster failover `SCAN`, binary key CRUD, and messaging integration tests. |
| `RedisGUI.Server.Tests` | NUnit + `WebApplicationFactory` | End-to-end HTTP endpoint integration: auth, RFC 9457 Problem Details status mapping, binary-key streaming export/import, background job lease downloads, and live `AccessRevoked` SignalR revocation. |
| `RedisGUI.Client` (`*.spec.ts`) | Jasmine / Karma | Signal stores (`BackgroundJobsStore`, `RedisKeysStore`, `SideNavStore` reload persistence), `BackgroundJobsService` SignalR/retry specs, and virtualized storage component cluster/pagination specs. |
