# RedisGUI — Architectural Diagrams, Sequences & Flowcharts

This document provides a comprehensive collection of Mermaid diagrams detailing the architectural design, entity relationships, CQRS execution pipelines, and workflows within **RedisGUI**.

---

## 1. Overall System Architecture (Clean Architecture Flowchart)

```mermaid
flowchart TD
    subgraph UI ["Presentation Layer (Client)"]
        Browser["User Web Browser"]
        AngularApp["Angular 19 SPA (Standalone & Signals)"]
        SignalStore["Signal Stores (Auth, RedisKeys, Theme)"]
        Browser <-->|DOM Events / Render| AngularApp
        AngularApp --> SignalStore
    end

    subgraph Host ["API Hosting Layer (Server)"]
        ServerAPI["ASP.NET Core 10 Minimal APIs"]
        SignalRHubs["SignalR Hubs (/hub/metrics, /hub/keys)"]
        Middlewares["Middlewares (Auth, ErrorHandling, RateLimiting)"]
    end

    subgraph AppLayer ["Business Logic Layer (Application)"]
        MediatR["MediatR Pipeline"]
        Behaviors["Pipeline Behaviors (Audit, Validation, Logging)"]
        Commands["Commands / Queries (CQRS)"]
        Validators["FluentValidation Rules"]
    end

    subgraph DomainLayer ["Enterprise Domain Layer (Domain)"]
        Entities["Entities (RedisConnection, User, AuditLog)"]
        ValueObjects["Value Objects (ConnectionHost, KeyPath)"]
        DomainContracts["Abstractions (IRedisValueService, IConnectionPool)"]
    end

    subgraph InfraLayer ["Infrastructure & Adapters Layer"]
        ConnPool["IConnectionPool (ConnectionMultiplexer Cache)"]
        TypeFactory["IRedisTypeHandlerFactory (Strategy Pattern)"]
        BgServices["Background Jobs (RedisMetricsJob, HealthMonitor)"]
        EFCore["Entity Framework Core (InMemory / MySQL)"]
    end

    subgraph External ["External Infrastructure"]
        RedisServer[("Redis Server 7.x / 8.x")]
        RelationalDB[("MySQL / MariaDB")]
    end

    AngularApp <-->|RESTful JSON| Middlewares
    AngularApp <-->|WebSockets| SignalRHubs
    Middlewares --> ServerAPI
    ServerAPI --> MediatR
    MediatR --> Behaviors
    Behaviors --> Validators
    Behaviors --> Commands
    Commands --> DomainContracts
    DomainContracts --> TypeFactory
    DomainContracts --> ConnPool
    TypeFactory --> ConnPool
    ConnPool <-->|RESP3 Protocol| RedisServer
    BgServices <-->|INFO Polling| RedisServer
    BgServices --> SignalRHubs
    EFCore <--> RelationalDB
    Commands --> EFCore
```

---

## 2. Entity-Relationship Diagram (ERD)

```mermaid
erDiagram
    User ||--o{ RedisConnection : "owns / manages"
    User ||--o{ AuditLog : "initiates"
    RedisConnection ||--o{ AuditLog : "target of action"

    User {
        guid Id PK
        string Email UK
        string PasswordHash
        int Role "Enum (Admin=0, Standard=1, ReadOnly=2)"
        boolean IsActive
        datetime CreatedAtUtc
        datetime LastLoginAtUtc
    }

    RedisConnection {
        guid Id PK
        guid OwnerId FK
        string Name
        string ServerHost
        int ServerPort
        string PasswordEncrypted
        boolean Ssl
        int DatabaseCount
        int ConnectTimeout
        boolean IsReadOnly "Safe Mode Flag"
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }

    AuditLog {
        guid Id PK
        guid UserId FK
        string UserEmail
        string IpAddress
        string ActionType "CreateKey, DeleteKey, LoadTest, SeedData, CLI"
        string Resource
        guid ConnectionId FK
        string DetailsJson
        boolean IsSuccess
        datetime TimestampUtc
    }
```

---

## 3. Sequence Diagram: CQRS MediatR Pipeline Execution

