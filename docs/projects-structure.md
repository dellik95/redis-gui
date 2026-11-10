# RedisGUI — Solution Projects & Architecture Breakdown

The **RedisGUI** solution (`RedisGUI.slnx`) is engineered according to Robert C. Martin's **Clean Architecture** principles with strict separation of concerns across concentric layers. The solution contains 10 projects: 5 core .NET modules, an Angular 19 SPA client, and 4 xUnit/Moq test suites.

```
RedisGUI.slnx
│
├── 📂 src/
│   ├── 📦 RedisGUI.Domain/           # Enterprise entities, value objects, domain contracts, and enums
│   ├── 📦 RedisGUI.Shared/           # Functional primitives: Result<T>, Error, and in-process CQRS pipeline
│   ├── 📦 RedisGUI.Application/      # CQRS Commands, Queries, FluentValidation rules, and MediatR behaviors
│   ├── 📦 RedisGUI.Infrastructure/   # StackExchange.Redis adapters, Strategy type handlers, EF Core, and SignalR
│   ├── 📦 RedisGUI.Server/           # ASP.NET Core 10 host, Minimal APIs, SignalR hubs, and OpenAPI/Scalar
│   └── 📦 RedisGUI.Client/           # Angular 19 Standalone, Signals, Tailwind CSS, and Monaco Editor
│
└── 📂 test/
    ├── 🧪 RedisGUI.Domain.Tests/
    ├── 🧪 RedisGUI.Shared.Tests/
    ├── 🧪 RedisGUI.Application.Tests/
    └── 🧪 RedisGUI.Infrastructure.Tests/
```

---

## 1. `RedisGUI.Domain` (Domain Layer)

The innermost layer of the system. It contains enterprise business logic and contracts with **zero external dependencies** on databases, Web frameworks, or serialization libraries.

### Core Entities & Domain Models:
* **`Connection/RedisConnection`**: Entity representing a stored connection profile to a Redis instance:
  * `ServerHost` (Value Object), `ServerPort`, `DatabaseCount`, `Name`, `Password`, `Ssl`, `ConnectTimeout`.
  * `IsReadOnly`: Safe Mode flag preventing write, update, and delete mutations.
  * `OwnerId`: Foreign key to the user owning this connection.
  * `BuildConnectionString()`: Domain method generating a safe, formatted `StackExchange.Redis` connection string.
* **`Users/User`**: User identity entity with `Email`, `PasswordHash`, `Role` (`UserRole` enum), `IsActive`, and `LastLoginAt`.
* **`Audit/AuditLog`**: Audit trail record capturing `Timestamp`, `UserId`, `UserEmail`, `IpAddress`, `ActionType`, `Resource`, `Details`, and `IsSuccess`.
* **`RedisMetrics/RedisMetrics`**: Performance telemetry aggregate populated using `[RedisMetric]` attributes: Ops/sec, Used Memory, Peak Memory, System/User CPU, Connected Clients, Total Keys, and Network I/O.
* **`Redis/RedisLoadTest`**: Models configuring and reporting stress tests: `LoadTestResult`, `LoadTestOptions`, latency percentiles (`p50`, `p95`, `p99`), RPS, and `ErrorCount`.
* **`Redis/RedisSlowlogItem`**: High-latency command inspection model: `Id`, `Timestamp`, `DurationMicroseconds`, `Arguments`, and `ClientIp`.
* **`Redis/SeedDataOptions`**: Configuration for demo data generation: `TotalKeys`, `KeyComplexity`, `PayloadComplexity`, `DataTypes`, and `TtlStrategy`.

### Domain Contracts & Interfaces:
* `IRedisConnectionRepository`: Abstraction for persisting and retrieving connection profiles.
* `IUserRepository`: Abstraction for user account queries and credential verification.
* `IAuditLogRepository`: Abstraction for appending and filtering audit events.
* `IRedisValueService`: Abstraction for high-level read and write operations across Redis types.
* `IRedisCliService`: Abstraction for executing raw CLI commands with database context.
* `IRedisDiagnosticsService`: Abstraction for server diagnostics (`SLOWLOG`, server configurations).
* `IRedisLoadTestService`: Abstraction for concurrent benchmark execution.
* `IRedisSeedDataService`: Abstraction for synthetic dataset generation.

---

## 2. `RedisGUI.Shared` (Shared Kernel & Functional Primitives)

Provides foundational primitives for robust, exception-safe code across the entire codebase.

* **`Result<T>` & `Error`**: Railway-Oriented Programming (ROP) pattern. Eliminates unhandled `null` references and replaces exceptions for business rule violations with structured, strongly-typed error responses.
* **In-Process CQRS & Mediator Pipeline**:
  * Interfaces: `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>`, `IMediator`, and `IPipelineBehavior`.
  * Enables lightweight, high-performance in-process messaging without third-party dependencies.

---

## 3. `RedisGUI.Application` (Business Logic & CQRS Layer)

Implements application business use cases using the CQRS pattern.

### Key Use-Case Handlers:
1. **`Connections/`**:
   * `CreateConnectionCommand`, `UpdateConnectionCommand`, `DeleteConnectionCommand`, `CheckConnectionCommand`.
   * `GetAllConnectionsQuery`, `GetConnectionByIdQuery`, `GetConnectionHealthQuery`, `ToggleReadOnlyCommand`.
   * `GetServerInfoQuery`, `GetServerConfigQuery`, `UpdateServerConfigCommand`.
