# RedisGUI — User Guide & Getting Started Manual

This comprehensive guide walks you through getting started with **RedisGUI**, configuring prerequisites, running locally or with Docker, and leveraging all features of the platform with screenshot walkthroughs.

---

## Table of Contents

1. [Getting Started & Installation](#1-getting-started--installation)
   - [Prerequisites](#prerequisites)
   - [Running Locally with .NET & Angular](#running-locally-with-net--angular)
   - [Running with Docker Compose](#running-with-docker-compose)
   - [Default Administrator Credentials](#default-administrator-credentials)
2. [Authentication & Secure Sign-In](#2-authentication--secure-sign-in)
3. [Dashboard & Connection Management](#3-dashboard--connection-management)
   - [Server Telemetry Overview](#server-telemetry-overview)
   - [Creating & Editing Redis Connections](#creating--editing-redis-connections)
   - [Safe Mode (Read-Only Protection)](#safe-mode-read-only-protection)
4. [Storage Manager & Key Exploration](#4-storage-manager--key-exploration)
   - [Table View & Real-Time Telemetry](#table-view--real-time-telemetry)
   - [Namespace Tree View & Direct Subfolder Key Creation](#namespace-tree-view--direct-subfolder-key-creation)
   - [Column Sorting & Filters](#column-sorting--filters)
   - [Command Palette (`Ctrl + K`)](#command-palette-ctrl--k)
   - [Creating Keys & Namespaces (Add Key Dialog)](#creating-keys--namespaces-add-key-dialog)
5. [Visual Data Structure Editors](#5-visual-data-structure-editors)
   - [String & Monaco JSON Editor](#string--monaco-json-editor)
   - [Hash Visual Attribute Editor](#hash-visual-attribute-editor)
   - [Stream Visual Timeline & JSON Inspector](#stream-visual-timeline--json-inspector)
   - [Lists, Sets, and Sorted Sets](#lists-sets-and-sorted-sets)
6. [Interactive Redis CLI Terminal Drawer](#6-interactive-redis-cli-terminal-drawer)
7. [Synthetic Benchmarking & Load Testing](#7-synthetic-benchmarking--load-testing)
8. [Realistic Sample Data Seeder](#8-realistic-sample-data-seeder)
9. [Diagnostics & Slowlog Analyzer](#9-diagnostics--slowlog-analyzer)
10. [Audit Logging & Security Trail](#10-audit-logging--security-trail)
11. [Design System & Theme Customization](#11-design-system--theme-customization)

---

## 1. Getting Started & Installation

### Prerequisites
Before running RedisGUI locally, ensure your machine has the following tools installed:
* **.NET 10 SDK** (or later) — [Download .NET](https://dotnet.microsoft.com/download)
* **Node.js 20+ & npm 10+** — [Download Node.js](https://nodejs.org)
* **Docker & Docker Compose** (optional, recommended for quick start) — [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* **Redis Server 7.x or 8.x** running locally on port 6379 or accessible over the network.

---

### Running Locally with .NET & Angular

#### Step 1: Start Redis
If you have Redis installed locally or via Docker:
```bash
docker run -d --name redis-local -p 6379:6379 redis:7-alpine
```

#### Step 2: Build the Frontend Client
```bash
cd src/RedisGUI.Client
npm install
npm run build
cd ../..
```
The compiled Angular assets will be placed into `src/RedisGUI.Client/dist/RedisGUI.Client/browser`.

#### Step 3: Run the Backend Host
The ASP.NET Core Web server automatically serves the compiled Angular client as static files:
```bash
dotnet run --project src/RedisGUI.Server/RedisGUI.Server.csproj -c Debug
```
The server will start listening on:
* **Web UI & API**: `http://localhost:5000`
* **Scalar / OpenAPI Explorer**: `http://localhost:5000/scalar/v1`

#### Optional: Frontend Live Development
If developing Angular components with instant hot reload:
```bash
cd src/RedisGUI.Client
npm start
```
Navigate to `http://localhost:4200` (API proxy forwards requests to `http://localhost:5000`).

---

### Running with Docker Compose

To spin up RedisGUI and a persistent Redis 7 server with a single command:

```bash
docker compose up -d --build
```

This launches two interconnected containers:
1. `redisgui-redis`: Redis 7 server on port `6379`.
2. `redisgui-app`: ASP.NET Core 10 server + Angular client on port `5000`.

To stop the containers:
```bash
docker compose down
```

---

### Default Administrator Credentials

When using the default `InMemory` database provider, an administrator account and default connection are automatically seeded:

* **Email**: `admin@redisgui.local`
* **Password**: `AdminPassword123!`
* **Pre-configured Connection**: `Local Redis` (`127.0.0.1:6379`)

---

## 2. Authentication & Secure Sign-In

Access to databases is secured through HMAC-SHA256 JWT bearer tokens with automatic session refresh.

![RedisGUI Sign In Screen](screenshots/01_login.png)

1. Navigate to `http://localhost:5000` in your web browser.
2. Enter the administrator credentials:
   - **Email**: `admin@redisgui.local`
   - **Password**: `AdminPassword123!`
3. Click **Sign In**. The JWT token is securely stored in client memory/cookies, and you are immediately redirected to the Connection Dashboard.

---

## 3. Dashboard & Connection Management

### Server Telemetry Overview
The Dashboard gives you an executive summary of all registered Redis servers, clusters, and standalone nodes.

![RedisGUI Main Dashboard](screenshots/02_dashboard.png)

* **Connection Cards**: Display hostname, port, database count, response ping status (Online / Offline), and active Safe Mode indicator.
* **Quick Action Controls**: Instant buttons to open the Storage Manager, edit connection parameters, trigger health diagnostics, or toggle **Safe Mode**.

### Creating & Editing Redis Connections
Click **Add Connection** in the upper right header to register a new Redis instance:

![Add New Connection Dialog](screenshots/14_add_connection_dialog.png)

* **Connection Name**: Human-friendly label (e.g. `Production Cluster`, `Staging Cache`).
* **Host & Port**: Hostname or IP address (e.g. `127.0.0.1` or `redis.internal.net`) and port (default: `6379`).
* **Password**: Optional AUTH password (encrypted before persistence).
* **SSL / TLS**: Toggles TLS encryption for secure cloud connections (e.g. AWS ElastiCache, Azure Redis).
* **Safe Mode (Read-Only)**: When enabled, prevents any write, update, rename, flush, or delete commands.
* **Test Connection**: Validates network connectivity and latency before saving.

---

## 4. Storage Manager & Key Exploration

The Storage Manager is the core workspace for querying, creating, and modifying Redis keys.

### Table View & Real-Time Telemetry

![Storage Manager Table View](screenshots/03_storage_table.png)

* **Real-Time Header Telemetry**:
  * **CPU**: Percentage of host CPU utilized by the Redis server engine.
  * **Memory**: Resident Set Size (RSS) memory consumed by Redis, formatted dynamically (e.g., `42.5 MB`).
  * **Network**: Real-time network throughput (Kb/s in and out).
  * **Keys**: Total number of keys in the currently active database.
* **Database Selector**: Switch instantly between logical databases (`DB 0` through `DB 15`) with real-time key counts per database.
* **Data Type Chips**: Filter by key type (`All`, `String`, `Hash`, `List`, `Set`, `ZSet`, `Stream`) with badge counts.
* **Pattern Search**: Search keys using glob patterns (e.g. `user:*`, `session:2026:*`).

### Namespace Tree View & Full Management Controls

Switch from flat Table view to hierarchical **Tree View** using the toggle in the toolbar:

![Namespace Tree View](screenshots/04_storage_tree.png)

* **Tree Toolbar & Global Controls**:
  * **Summary Counter**: Real-time summary showing total keys count and number of active namespaces (e.g., `54 keys in 12 namespaces`).
  * **Expand All** (`unfold_more`): Expands all nested namespace folders in the tree with one click.
  * **Collapse All** (`unfold_less`): Collapses all folders back to the root level.
* **Namespace Folder Controls (Group Nodes)**:
  * **Add Key in Folder (`+`)**: Opens the Add Key modal pre-populated with the folder prefix (e.g., `users:profiles:`).
  * **Copy Folder Path (`content_copy`)**: Copies the complete namespace prefix to the clipboard.
  * **Delete Entire Folder (`delete_sweep`)**: Displays a confirmation modal showing the folder path and number of affected keys. Upon confirmation, all keys under this namespace (e.g., `users:profiles:*`) are deleted in a single batch operation, and both the tree and database statistics update immediately.
* **Key Row Controls (Leaf Nodes)**:
  * Full row management toolbar visible on hover and selection, providing complete parity with the Table View:
    * **View / Edit Key** (`edit_note`): Opens the specialized type editor panel.
    * **Copy Key Name** (`content_copy`): Copies the full key name to the clipboard.
    * **Rename Key** (`drive_file_rename_outline`): Renames the key with instant tree updates.
    * **Duplicate Key** (`file_copy`): Creates a clone of the key with a custom target name.
    * **Delete Key** (`delete_outline`): Prompts confirmation and removes the individual key.

### Column Sorting & Filters

* Click any sortable column header (**Key**, **Type**, **TTL**, **Size**) to toggle ascending and descending order.
* The sort icon is aligned and centered with the column title for clear visibility.
* TTL column includes real-time live countdown badges with color-coded urgency (red for `< 60s`, green for persistent).

### Command Palette (`Ctrl + K`)

Press `Ctrl + K` (or `Cmd + K` on macOS) anywhere in the application to summon the Command Palette:

![Command Palette](screenshots/16_command_palette.png)

* Search and jump directly to actions: Create Key, Switch Database, Run Load Test, Open Slowlog, Export JSON/CSV, or Launch CLI.

### Creating Keys & Namespaces (Add Key Dialog)

Click **Add Key** in the toolbar or use the `+` folder button in the tree:

![Add New Key Dialog](screenshots/15_add_key_dialog.png)

* **Key Name**: Name of the key (supports namespace delimiters like `cache:session:id`).
* **Namespace Badge**: Displays the parent subfolder badge when initiated from a tree group node.
* **Type**: Strongly-typed selector (`String`, `Hash`, `List`, `Set`, `SortedSet`, `Stream`).
* **TTL (Seconds)**: Set exact expiration time or leave blank / `-1` for persistent keys.

---

## 5. Visual Data Structure Editors

RedisGUI provides custom visual editors tailored to each Redis data type using the **Strategy Pattern**.

### String & Monaco JSON Editor

![String Value Editor](screenshots/07b_string_editor.png)

* Supports plain text, numbers, and structured JSON documents.
* Automatic JSON syntax highlighting, format validation, and beautification.
* Instant TTL modification and key deletion actions.

### Hash Visual Attribute Editor

![Hash Field-Value Editor](screenshots/07_hash_editor.png)

* Tabular representation of `Field` and `Value` pairs.
* Inline editing with instant row addition and deletion.
* Real-time search filter for large hashes containing hundreds of fields.

### Stream Visual Timeline & JSON Inspector

````carousel
![Stream Visual Timeline View](screenshots/05_stream_visual_editor.png)
<!-- slide -->
![Stream JSON Representation View](screenshots/06_stream_json_editor.png)
````

* **Visual View**: Displays stream entries in chronological order with exact millisecond IDs (`1794300000000-0`) and payload attribute badges.
* **JSON View**: One-click toggle to view and copy the complete stream in raw JSON format.

### Lists, Sets, and Sorted Sets

* **List Editor**: Visual list with push (`LPUSH`/`RPUSH`), pop (`LPOP`/`RPOP`), and drag-and-drop ordering.
* **Set Editor**: Unique member manager with duplicate detection and member removal.
* **Sorted Set Editor**: Rank and score table with score modification and range inspection.

---

## 6. Interactive Redis CLI Terminal Drawer

Click the **Redis CLI** badge in the bottom toolbar to slide out the full-featured interactive terminal:

![Interactive Redis CLI Drawer](screenshots/10_redis_cli_drawer.png)

* **Integrated Terminal Drawer**: Accessible on any page without navigating away.
* **History Navigation**: Use `↑` (Up) and `↓` (Down) arrow keys to recall previous commands.
* **Database Context**: Commands execute against the currently selected database (`DB 0` – `DB 15`).
* **RESP3 Output Formatting**: Multi-bulk arrays, server info tables, and error diagnostics are rendered with monospace formatting.
* **Safe Mode Enforcement**: Destructive commands (e.g. `FLUSHALL`, `SHUTDOWN`) are automatically rejected if Safe Mode is active.

---

## 7. Synthetic Benchmarking & Load Testing

Measure Redis server throughput, latency, and concurrency under realistic conditions:

![Synthetic Load Test Dialog](screenshots/08_load_test_dialog.png)

* **Total Operations**: Configurable benchmark volume (from 50 to 50,000 requests).
* **Concurrency Workers**: Number of parallel asynchronous worker tasks (1 to 50 threads).
* **Workload Distribution**:
  * *Read Heavy*: 80% GET, 20% SET.
  * *Write Heavy*: 80% SET, 20% GET.
  * *Balanced*: 50% GET, 50% SET.
* **Comprehensive Metrics**: Displays total elapsed time, Requests Per Second (RPS), and latency percentiles (`p50`, `p95`, `p99`).
* **Automatic Cleanup**: Benchmark keys (`bench:*`) are automatically removed upon completion.

---

## 8. Realistic Sample Data Seeder

Populate empty or test databases with synthetic data structures in seconds:

![Sample Data Seeder Dialog](screenshots/09_seed_data_dialog.png)

* **Key Count**: Generate from 10 up to 10,000 keys with a single click.
* **Naming Schemes**:
  * *Simple*: `demo:string:1`
  * *Hierarchical*: `prod:europe:auth:session:1`
  * *UUID*: `orders:hash:3f4a-9b1c-...`
* **Data Types**: Choose any combination of Strings, Hashes, Lists, Sets, Sorted Sets, and Streams.
* **TTL Strategy**: Persistent keys, randomized expirations, or mixed lifetimes.

---

## 9. Diagnostics & Slowlog Analyzer

Identify high-latency queries that block the Redis single-threaded event loop:

![Slowlog Diagnostics](screenshots/11_slowlog_dialog.png)

* Lists timestamp, client IP, raw command arguments, and exact execution duration in microseconds (`µs`).
* Pinpoints slow non-indexed `KEYS *` queries, large sorted set intersections, or bulky JSON serializations.

---

## 10. Audit Logging & Security Trail

Maintain regulatory compliance and operational visibility across all user activities:

![Audit Log Viewer](screenshots/12_audit_logs.png)

* **Comprehensive Audit Trail**: Captures user identity, IP address, timestamp, action type (`CreateKey`, `DeleteKey`, `ExecuteCli`, `LoadTest`), and target database.
* **JSON Payload Inspector**: Review the exact arguments and server responses for any audit entry.

---

## 11. Design System & Theme Customization

RedisGUI features a design system supporting dark and light themes:

![Light Theme Preview](screenshots/13_light_theme_preview.png)

* **Color Palettes**: Slate Dark, Obsidian Purple, Midnight Emerald, Nordic Cyan, and Clean Light.
* **Theme Toggle**: Instantly switch between dark and light modes via the sun/moon icon in the navigation bar.
