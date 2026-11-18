# RedisGUI — Modern Redis Studio & Observability Platform

<p align="center">
  <img src="docs/screenshots/03_storage_table.png" alt="RedisGUI Storage Manager" width="850">
</p>

<p align="center">
  <strong>An enterprise-grade, high-performance Redis management studio and diagnostics platform built on .NET 11, ASP.NET Core Minimal APIs, and Angular 22 Signals.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-11.0-512BD4?logo=dotnet" alt=".NET 11">
  <img src="https://img.shields.io/badge/Angular-22.2.5-DD0031?logo=angular" alt="Angular 22">
  <img src="https://img.shields.io/badge/Redis-7.x%20%7C%208.x-DC382D?logo=redis" alt="Redis">
  <img src="https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker" alt="Docker">
  <img src="https://img.shields.io/badge/Architecture-Clean%20%2F%20CQRS-brightgreen" alt="Clean Architecture">
  <img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License">
</p>

---

## 🌟 Key Features

### 🗄️ Comprehensive Redis Data Structure Support
Full visual inspection and editing across all native Redis types using the **Strategy Pattern**:
- **Strings**: Raw text, JSON document syntax highlighting, and exact TTL control.
- **Hashes**: Interactive field-value editor with one-click additions and deletions.
- **Lists**: Push, pop, reorder, and element navigation.
- **Sets & Sorted Sets**: Set member management and score-based ranking.
- **Streams**: Append-only log inspector with entry IDs, field-value maps, and dual Visual/JSON modes.

### Messaging Inspector
- Subscribe to literal pub/sub channels or patterns and publish messages. Pub/sub messages are transient and only appear while monitoring.
- Inspect stream length, consumer-group count, last generated ID, and recent entries; new entries arrive over SignalR.
- Watch live charts for delivery rate and direct subscribers or stream length, with current consumer-group and pattern-subscription counts.
- Publishing respects user write access and connection Safe Mode.

### ⚡ Real-Time Live Telemetry & Notification Center
- Sub-second live metrics streamed over **SignalR WebSockets** (`/hub/metrics`): Ops/sec, Memory RSS, CPU usage, and network bandwidth.
- **Typed Background Jobs & Notification Center** (`/hub/jobs`): Queue bulk deletes/TTLs, JSON/CSV exports, imports, synthetic load tests, and sample data seeding asynchronously with live progress, download leases, access-revocation enforcement (`AccessRevoked`), and a global header notification popover.
- Live status pills in the Storage Manager header with pulsing indicators and **Cluster** mode badges.

### 📂 Dual Exploration Modes, Binary Keys & Cluster Safety
- **Virtualized Table View**: An Angular CDK (`*cdkVirtualFor`) virtual-scrolled grid with threshold auto-pagination, Redis glob search, type filters, `BIN` binary-key badges (`KeyBase64`), TTL countdowns, and sorting of loaded keys. Cursor-based scans load key metadata incrementally; scroll or click **Load more** to continue.
- **Namespace Tree View**: Hierarchical folder tree grouping keys by delimiter (`:`, `/`, `.`, etc.) with **direct subfolder key creation** via the folder `+` action.
- **Redis Cluster Awareness**: Automatically detects Standalone vs. Cluster topology (`IRedisTopologyRegistry`), locks DB 0 in Cluster mode, traverses all Cluster primaries with HMAC-signed continuation cursors that fail fast on mid-scan `CLUSTER FAILOVER`, and parses `CLUSTER NODES`.

The browser shows only keys discovered so far. Redis `SCAN` is not a snapshot: keys may change during a scan, and the count hint is not a strict page-size limit. Type counts, sorting, and tree contents apply to loaded results. The browser does not load values or report value sizes while scanning.

### 🛠️ Interactive Redis CLI Drawer
- Built-in slide-out terminal drawer.
- Multi-database awareness (`DB 0` – `DB 15`).
- Keyboard history navigation (`↑` / `↓`).
- Output formatting for multi-bulk responses and arrays.

### 🚀 Benchmarking & Load Testing
- Run synthetic concurrent benchmarks directly from your browser.
- Configure workers, total requests, and Read/Write ratios (Read Heavy, Write Heavy, Balanced).
- Instant report of RPS, elapsed time, and `p50` / `p95` / `p99` latency percentiles.