2. **`Redis/` (Key & Data Structure Management)**:
   * `GetAllRedisKeysQuery`: Cursor-based `SCAN` iteration with pattern and database filters.
   * `GetRedisValueQuery`: Resolves and returns key data via the Strategy Pattern handler factory.
   * `SaveRedisValueCommand`: Persists String, Hash, List, Set, Sorted Set, or Stream data.
   * `DeleteRedisKeyCommand`, `DeleteMultipleKeysCommand`, `DeleteKeysByPatternCommand`.
   * `DuplicateRedisKeyCommand`: Deep clones a key and its contents under a new name.
   * `BulkUpdateTtlCommand`: Updates expiration for multiple keys simultaneously.
3. **`Diagnostics/` & `Benchmarking/`**:
   * `GetRedisSlowlogQuery`: Fetches recent slow operations with execution microsecond durations.
   * `RunRedisLoadTestCommand`: Executes concurrent stress tests and computes RPS / percentiles.
   * `SeedRedisSampleDataCommand`: Populates databases with synthetic data structures.
4. **`Users/` & `Audit/`**:
   * `SignInCommand`, `SignUpCommand`, `GetCurrentUserQuery`.
   * `GetAuditLogsQuery`: Paginated and filtered retrieval of audit records.

### Cross-Cutting Behaviors (`Behaviors/`):
* `ValidationBehavior<TRequest, TResponse>`: Automatically executes FluentValidation validators before handler execution.
* `LoggingBehavior<TRequest, TResponse>`: Structured execution duration and parameter logging.
* `AuditingBehavior<TRequest, TResponse>`: Automatically creates audit entries for mutating commands.

---

## 4. `RedisGUI.Infrastructure` (Adapters & External Services)

Implements domain abstractions and manages external network connections.

### Key Subsystems:
* **`Redis/Handlers/` (Strategy Pattern for Redis Types)**:
  * Handlers for each native Redis data type: `StringTypeHandler`, `HashTypeHandler`, `ListTypeHandler`, `SetTypeHandler`, `SortedSetTypeHandler`, and `StreamTypeHandler`.
  * Managed by `RedisTypeHandlerFactory`, with all handlers registered directly into the ASP.NET Core DI container.
* **`Redis/ConnectionPool/`**:
  * Thread-safe cache of `IConnectionMultiplexer` instances keyed by connection ID, preventing socket exhaustion.
* **`Redis/Telemetry/`**:
  * `RedisMetricsCollector`: Periodically collects telemetry via `server.InfoAsync()`, parsing numerical metrics using `CultureInfo.InvariantCulture` to prevent decimal formatting crashes.
* **`Persistence/`**:
  * Entity Framework Core `ApplicationDbContext` supporting both `InMemory` and `MySQL` / `MariaDB` providers.
  * Automatic schema migrations and seeding of default administrator accounts and demo connection profiles.
* **`Authentication/`**:
  * Cryptographic password hashing and JWT token issuance via `IJwtTokenGenerator`.

---

## 5. `RedisGUI.Server` (API Host & Presentation Layer)

The ASP.NET Core 10 Web host serving RESTful APIs, SignalR WebSockets, and static client assets.

### Features & Configuration:
* **Minimal APIs & Carter**: Modular endpoint definitions organized under `Endpoints/`.
* **SignalR Real-Time Hubs**:
  * `/hub/metrics`: Streams ops/sec, memory RSS, and CPU utilization every 5 seconds.
  * `/hub/keys`: Broadcasts key modifications and invalidation events.
* **Static File Hosting**: Serves the compiled Angular 19 SPA from `wwwroot/browser` with HTML5 fallback routing.
* **Security & Middleware**:
  * JWT Bearer token authentication and role authorization policies.
  * Global exception handling and RFC 7807 ProblemDetails middleware.
  * CORS headers for cross-origin local development.
* **OpenAPI / Scalar**: Interactive API documentation explorer available at `/scalar/v1`.

---

## 6. `RedisGUI.Client` (Angular 19 SPA Frontend)

Modern, high-performance single-page application built on Angular 19 with zoneless Signals.

### Architecture Highlights:
* **Signals & Signal Stores**: Reactive state management with zero RxJS memory leak risks (`AuthStore`, `RedisKeysStore`, `ThemeService`).
* **Dual Key Exploration**:
  * **Table View**: Virtual-scrolled table with live TTL countdowns, size formatting, and custom sort headers.
  * **Namespace Tree View**: Delimiter-based hierarchical folder browser (`:`, `/`, etc.) with direct subfolder key creation.
* **Data Structure Visual Editors**:
  * String: Plain text and Monaco JSON editor.
  * Hash: Interactive key-value attribute grid.
  * List, Set, Sorted Set: Element inspectors with push, pop, and score modifiers.
  * Stream: Append-only log inspector with Entry IDs and dual Visual/JSON modes.
* **Diagnostics & Utilities**:
  * Slide-out interactive Redis CLI terminal drawer with command history (`↑` / `↓`).
  * Command Palette (`Ctrl+K` / `Cmd+K`) for keyboard-driven navigation.
  * Slowlog inspector, synthetic load test runner, and sample data generator.
* **Design System**:
  * 6 custom color schemes, Dark/Light mode toggle, and Tailwind CSS utility classes.

---

## 7. Test Projects (Automated Test Suites)

Comprehensive automated testing with over 350 test cases across 4 test projects:

1. **`RedisGUI.Domain.Tests`**: Unit tests verifying entities, value objects, domain rules, and connection string builders.
2. **`RedisGUI.Shared.Tests`**: Unit tests verifying functional `Result<T>` and custom MediatR pipeline behaviors.
3. **`RedisGUI.Application.Tests`**: CQRS handler tests verifying commands, queries, and FluentValidation rules.
4. **`RedisGUI.Infrastructure.Tests`**: Unit and integration tests verifying Strategy type handlers, metric parsers, and connection pooling.
