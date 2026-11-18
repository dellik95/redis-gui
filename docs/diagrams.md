# RedisGUI — Architectural Diagrams & Operational Flowcharts

All system diagrams in **Mermaid** format illustrating the **.NET 11 + Angular 22** RedisGUI architecture, lock-free subsystems, Cluster topology handling, SignalR streaming hubs, and background job pipelines.

---

## 1. High-Level System Architecture

```mermaid
flowchart TB
    subgraph ClientLayer["Frontend Layer (Angular 22 + Tailwind + Material 22)"]
        UI["Standalone Components & Visual Editors"]
        VScroll["Storage List (*cdkVirtualFor) & Delimiter Tree"]
        Stores["NgRx Signal Stores (Keys, Jobs, Connections, Auth, SideNav)"]
        Notif["Job Notification Center + Command Palette (Ctrl+K)"]
    end

    subgraph WebLayer["Web & Real-Time API (ASP.NET Core 11)"]
        API["Versioned Minimal APIs (api/v1/* + ProblemDetails)"]
        HubMetrics["/hub/metrics (RedisMetricsHub)"]
        HubKey["/hub/keychange (KeyChangeHub)"]
        HubMsg["/hub/messaging (MessagingHub)"]
        HubJobs["/hub/jobs (BackgroundJobsHub [Authorize])"]
    end

    subgraph CoreLayer["Application & Shared Core (Lock-Free CQRS)"]
        Med["Lock-Free Mediator + PipelineRegistry (CAS Arrays)"]
        Behav["Validation, Logging & Auditing Behaviors"]
        Handlers["CQRS Handlers & IConnectionAccessService"]
        Domain["Domain Core (Entities, Value Objects, Result, Errors, IRedisTopologyRegistry)"]
    end

    subgraph InfraLayer["Infrastructure & Data Plane"]
        Pool["Lock-Free RedisConnectionManager + TopologyRegistry"]
        Jobs["BackgroundJobManager (Channel<QueuedJob> Pool + 5 IBackgroundJobHandlers)"]
        EFCache["EF Core 11 + Parameter-Aware CacheKeyProvider (FusionCache)"]
        RedisStandalone[("Redis 7/8 Standalone")]
        RedisCluster[("Redis Cluster Primaries & Replicas")]
        MySQL[("MySQL 8.4 / EF InMemory")]
    end

    UI --> Stores
    VScroll --> Stores
    Notif --> Stores
    Stores <-->|"HTTPS JSON / Query-Safe Key Routes"| API
    Stores <-->|"SignalR WebSockets"| HubMetrics
    Stores <-->|"SignalR WebSockets"| HubKey
    Stores <-->|"SignalR WebSockets"| HubMsg
    Stores <-->|"SignalR WebSockets (JWT)"| HubJobs

    API --> Med
    Med --> Behav --> Handlers --> Domain
    Handlers --> Pool
    Handlers --> Jobs
    Handlers --> EFCache
    Jobs --> HubJobs
    Pool <--> RedisStandalone
    Pool <--> RedisCluster
    EFCache <--> MySQL
```

---

## 2. CQRS Request Lifecycle & RFC 9457 Problem Details

```mermaid
sequenceDiagram
    autonumber
    participant UI as Angular Client
    participant EP as Minimal API Endpoint (api/v1)
    participant MED as Lock-Free Mediator
    participant VAL as ValidationBehavior
    participant AUTH as IConnectionAccessService
    participant HND as Command/Query Handler
    participant AUD as AuditingBehavior
    participant MAP as ErrorResultExtensions

    UI->>EP: HTTP Request (e.g. GET /storage/{id}/key?keyBase64=...)
    EP->>MED: Send(Query / Command)
    MED->>VAL: Validate via FluentValidation
    alt Validation Fails
        VAL-->>EP: ValidationFailureError
        EP-->>UI: 400 Bad Request + Field ValidationDetails
    else Valid Request
        VAL->>HND: Invoke Handler
        HND->>AUTH: CanAccessAsync(connection)
        alt Unauthorized / Not Owner
            AUTH-->>HND: Denied
            HND-->>MAP: Result.Failure("Connection.Forbidden")
            MAP-->>UI: 403 Forbidden (RFC 9457 ProblemDetails)
        else Authorized
            HND->>HND: Execute Domain / Redis Operation
            opt [Auditable] on Command
                HND->>AUD: Record AuditLog Entry
            end
            HND-->>EP: Result<T> (Success or Domain Error)
            EP-->>UI: 200 OK or Mapped HTTP Status (404 / 409 / 502 / 503)
        end
    end
```

---

## 3. Domain & Persistence Entity Model

```mermaid
erDiagram
    USER {
        Guid Id PK
        string Email UK
        string PasswordHash
        string FirstName
        string LastName
        enum Role "Admin | Standard | ReadOnly"
        bool IsActive
        DateTime CreatedAt
        DateTime LastLoginAt
    }

    REDIS_CONNECTION {
        Guid Id PK
        string ConnectionName
        string ServerHost
        int ServerPort
        int DatabaseNumber
        bool IsReadOnly
        bool IsAvailable
        Guid OwnerId FK "None for Public connections"
        string Discriminator "Anonymous | WithCredentials"
        string EncryptedUsername
        string EncryptedPassword
    }

    AUDIT_LOG {
        Guid Id PK
        Guid UserId FK
        enum Action
        string EntityType
        Guid EntityId
        string IpAddress
        string OldValue
        string NewValue
        DateTime Timestamp
    }

    USER ||--o{ REDIS_CONNECTION : "owns / manages"
    USER ||--o{ AUDIT_LOG : "performs"
```

---

## 4. Extensible Background Jobs & Live SignalR Lifecycle

