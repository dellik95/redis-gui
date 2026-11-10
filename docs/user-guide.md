# RedisGUI — User Guide & Operations Manual

Welcome to **RedisGUI**, an intuitive, modern Redis administration dashboard designed for developers, DevOps engineers, and system architects.

---

## Table of Contents
1. [Getting Started & Authentication](#1-getting-started--authentication)
2. [Dashboard & Real-Time Monitoring](#2-dashboard--real-time-monitoring)
3. [Storage Manager & Key Management](#3-storage-manager--key-management)
   - [Table View](#table-view)
   - [Tree / Namespace View](#tree--namespace-view)
   - [Key Filtering & Pagination](#key-filtering--pagination)
4. [Editing Redis Values](#4-editing-redis-values)
   - [String & JSON Values](#string--json-values)
   - [Hash Key Visual Editor](#hash-key-visual-editor)
   - [Stream Visual & JSON Editors](#stream-visual--json-editors)
   - [Lists, Sets, and Sorted Sets](#lists-sets-and-sorted-sets)
5. [Interactive Redis CLI Terminal](#5-interactive-redis-cli-terminal)
6. [Benchmarking & Load Testing](#6-benchmarking--load-testing)
7. [Sample Data Generator & Seeder](#7-sample-data-generator--seeder)
8. [Slowlog & Diagnostics](#8-slowlog--diagnostics)
9. [Audit Logs & Compliance](#9-audit-logs--compliance)
10. [Appearance, Themes & Safe Mode](#10-appearance-themes--safe-mode)

---

## 1. Getting Started & Authentication

When launching RedisGUI, you are greeted with the secure authentication gateway.

![RedisGUI Login Gateway](screenshots/01_login.png)

### Default Credentials
For local development and initial Docker runs, the system seeds a default administrative account:
- **Email**: `admin@redisgui.local`
- **Password**: `AdminPassword123!`

> [!NOTE]
> You can create additional users with granular roles (`Admin`, `Standard`, `ReadOnly`) from the application settings.

---

## 2. Dashboard & Real-Time Monitoring

Upon signing in, the **Dashboard** presents immediate health telemetry and connection overviews for all configured Redis clusters and standalones.

![RedisGUI Dashboard](screenshots/02_dashboard.png)

### Key Metrics Displayed:
- **Ops / Sec**: Instantaneous operations throughput processed by Redis.
- **Memory Usage**: Real-time RSS memory allocated vs. peak memory.
- **CPU System / User**: Redis engine CPU consumption.
- **Active Connections**: Active client connections and health status.

---

## 3. Storage Manager & Key Management

The **Storage Manager** is your primary workspace for exploring and manipulating Redis keys across all available logical databases (`DB 0` through `DB 15`).

### Table View
The Table View presents keys in a structured, sortable, and filterable data grid, featuring live metrics pills in the header:

![Storage Manager Table View](screenshots/03_storage_table.png)

- **Live Metrics Bar**: High-visibility pills showing CPU usage, live memory footprint, network bandwidth in/out, and total keys count streamed via SignalR.
- **Action Bar**: Instant access to **Add Key**, **Seed Demo Data**, **Load Test**, **Slowlog**, and database switcher.

### Tree / Namespace View
Switch to **Tree View** by clicking the view toggler. This hierarchical tree automatically groups keys by their separator (e.g. `:` or `/`):

![Storage Manager Tree View](screenshots/04_storage_tree.png)

- Click any folder to expand or collapse namespaces (e.g., `user:*`, `demo:*`, `orders:*`).
- Select any leaf node to open the key editor instantly.

---

## 4. Editing Redis Values

RedisGUI includes dedicated visual and code editors for each Redis data type.

### Hash Key Visual Editor
Easily inspect and edit field-value pairs in Redis Hashes without manual serialized syntax:

![Hash Key Visual Editor](screenshots/07_hash_editor.png)

- Add new fields with one click.
- Inline edit field names and values.
- Delete individual fields or update TTL.

### Stream Visual & JSON Editors
Redis Streams are first-class citizens in RedisGUI:
- **Visual Event Stream**: Renders each stream entry with its auto-generated timestamp ID (`<timestamp>-<sequence>`) and all associated telemetry fields.
- **JSON View**: Switch the toggle at the top right to inspect the raw JSON document representation.

---

## 5. Interactive Redis CLI Terminal

Need direct console access? Click the **CLI Terminal** button in the bottom drawer to open the integrated interactive Redis CLI:

![Redis CLI Terminal Drawer](screenshots/10_redis_cli_drawer.png)

- Supports standard Redis commands: `INFO`, `PING`, `KEYS`, `GET`, `SET`, `HGETALL`, `XREAD`, etc.
- **Multi-Database Awareness**: Commands execute against the currently active database context.
- **Keyboard Navigation**: Press `Up` / `Down` arrow keys to cycle through past command history.
- Auto-scrolls to latest output and formats RESP multi-bulk responses clearly.

---

## 6. Benchmarking & Load Testing

Benchmark your Redis instance directly from the UI without external tools like `redis-benchmark`:

![Load Test Dialog](screenshots/08_load_test_dialog.png)

### Configurable Test Parameters:
- **Total Operations**: Number of commands to execute (e.g. 500 – 10,000).
- **Concurrency**: Number of parallel workers (1 to 50 concurrent threads).
- **Operation Distribution**: Choose between **Read Heavy (80% Get / 20% Set)**, **Write Heavy**, or **Balanced (50/50)**.
- **Live Performance Report**: Displays total duration, operations per second, latency percentiles (`p50`, `p95`, `p99`), and error counts.

---

## 7. Sample Data Generator & Seeder

Quickly populate realistic datasets for staging, performance testing, and demo environments:

![Sample Data Generator Dialog](screenshots/09_seed_data_dialog.png)

- **Key Count**: Generate up to 10,000 keys in seconds.
- **Key Complexity**: Choose between Simple (`prefix:id`), Hierarchical (`org:dept:entity:id`), or UUID-based keys.
- **Data Types**: Choose which types to include (String, Hash, List, Set, Sorted Set, Stream).
- **TTL Strategies**: Choose between Persistent keys, Expiring keys (60s – 3600s), or Mixed.

---

## 8. Slowlog & Diagnostics

Diagnose performance bottlenecks and queries exceeding your latency threshold:

![Slowlog Diagnostics Dialog](screenshots/11_slowlog_dialog.png)

- Displays execution timestamp, duration (in microseconds), client IP, and full command arguments.
- Identifies slow O(N) operations (e.g. unindexed `KEYS *` or large `SMEMBERS`).

---

## 9. Audit Logs & Compliance

All administrative actions, key mutations, and CLI commands are recorded in the immutable audit log:

![Audit Logs Viewer](screenshots/12_audit_logs.png)

- Search and filter by user email, action category, connection ID, or date range.
- View request payloads, client IP addresses, and HTTP status codes.

---

## 10. Appearance, Themes & Safe Mode

RedisGUI provides a tailored visual experience with 6 built-in themes and a toggleable Light/Dark mode:

![Light Theme Storage View](screenshots/13_light_theme_preview.png)

- **Available Themes**: Deep Violet, Emerald Green, Indigo Night, Crimson Gold, Cyan Amber, Charcoal Dark.
- **Safe Mode**: Toggle any connection into **Read-Only** mode to protect production environments from unintended modifications or deletions.
