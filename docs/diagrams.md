# RedisGUI — Архітектурні діаграми, послідовності та флоучарти

У цьому розділі наведено вичерпну колекцію діаграм Mermaid, що описують архітектурні рішення, структури даних та динаміку ключових процесів платформи **RedisGUI**.

---

## 1. Загальна архітектура системи (Clean Architecture Flowchart)

```mermaid
flowchart TD
    subgraph UI ["Шар представлення (Presentation Layer)"]
        Browser["Веб-браузер Користувача"]
        AngularApp["Angular 19 SPA (Standalone & Signals)"]
        SignalStore["Signal Stores (Auth, RedisKeys, Theme)"]
        Browser <-->|DOM Events / Render| AngularApp
        AngularApp --> SignalStore
    end

    subgraph Host ["Шар хостингу (API Host Layer)"]
        ServerAPI["ASP.NET Core 10 Minimal APIs"]
        SignalRHubs["SignalR Hubs (/hub/metrics, /hub/keys)"]
        Middlewares["Middlewares (Auth, ErrorHandling, RateLimiting)"]
    end

    subgraph AppLayer ["Шар бізнес-логіки (Application Layer)"]
        MediatR["MediatR Pipeline"]
        Behaviors["Behaviors (Audit, Validation, Logging)"]
        Commands["Commands / Queries (CQRS)"]
        Validators["FluentValidation Rules"]
    end

    subgraph DomainLayer ["Доменний шар (Domain Layer)"]
        Entities["Entities (RedisConnection, User, AuditLog)"]
        ValueObjects["Value Objects (ConnectionHost, KeyPath)"]
        DomainContracts["Abstractions (IRedisValueService, IConnectionPool)"]
    end

    subgraph InfraLayer ["Шар інфраструктури (Infrastructure Layer)"]
        ConnPool["IConnectionPool (ConnectionMultiplexer Cache)"]
        TypeFactory["IRedisTypeHandlerFactory (Strategy Pattern)"]
        BgServices["Background Jobs (RedisMetricsJob, HealthMonitor)"]
        EFCore["Entity Framework Core (InMemory / MySQL)"]
    end

    subgraph External ["Зовнішні сервери"]
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

## 2. Модель сутностей даних (Entity-Relationship Diagram)

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

## 3. Діаграма послідовності: CQRS MediatR Pipeline

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
    LogPipe->>LogPipe: Запис у лог про початок виконання

    LogPipe->>ValPipe: next()
    ValPipe->>ValPipe: Валідація через FluentValidation
    alt Валідація не пройшла
        ValPipe-->>Endpoint: ValidationException / Result.Failure
        Endpoint-->>Client: 400 Bad Request (Деталі помилок валідації)
    end

    ValPipe->>AuditPipe: next()
    AuditPipe->>Handler: next()
    Handler->>Service: Виконання операції збереження в Redis
    Service-->>Handler: Result.Success
    Handler-->>AuditPipe: Result.Success

    AuditPipe->>AuditPipe: Формування та збереження AuditLog запису
    AuditPipe-->>LogPipe: Result.Success
    LogPipe->>LogPipe: Фіксація тривалості виконання
    LogPipe-->>Endpoint: Result.Success
    Endpoint-->>Client: 200 OK
```

---

## 4. Паттерн Стратегія (Strategy Pattern) для типів даних Redis

```mermaid
flowchart TD
    Req["Запит на читання / запис ключа (Key, Type, Value, TTL)"]
    Factory["RedisTypeHandlerFactory.GetHandler(RedisType)"]
    Req --> Factory

    Factory -->|Type == String| S_Str["StringTypeHandler\n- Raw text vs JSON\n- Check TTL > 0 => Expiration\n- StringSetAsync"]
    Factory -->|Type == Hash| S_Hash["HashTypeHandler\n- HashEntry[] mapping\n- Inline field addition\n- HashSetAsync"]
    Factory -->|Type == List| S_List["ListTypeHandler\n- Order preservation\n- Push / Pop\n- ListRangeAsync"]
    Factory -->|Type == Set| S_Set["SetTypeHandler\n- Distinct uniqueness\n- SetMembersAsync\n- SetAddAsync"]
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

## 5. Діаграма послідовності: Жива трансляція телеметрії (SignalR Telemetry)

```mermaid
sequenceDiagram
    autonumber
    actor Client as Angular Storage Component
    participant Hub as RedisMetricsHub (/hub/metrics)
    participant SubMgr as IHubSubscribersManager
    participant BgJob as RedisMetricsBackgroundJob
    participant Collector as RedisMetricsCollector
    participant Redis as Двигун Redis (TCP)

    Client->>Hub: WebSocket Connect (?connectionId=Guid)
    Hub->>SubMgr: AddConnection(clientId, redisConnectionId)
    SubMgr-->>Hub: Registered

    Note over BgJob,Collector: Кожні 5 секунд спрацьовує фоновий таймер
    BgJob->>SubMgr: GetSubscribersMap()
    SubMgr-->>BgJob: Словник активних підписників за з'єднаннями

    loop Для кожного активного RedisConnectionId
        BgJob->>Collector: CollectMetrics(connection)
        Collector->>Redis: INFO ALL
        Redis-->>Collector: Текст секцій (Server, Memory, Stats, CPU)
        Collector->>Collector: Парсинг за допомогою CultureInfo.InvariantCulture
        Collector-->>BgJob: RedisMetrics Model (CPU, RAM, Ops, Clients)
        BgJob->>Hub: NotifyUser(clientId, metrics)
        Hub-->>Client: WebSocket Push: NotifyUserAsync(metrics JSON)
        Client->>Client: Оновлення пігулок у верхній панелі
    end