```mermaid
sequenceDiagram
    autonumber
    participant UI as Feature Dialog / Notification Center
    participant STORE as BackgroundJobsStore + BackgroundJobsService
    participant API as BackgroundJobsEndpoint (/api/v1/jobs)
    participant MGR as BackgroundJobManager (Channel Worker Pool)
    participant HANDLER as Typed IBackgroundJobHandler
    participant HUB as BackgroundJobsHub (/hub/jobs)

    UI->>STORE: startJob(kind, connectionId, params)
    STORE->>API: POST /api/v1/jobs
    API->>MGR: TryEnqueue(ownerId, kind, payload, writeCheck)
    MGR-->>API: 202 Accepted (JobId + Initial Snapshot)
    API-->>STORE: Job Snapshot

    STORE->>HUB: EnsureConnected (with initial retry) & Subscribe(JobId)
    HUB->>HUB: Authorize Owner + Register in BackgroundJobSubscriptionRegistry

    MGR->>HANDLER: ExecuteAsync(context, progressCallback, ct)
    loop Every Progress Checkpoint
        HANDLER-->>MGR: Report Progress (processed / total, message)
        MGR->>HUB: Broadcast Update -> Group("job:{jobId}")
        HUB-->>UI: JobUpdated (Reactively Updates Bell + Dialog)
    end

    alt Access Revoked While Job Active or Subscribed
        MGR->>HUB: SendAsync("AccessRevoked", JobId)
        HUB->>HUB: RemoveFromGroupAsync
        HUB-->>STORE: Clear/Flag Restricted Job Access
    else Job Completes With Export Result
        HANDLER-->>MGR: Stored Result Artifact
        MGR->>HUB: JobCompleted (HasResult = true)
        UI->>API: GET /api/v1/jobs/{jobId}/result
        API->>MGR: Acquire Download Lease (pins artifact) -> Stream File
    end
```

---

## 5. Redis Cluster Topology Detection & Failover-Safe Cursor `SCAN`

```mermaid
flowchart TD
    UI["Storage List (*cdkVirtualFor) Scrolls Near Bottom"] -->|"auto-pagination"| API["GET /api/v1/connections/{id}/key-scan?cursor=..."]
    API --> Query["ScanRedisKeysQueryHandler"]
    Query --> Svc["RedisKeyService.ScanKeysAsync"]
    Svc --> Reg["IRedisTopologyRegistry.SetMode(connectionId, mode)"]
    Reg --> CheckMode{"Is Cluster?"}

    CheckMode -->|"Standalone"| Single["SCAN Active Primary DB 0..15 -> RedisKeyScanPage (Binary KeyBase64)"]
    CheckMode -->|"Cluster (DB 0 Only)"| ValidateDB{"Requested DB == 0?"}

    ValidateDB -->|"DB != 0"| RejectDB["Fail: Redis.InvalidDatabase (400)"]
    ValidateDB -->|"DB == 0"| CursorCheck{"Continuation Cursor Provided?"}

    CursorCheck -->|"Null (Page 1)"| EnumPrim["Enumerate Connected Cluster Primaries & Build Signed Epoch Token"]
    CursorCheck -->|"Present"| VerifySig["Verify HMAC Token Signature & Primary Topology Snapshot"]

    VerifySig -->|"Topology Changed / CLUSTER FAILOVER"| RejectCursor["Fail: Redis.InvalidCursor / Redis.ScanUnavailable"]
    VerifySig -->|"Topology Valid"| NextShard["SCAN Next Primary Shard -> Collect Keys + Binary Flags"]

    EnumPrim --> NextShard
    NextShard --> PageOut["Return RedisKeyScanPage (Keys, NextCursor, ServerMode=Cluster)"]
```

---

## 6. Lock-Free `RedisConnectionManager` State Machine

```mermaid
stateDiagram-v2
    [*] --> Connecting : First GetConnection(stableKey, config)
    Connecting --> Active : Multiplexer connected & healthy
    Connecting --> Faulted : Connect timeout / DNS failure

    state Active {
        [*] --> Idle
        Idle --> InUse : TryAcquireOperation() atomic increment
        InUse --> Idle : Dispose operation handle atomic decrement
    }

    Active --> Retiring : Config fingerprint changed OR IdleTimeout expired
    Retiring --> Disposed : DrainTimeout elapsed & active ops == 0
    Faulted --> Disposed : Cleanup failed entry
    Disposed --> [*]
```

---

## 7. Real-Time Telemetry & Messaging Inspector Hubs

```mermaid
sequenceDiagram
    autonumber
    participant Client as Angular Dashboard / Messaging UI
    participant MetricsHub as /hub/metrics (RedisMetricsHub)
    participant MsgHub as /hub/messaging (MessagingHub)
    participant MetricsJob as RedisMetricsBackgroundJob
    participant MsgSvc as RedisMessagingService
    participant Redis as Redis Server / Cluster

    par Telemetry Broadcast
        Client->>MetricsHub: Connect(?connectionId=...)
        loop Every PoolingIntervalInSec
            MetricsJob->>Redis: INFO Stats / Memory / CPU
            MetricsJob-->>MetricsHub: NotifyUser(MetricsDto)
            MetricsHub-->>Client: Update Signal Sparklines
        end
    and Live Messaging Inspector
        Client->>MsgHub: WatchChannel(connId, pattern) / WatchStream(connId, key)
        MsgHub->>MsgHub: CanAccessAsync + Enforce DB 0 if Cluster
        MsgHub->>MsgSvc: SubscribeAsync / XREAD Polling
        Redis-->>MsgSvc: Live Messages + Delivery Metrics (1Hz)
        MsgSvc-->>Client: Stream Entries & Chart Samples
    end
```