### 🧪 Realistic Sample Data Seeder
- Populate test environments with up to 10,000 keys in seconds.
- Customize key complexity (Simple, Hierarchical, UUID), payload styles, and TTL strategies.

### 🔍 Diagnostics & Slowlog
- Real-time Slowlog viewer displaying command duration in microseconds, client IP, and query arguments.

### 🛡️ Enterprise Security & Safe Mode
- **Safe Mode (Read-Only)**: Protect production environments from accidental data mutations.
- **Audit Logging**: Full audit trail of all administrative actions and mutations.
- **JWT Authentication & RBAC**: Role-based access control (`Admin`, `Standard`, `ReadOnly`).

### 🎨 Theme Customization
- 6 vibrant color schemes (Deep Violet, Emerald Green, Indigo Night, Crimson Gold, Cyan Amber, Charcoal).
- One-click Dark and Light theme toggles.

---

## 📸 Screenshots Gallery

| Live Pub/Sub Metrics | Live Stream Metrics |
| :---: | :---: |
| ![Live pub/sub metrics](docs/screenshots/17_pubsub_metrics.jpg) | ![Live stream metrics](docs/screenshots/18_stream_metrics.jpg) |

| Dashboard & Metrics | Storage Manager (Table View) |
| :---: | :---: |
| ![Dashboard](docs/screenshots/02_dashboard.png) | ![Table View](docs/screenshots/03_storage_table.png) |

| Namespace Tree View | Hash Key Visual Editor |
| :---: | :---: |
| ![Tree View](docs/screenshots/04_storage_tree.png) | ![Hash Editor](docs/screenshots/07_hash_editor.png) |

| Stream Visual Editor | Stream JSON Document View |
| :---: | :---: |
| ![Stream Visual](docs/screenshots/05_stream_visual_editor.png) | ![Stream JSON](docs/screenshots/06_stream_json_editor.png) |

| String / JSON Value Editor | Add New Key Modal |
| :---: | :---: |
| ![String Editor](docs/screenshots/07b_string_editor.png) | ![Add Key](docs/screenshots/15_add_key_dialog.png) |

| Synthetic Load Testing | Interactive CLI Terminal |
| :---: | :---: |
| ![Load Test Dialog](docs/screenshots/08_load_test_dialog.png) | ![CLI Drawer](docs/screenshots/10_redis_cli_drawer.png) |

| Sample Data Seeder | Slowlog Diagnostics |
| :---: | :---: |
| ![Seed Data Dialog](docs/screenshots/09_seed_data_dialog.png) | ![Slowlog](docs/screenshots/11_slowlog_dialog.png) |

| Command Palette (Ctrl+K) | Add Connection Dialog |
| :---: | :---: |
| ![Command Palette](docs/screenshots/16_command_palette.png) | ![Add Connection](docs/screenshots/14_add_connection_dialog.png) |

| Audit Logs & Compliance | Light Theme Preview |
| :---: | :---: |
| ![Audit Logs](docs/screenshots/12_audit_logs.png) | ![Light Theme](docs/screenshots/13_light_theme_preview.png) |

---

## 🏗️ Architecture & Tech Stack

RedisGUI follows **Clean Architecture** and **CQRS**:

```mermaid
flowchart LR
    Client["Angular 22 (Signals & CDK Virtual Scroll)"] <-->|"REST (ProblemDetails) & 4 SignalR Hubs"| Server["ASP.NET Core 11 Minimal APIs"]
    Server --> Mediator["Lock-Free In-Process Mediator (CAS Pipelines)"]
    Mediator --> App["Application Core & Typed Job Handlers"]
    App --> Domain["Domain (Entities, Value Objects & Topology Contracts)"]
    App --> Infra["Infrastructure (Lock-Free Connection Pool, EF Core + FusionCache, Channels)"]
    Infra <--> Redis[("Redis 7/8 Standalone & Cluster")]
    Infra <--> MySQL[("MySQL 8.4 / InMemory")]
```

