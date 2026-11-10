# RedisGUI — Comprehensive Feature Catalog

RedisGUI is engineered as a modern, production-ready GUI client for Redis 7.x and 8.x, featuring rich data manipulation tools, observability dashboards, and diagnostic utilities.

---

## Feature Matrix

| Feature Module | Capabilities | Backend Implementation | Frontend Implementation |
| :--- | :--- | :--- | :--- |
| **Key & Data Management** | CRUD operations across String, Hash, List, Set, Sorted Set, Stream | Strategy pattern via `IRedisTypeHandlerFactory` & `IRedisTypeHandler` | `RedisTypeHandlerRegistry`, visual editors + Monaco JSON |
| **Namespace Tree Explorer** | Hierarchical key browsing with custom delimiters (`:`, `/`) | Redis `SCAN` cursor iteration with count estimation | Angular Tree component with lazy expanding nodes |
| **Real-time Telemetry** | Instant ops/sec, memory RSS, CPU usage, bandwidth, key counters | Background timer service + SignalR `RedisMetricsHub` | Reactive Signal component with live pulsing pills |
| **Interactive Redis CLI** | Full command prompt with multi-database support & history | `IRedisCliService`, command sanitizer, RESP executor | Draggable drawer terminal with history navigation (`↑`/`↓`) |
| **Benchmarking & Load Testing** | Synthetic load runner with configurable concurrency & read/write ratios | `IRedisLoadTestService`, parallel Task workers, Stopwatch | Load test modal dialog with latency percentiles |
| **Smart Data Seeding** | Generates realistic synthetic datasets (simple, complex, JSON) | `IRedisSeedDataService`, Strategy pattern for key naming & payloads | Seed modal with type selectors, count slider, TTL strategies |
| **Slowlog Diagnostics** | Inspects commands exceeding the execution threshold | `IRedisDiagnosticsService`, `SLOWLOG GET` parser | Sortable modal dialog with microsecond execution durations |
| **Safe Mode (Read-Only)** | Protects production databases from accidental writes or deletes | Connection metadata `IsReadOnly`, command pipeline filter | Safety banner in header, disables mutation actions |
| **Multi-Database Support** | Seamless switching across DB 0 through DB 15 | `IDatabase` parameter routing per command/query | Database dropdown selector in top navigation bar |
| **Audit Logging** | Complete audit trail for compliance and tracking | MediatR `AuditingBehavior`, EF Core persistence | Audit logs table with search, filters, and JSON payload inspector |
| **Theme & Appearance** | 6 color schemes, Dark & Light modes, responsive layout | User preference persistence | Angular Material CSS variables, Tailwind theme engine |

---

## Detailed Module Specifications

### 1. Key & Data Structure Strategy Handlers
The system does not rely on generic string dumps for complex Redis types:
- **String Handler**: Auto-detects whether the string is raw text, valid JSON, or formatted number. Supports strict TTL (non-expiring vs expiring).
- **Hash Handler**: Visual grid for editing key-value pairs with instant field addition, inline modification, and per-field deletion.
- **List Handler**: Supports push (`LPUSH`/`RPUSH`), pop (`LPOP`/`RPOP`), item reordering, and sub-range inspection.
- **Set Handler**: Unique member management with duplicate prevention.
- **Sorted Set Handler**: Score adjustment, rank-based sorting, and range queries.
- **Stream Handler**: Displays append-only log entries with millisecond timestamp IDs, field-value dictionaries, and real-time append capability.

### 2. Live Metrics Pipeline
- Utilizes `StackExchange.Redis` `server.InfoAsync()` to extract live metrics.
- Formats and parses metrics using `CultureInfo.InvariantCulture` to prevent culture-sensitive parsing exceptions on non-US host environments.
- Streams telemetry over WebSockets via SignalR `RedisMetricsHub` every 5 seconds.
- Provides immediate HTTP REST endpoint (`GET /connections/{id}/metrics`) for zero-delay initial load.

### 3. Load Testing Engine
- Runs concurrent workers using .NET `Task.WhenAll`.
- Simulates realistic workloads:
  - *Read-Heavy*: 80% `GET` operations, 20% `SET` operations.
  - *Write-Heavy*: 80% `SET` operations, 20% `GET` operations.
  - *Balanced*: 50% `GET`, 50% `SET`.
- Computes comprehensive statistics:
  - Total elapsed time
  - Requests per second (RPS)
  - `p50`, `p95`, `p99` latency in milliseconds
  - Failure / error rates

### 4. Interactive CLI Terminal
- Executes commands against the currently selected Redis database context.
- Sanitizes dangerous server commands (e.g. `FLUSHALL`, `SHUTDOWN`) when in Safe Mode or restricted user sessions.
- Formats RESP array responses into clean terminal output.
- Remembers last 50 executed commands in session history.
