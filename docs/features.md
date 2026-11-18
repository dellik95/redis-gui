# RedisGUI — Comprehensive Feature Matrix & Capabilities

This document catalogs the core engineering and user-facing capabilities of **RedisGUI** (built on **.NET 11**, **ASP.NET Core 11 Minimal APIs**, and **Angular 22 Signals**).

---

## Core Feature Matrix

| Feature Module | Capabilities | Backend Implementation | Frontend Implementation |
| :--- | :--- | :--- | :--- |
| **Virtualized Key Browser** | Smooth browsing of large loaded keysets with threshold auto-pagination and `BIN` indicators for binary keys | Cursor-based `RedisKeyService` + `RedisKeyScan` (`KeyBase64`, `IsBinary`) | Angular CDK `<cdk-virtual-scroll-viewport>` (`*cdkVirtualFor`) + `RedisKeysStore` |
| **Redis Cluster Support** | Automatic Cluster discovery, multi-primary `SCAN`, failover-safe HMAC cursor tokens, DB 0 enforcement, `CLUSTER NODES` info | `IRedisTopologyRegistry`, `RedisDiagnosticsService`, Cluster DB 0 validation | `Cluster` topology pills in Dashboard & Storage Manager + locked DB 0 picker |
| **Namespace Tree Explorer** | Hierarchical grouping of keys by customizable delimiters (`:`, `/`, `.`) with folder counts & `+` quick creation | Incremental cursor `SCAN` + server-side glob filtering | `TreeViewListComponent` with persistent expansion & direct path seeding |
| **Binary-Safe Key CRUD & Editors** | Full create, read, update, rename, duplicate, TTL editing & delete across UTF-8 and raw binary Redis keys (`KeyBase64`) | `IRedisTypeHandler` / `IRedisTypeHandlerFactory` + query-safe `storage/{id}/key` routes | Strategy-based visual drawers (`String`/Monaco JSON, `Hash`, `List`, `Set`, `SortedSet`, `Stream`) |
| **Typed Background Jobs & Notification Center** | Async queuing, real-time progress, cancellation, download leases & live `AccessRevoked` revocation notifications | `BackgroundJobManager` (`Channel<QueuedJob>`), 5 `IBackgroundJobHandler`s, `BackgroundJobsHub` (`/hub/jobs`) | `BackgroundJobsStore`, `BackgroundJobsService`, header `JobNotificationCenterComponent` |
| **Messaging Inspector (Pub/Sub & Streams)** | Watch pattern/literal Pub/Sub channels, publish messages, monitor Streams (`XINFO`/`XREAD`), live 1Hz rate & subscriber charts | `MessagingHub` (`/hub/messaging`), `RedisMessagingService`, `RedisMessagingMetricsCollector` | `MessagingInspectorComponent` with live message buffer (200 items) & chart history |
| **Real-Time Health Telemetry** | Instant ops/sec, Memory RSS/Peak, CPU usage, bandwidth, uptime, connected clients, and connection health matrix | `RedisMetricsBackgroundJob`, `ConnectionHealthMonitor`, `NotificationService` (`/hub/metrics`) | `DashboardMetricsComponent` & `StorageResourcesInfoComponent` with pulsing Signal pills |
| **Data Transfer (Import & Export)** | Streamed JSON snapshots & CSV exports preserving data types, binary payloads/keys & TTLs via background jobs | `RedisSnapshotService`, `ExportRedisDataJobHandler`, `ImportRedisDataJobHandler`, download leases | `ExportDialogComponent` & `ImportDialogComponent` backed by `BackgroundJobsStore` |
| **Synthetic Load Benchmark & Data Seeder** | Concurrent GET/SET/Mixed workers with latency percentiles (`p50`/`p95`/`p99`) + up to 10k realistic keys generator | `RedisLoadTestService` (`LoadTestOperations`), `RedisSeedDataService` (Strategy/Factory) | `LoadTestDialogComponent` & `SeedDataDialogComponent` |
| **Interactive Redis CLI Drawer** | DB-scoped ad-hoc command terminal with 1-click diagnostic presets and RESP3 array/bulk formatting | `ExecuteCliCommand` + `RedisRespFormatter` + Safe Mode command blocking | `RedisCliDrawerComponent` docked bottom panel |
| **Governance, RBAC, Safe Mode & Audit Logs** | JWT auth (`Admin`, `Standard`, `ReadOnly`), per-user connection ownership, Read-Only Safe Mode toggle, compliance trail | `IConnectionAccessService`, `AuditingBehavior`, RFC 9457 `ProblemDetails`, parameter-aware SQL cache | `AuthStore` (`logout()` clears all user stores), `AuditLogsComponent`, `EditConnectionComponent` |
| **Reload Persistence & Design System** | Restores active Storage Manager workspace across full browser reloads; 6 color schemes + Dark/Light toggle; `Ctrl+K` palette | Invariant REST APIs + SPA `/index.html` fallback (preserving `/health` probes) | `SideNavStore`, `CommandPaletteComponent`, Material 22 + Tailwind theme system |

---

## Architectural Feature Highlights

### 1. Virtualized Storage & Binary Key Fidelity
- **Zero-LAG Rendering**: `StorageValuesListComponent` renders visible rows inside `<cdk-virtual-scroll-viewport itemSize="48">` (`*cdkVirtualFor`) and automatically fetches the next `SCAN` page as users scroll toward the bottom of loaded keys (`Load more` button remains available for manual control).
- **Lossless Binary Identity**: Redis treats keys as binary-safe byte sequences. Whenever a scanned key contains non-UTF-8 sequences or control bytes, the backend sets `IsBinary = true` and populates `KeyBase64`. Query-based key endpoints (`api/v1/storage/{connectionId:guid}/key?key=&keyBase64=`) prevent URL path-encoding truncation or ambiguity across every read, edit, rename, duplicate, and delete operation.

### 2. Extensible Typed Background Jobs
Long-running tasks share a unified execution, authorization, and notification pipeline:
- **Supported Kinds (`GET /api/v1/jobs/kinds`)**:
  - `bulk-keys` — Bulk delete by list or pattern, and bulk TTL assignment.
  - `load-test` — Multi-client benchmark with isolated `__bench_*: ` keys and automatic cleanup.
  - `seed-data` — Configurable realistic/hierarchical/hashtag sample data generation.
  - `redis-export` — JSON/CSV snapshot streaming protected by download leases.
  - `redis-import` — JSON/CSV restoration preserving TTLs and binary keys across target DBs.
- **Resilient Notifications**: `BackgroundJobsService` opens a shared authenticated `/hub/jobs` SignalR connection with automatic initial retry on startup races and surfaces live completion/failure/cancellation inside the header bell popover.

### 3. Cluster Topology Safety
- `RedisKeyScan` hashes connected Cluster primary node identities into continuation cursors so a mid-scan `CLUSTER FAILOVER` returns an explicit cursor invalidation error instead of silently scanning incomplete slots.
- Whenever a connection is detected as `Cluster`, UI database selectors display **DB 0 · Cluster** and lock database switching, while `Seed`, `LoadTest`, `Export`, `Import`, and `MessagingHub` reject non-zero database targets server-side.