- **Backend**: .NET 11, ASP.NET Core 11 Minimal APIs, lock-free in-process mediator, FluentValidation, StackExchange.Redis, Entity Framework Core + parameter-aware FusionCache query caching, `Channel<T>` background job workers, OpenTelemetry, RFC 9457 Problem Details.
- **Frontend**: Angular 22 (Standalone Components, Signals, Signal Forms, `@ngrx/signals` stores, CDK Virtual Scroll), Angular Material 22, Tailwind CSS, Monaco Editor.
- **Architecture Highlights**:
  - **Lock-Free Coordination**: Lock-free `PipelineRegistry` (CAS array swaps), `RedisConnectionManager` state/ref-count machine, `KeyChangeSubscriptionManager`, and `BackgroundJobSubscriptionRegistry`.
  - **Binary-Safe & Cluster-Aware**: Base64 key identity (`KeyBase64`, `IsBinary`) over query-safe routes plus `IRedisTopologyRegistry` Cluster detection & failover-safe cursor tokens.
  - **No magic values**: Strongly-typed enums (`RedisDataType`, `UserRole`, `RedisServerMode`) synced between client and server.
  - **Strategy & Factory Patterns**: Data type, naming, seed, and background job (`IBackgroundJobHandler`) strategies registered in DI and discovered dynamically.
  - **Invariant Culture Parsers**: Resilient reflection metrics collectors supporting any OS locale.

---

## 🚀 Quickstart

### Option 1: Run with Docker Compose (Recommended)

Clone the repository and run:

```bash
docker compose up -d --build
```

Navigate to:
- **Web Studio**: [http://localhost:5000](http://localhost:5000)
- **Redis Server**: `localhost:6379`

#### Default Credentials:
- **Email**: `admin@redisgui.local`
- **Password**: `AdminPassword123!`

---

### Option 2: Local Development

#### Prerequisites
- [.NET 11 SDK](https://dotnet.microsoft.com/)
- [Node.js & npm](https://nodejs.org/) (Node 22.22.3+, 24.15+, or 26.x)
- Running Redis instance on `127.0.0.1:6379` (e.g. via Docker or WSL)

#### Start Angular and ASP.NET Core together

Run these commands from the repository root.

```bash
npm ci --prefix src/RedisGUI.Client
dotnet run --project src/RedisGUI.Server --launch-profile https
```

Open [the backend root](https://localhost:61034/) to activate the ASP.NET Core SPA proxy. It starts the Angular dev server and redirects to [the client](https://localhost:4200/). Angular forwards API and SignalR requests to the backend.

In Visual Studio, open `RedisGUI.slnx`, select the shared **RedisGUI (Angular + API)** launch profile, and press **F5**. The profile starts both projects and opens Angular in the browser. If your browser prompts about the local development certificate, use `dotnet dev-certs https --trust`.

---

## 🧪 Running Tests

The test suite covers domain logic, CQRS handlers, type handlers, and infrastructure:

```bash
dotnet test RedisGUI.slnx
```
*(460+ automated .NET 11 & Angular 22 unit/integration tests)*

---

## 📚 Documentation & Technical References

- 🏛️ [System Architecture & Design Details](docs/architecture.md) — Clean Architecture layers, CQRS, Strategy/Factory patterns, Connection Pooling.
- 📦 [Projects & Solution Structure](docs/projects-structure.md) — Comprehensive breakdown of every project in `RedisGUI.slnx`, classes, and tests.
- ⚙️ [Configuration & Settings Guide](docs/configuration.md) — Full reference for `appsettings.json`, JWT, Redis timeouts, Docker env vars.
- 📊 [Architectural Diagrams & Flowcharts](docs/diagrams.md) — Complete Mermaid diagrams (Architecture, ER, CQRS pipeline, SignalR, CLI, Load testing).
- 📋 [Comprehensive Feature Catalog](docs/features.md) — Specifications for all 10+ core feature modules and API contracts.
- 📖 [User Guide & Operations Manual](docs/user-guide.md) — Step-by-step user manual with screenshots for every feature.
- 🐳 [Docker Deployment & Configuration](docs/docker.md) — Production multi-stage Docker builds and docker-compose orchestration.

---

## 📄 License

This project is licensed under the MIT License.