```mermaid
sequenceDiagram
    autonumber
    actor Client as Angular SPA
    participant Endpoint as Minimal API Endpoint
    participant MediatR as IMediator
    participant LogPipe as LoggingBehavior
    participant ValPipe as ValidationBehavior
    participant AuditPipe as AuditingBehavior
    participant Handler as CommandHandler
    participant Service as Domain / Infra Service

    Client->>Endpoint: HTTP POST /api/v1/storage/{id}/keys
    Endpoint->>MediatR: Send(AddRedisValueCommand)
    
    MediatR->>LogPipe: Handle(request, next)
    LogPipe->>LogPipe: Log execution start with correlation ID

    LogPipe->>ValPipe: next()
    ValPipe->>ValPipe: Validate request via FluentValidation
    alt Validation Failed
        ValPipe-->>Endpoint: ValidationException / Result.Failure
        Endpoint-->>Client: 400 Bad Request (Validation Problem Details)
    end

    ValPipe->>AuditPipe: next()
    AuditPipe->>Handler: next()
    Handler->>Service: Execute Redis mutation via TypeHandler
    Service-->>Handler: Result.Success
    Handler-->>AuditPipe: Result.Success

    AuditPipe->>AuditPipe: Construct and persist AuditLog record
    AuditPipe-->>LogPipe: Result.Success
    LogPipe->>LogPipe: Log execution duration and completion
    LogPipe-->>Endpoint: Result.Success
    Endpoint-->>Client: 200 OK
```

---

## 4. Strategy Pattern for Redis Data Types

```mermaid
flowchart TD
    Req["Read / Write Key Request (Key, Type, Value, TTL)"]
    Factory["RedisTypeHandlerFactory.GetHandler(RedisType)"]
    Req --> Factory

    Factory -->|Type == String| S_Str["StringTypeHandler\n- Raw text vs JSON detection\n- Check TTL > 0 => Expiration\n- StringSetAsync"]
    Factory -->|Type == Hash| S_Hash["HashTypeHandler\n- HashEntry[] mapping\n- Inline field addition\n- HashSetAsync"]
    Factory -->|Type == List| S_List["ListTypeHandler\n- Order preservation\n- Push / Pop\n- ListRangeAsync"]
    Factory -->|Type == Set| S_Set["SetTypeHandler\n- Set uniqueness validation\n- SetMembersAsync\n- SetAddAsync"]
    Factory -->|Type == SortedSet| S_ZSet["SortedSetTypeHandler\n- Score-value pairs\n- Rank sorting\n- SortedSetAddAsync"]
    Factory -->|Type == Stream| S_Stream["StreamTypeHandler\n- Timestamped IDs\n- Event field-values\n- StreamReadAsync / StreamAddAsync"]

    DB[("IDatabase (StackExchange.Redis)")]
    S_Str --> DB
    S_Hash --> DB
    S_List --> DB
    S_Set --> DB
    S_ZSet --> DB
    S_Stream --> DB
```

---

## 5. Sequence Diagram: Real-Time Telemetry Streaming (SignalR)

```mermaid
sequenceDiagram
    autonumber
    actor Client as Angular Storage Component
    participant Hub as RedisMetricsHub (/hub/metrics)
    participant SubMgr as IHubSubscribersManager
    participant BgJob as RedisMetricsBackgroundJob
    participant Collector as RedisMetricsCollector
    participant Redis as Redis Server Engine (TCP)

    Client->>Hub: WebSocket Connect (?connectionId=Guid)
    Hub->>SubMgr: AddConnection(clientId, redisConnectionId)
    SubMgr-->>Hub: Registered

    Note over BgJob,Collector: Background timer fires every 5 seconds
    BgJob->>SubMgr: GetSubscribersMap()
    SubMgr-->>BgJob: Active subscribers per connection ID

    loop For Each Active RedisConnectionId
        BgJob->>Collector: CollectMetrics(connection)
        Collector->>Redis: INFO ALL
        Redis-->>Collector: Raw INFO text (Server, Memory, Stats, CPU)
        Collector->>Collector: Parse via CultureInfo.InvariantCulture
        Collector-->>BgJob: RedisMetrics Model (CPU, RAM, Ops, Clients)
        BgJob->>Hub: NotifyUser(clientId, metrics)
        Hub-->>Client: WebSocket Push: NotifyUserAsync(metrics JSON)
        Client->>Client: Update top telemetry pills reactively
    end
```