```

---

## 6. Флоучарт синтетичного навантажувального тестування (Load Testing Workflow)

```mermaid
flowchart TD
    Start["Користувач налаштовує параметри (Операції, Воркери, Read/Write Ratio)"] --> Validate["Валідація параметрів (Math.Clamp: 10-100,000 ops)"]
    Validate --> SafeCheck{"Чи активний Safe Mode?"}
    SafeCheck -->|Так| Abort["Помилка 403: Write-операції заборонені в Safe Mode"]
    SafeCheck -->|Ні| Warmup["Прогрів пулу: генерація тестових ключів bench:warmup:*"]
    
    Warmup --> Spawn["Запуск N паралельних Task-воркерів"]
    Spawn --> StopwatchStart["Старт таймера високої точності Stopwatch"]

    subgraph Workers ["Конкурентне виконання"]
        direction TB
        W1["Worker 1: Random(Get / Set)"]
        W2["Worker 2: Random(Get / Set)"]
        W3["Worker N: Random(Get / Set)"]
    end
    StopwatchStart --> Workers

    Workers --> Collect["Збір індивідуальних таймінгів кожної операції"]
    Collect --> Calc["Обчислення: Загальний час, RPS, Латентність (p50, p95, p99), Помилки"]
    Calc --> Cleanup["Асинхронне автоочищення тестових ключів bench:*"]
    Cleanup --> Response["Повернення звіту LoadTestResult на фронтенд"]
```

---

## 7. Флоучарт генератора демо-даних (Smart Data Seeder Pipeline)

```mermaid
flowchart LR
    Opts["Параметри посіву:\n- Кількість ключів\n- Бази даних\n- Складність ключів\n- Складність корисного навантаження\n- Стратегія TTL"] --> Naming["KeyNamingStrategyFactory"]
    
    Naming -->|Simple| N1["prefix:type:index"]
    Naming -->|Hierarchical| N2["org:env:dept:service:index"]
    Naming -->|UUID| N3["prefix:type:GUID"]

    N1 & N2 & N3 --> TypeDistributor["Розподіл за типами даних\n(String, Hash, List, Set, ZSet, Stream)"]
    
    TypeDistributor --> SeedFactory["SeedDataStrategyFactory"]
    
    SeedFactory --> S1["String: Simple / JSON / Telemetry"]
    SeedFactory --> S2["Hash: User Profiles / Permissions"]
    SeedFactory --> S3["List: Job Queues / Audit trails"]
    SeedFactory --> S4["Set: Tags / Feature flags"]
    SeedFactory --> S5["Sorted Set: Leaderboards / Scores"]
    SeedFactory --> S6["Stream: IoT Sensors / Telemetry Logs"]

    S1 & S2 & S3 & S4 & S5 & S6 --> BatchWrite["Пакетний запис у вказані БД (DB 0..15)"]
    BatchWrite --> Summary["Формування звіту SeedDataResult"]
```

---

## 8. Діаграма послідовності: Виконання команд у Redis CLI Terminal

```mermaid
sequenceDiagram
    autonumber
    actor Dev as Розробник (UI CLI Drawer)
    participant Endpoint as ExecuteCliEndpoint (POST /execute-cli)
    participant Handler as ExecuteCliCommandHandler
    participant SafeMode as Safe Mode Check
    participant Pool as IConnectionPool
    participant Redis as Redis Server

    Dev->>Endpoint: Введення команди ("INFO memory", активна DB: 0)
    Endpoint->>Handler: Send(ExecuteCliCommand)
    Handler->>SafeMode: Перевірка дозволу команди
    
    alt Команда деструктивна (FLUSHALL, SHUTDOWN) у Safe Mode
        SafeMode-->>Endpoint: Result.Failure("Command forbidden in Safe Mode")
        Endpoint-->>Dev: Червоне повідомлення про блокування безпеки
    else Команда дозволена
        Handler->>Pool: GetConnection()
        Pool-->>Handler: ConnectionMultiplexer
        Handler->>Redis: ExecuteArbitraryCommand(command, args)
        Redis-->>Handler: Raw RESP Result (Bulk String / Array / Integer)
        Handler->>Handler: Форматування у текстовий вивід терміналу
        Handler-->>Endpoint: Result.Success(formattedOutput)
        Endpoint-->>Dev: Друк результату в консоль із підсвіткою синтаксису
    end
```