---

## 6. Flowchart: Synthetic Benchmark & Load Testing Workflow

```mermaid
flowchart TD
    Start["User configures benchmark (Requests, Workers, Read/Write Ratio)"] --> Validate["Parameter validation (Clamped: 10 - 100,000 requests)"]
    Validate --> SafeCheck{"Is Safe Mode (Read-Only) active?"}
    SafeCheck -->|Yes| Abort["Error 403: Write operations prohibited in Safe Mode"]
    SafeCheck -->|No| Warmup["Connection warmup: generate bench:warmup:* keys"]
    
    Warmup --> Spawn["Spawn N concurrent Task workers"]
    StopwatchStart["Start high-resolution Stopwatch"] --> Spawn

    subgraph Workers ["Concurrent Execution"]
        direction TB
        W1["Worker 1: Random(Get / Set)"]
        W2["Worker 2: Random(Get / Set)"]
        W3["Worker N: Random(Get / Set)"]
    end
    Spawn --> Workers

    Workers --> Collect["Aggregate individual operation durations"]
    Collect --> Calc["Compute total duration, RPS, latency percentiles (p50, p95, p99), errors"]
    Calc --> Cleanup["Asynchronously clean up temporary bench:* keys"]
    Cleanup --> Response["Deliver LoadTestResult report to client"]
```

---

## 7. Flowchart: Realistic Sample Data Seeder Pipeline

```mermaid
flowchart LR
    Opts["Seeder Options:\n- Total Keys\n- Target Databases\n- Key Complexity\n- Payload Complexity\n- TTL Strategy"] --> Naming["KeyNamingStrategyFactory"]
    
    Naming -->|Simple| N1["prefix:type:index"]
    Naming -->|Hierarchical| N2["org:env:dept:service:index"]
    Naming -->|UUID| N3["prefix:type:GUID"]

    N1 & N2 & N3 --> TypeDistributor["Data Type Distribution\n(String, Hash, List, Set, ZSet, Stream)"]
    
    TypeDistributor --> SeedFactory["SeedDataStrategyFactory"]
    
    SeedFactory --> S1["String: Raw / JSON / Telemetry"]
    SeedFactory --> S2["Hash: User Profiles / Permissions"]
    SeedFactory --> S3["List: Task Queues / Event logs"]
    SeedFactory --> S4["Set: Tags / Feature flags"]
    SeedFactory --> S5["Sorted Set: Leaderboards / High Scores"]
    SeedFactory --> S6["Stream: IoT Sensors / Telemetry Logs"]

    S1 & S2 & S3 & S4 & S5 & S6 --> BatchWrite["Batch write into specified DBs (0..15)"]
    BatchWrite --> Summary["Generate and return SeedDataResult summary"]
```

---

## 8. Sequence Diagram: Interactive Redis CLI Command Execution

```mermaid
sequenceDiagram
    autonumber
    actor Dev as Developer (CLI Terminal Drawer)
    participant Endpoint as ExecuteCliEndpoint (POST /execute-cli)
    participant Handler as ExecuteCliCommandHandler
    participant SafeMode as Safe Mode Policy
    participant Pool as IConnectionPool
    participant Redis as Redis Server

    Dev->>Endpoint: Submit command ("INFO memory", active DB: 0)
    Endpoint->>Handler: Send(ExecuteCliCommand)
    Handler->>SafeMode: Evaluate command permissions
    
    alt Destructive command (FLUSHALL, SHUTDOWN) in Safe Mode
        SafeMode-->>Endpoint: Result.Failure("Command forbidden in Safe Mode")
        Endpoint-->>Dev: Safety rejection message in terminal
    else Command permitted
        Handler->>Pool: GetConnection()
        Pool-->>Handler: ConnectionMultiplexer
        Handler->>Redis: ExecuteRawCommand(command, args)
        Redis-->>Handler: Raw RESP Result (Bulk String / Array / Integer)
        Handler->>Handler: Format output into terminal text representation
        Handler-->>Endpoint: Result.Success(formattedOutput)
        Endpoint-->>Dev: Print colored result to terminal with prompt
    end
```
