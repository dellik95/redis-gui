# RedisGUI — Детальний опис проєктів рішення (Solution Structure)

Рішення **RedisGUI** (`RedisGUI.slnx`) побудовано за принципами **Clean Architecture** (Чистої Архітектури) Роберта Мартіна з чітким розділенням відповідальностей за шарами. Рішення містить 10 проєктів: 5 основних модулів .NET, клієнтський проєкт на Angular 19 та 4 тестові проєкти xUnit/Moq.

```
RedisGUI.slnx
│
├── 📂 src/
│   ├── 📦 RedisGUI.Domain/           # Підприємницькі моделі, сутності, enums, доменні контракти
│   ├── 📦 RedisGUI.Shared/           # Загальні примітиви Result<T>, Error, кастомний MediatR пайплайн
│   ├── 📦 RedisGUI.Application/      # CQRS Команди, Запити, Валідатори (FluentValidation), Behaviors
│   ├── 📦 RedisGUI.Infrastructure/   # Реалізація StackExchange.Redis, EF Core, SignalR, Services
│   ├── 📦 RedisGUI.Server/           # ASP.NET Core 10 Host, Minimal APIs, SignalR Hubs, OpenAPI
│   └── 📦 RedisGUI.Client/           # Angular 19 Standalone, Signals, Tailwind CSS, Monaco Editor
│
└── 📂 test/
    ├── 🧪 RedisGUI.Domain.Tests/
    ├── 🧪 RedisGUI.Shared.Tests/
    ├── 🧪 RedisGUI.Application.Tests/
    └── 🧪 RedisGUI.Infrastructure.Tests/
```

---

## 1. `RedisGUI.Domain` (Доменний шар)
Найглибший шар системи, що не має жодних зовнішніх залежностей від баз даних чи фреймворків.

### Основні сутності та моделі:
* **`Connection/RedisConnection`**: Сутність збереженого підключення до Redis. Містить:
  * `ServerHost` (Value Object), `ServerPort`, `DatabaseCount`, `Name`, `Password`, `Ssl`, `ConnectTimeout`.
  * `IsReadOnly`: Прапорець Safe Mode (режим захисту від запису).
  * `OwnerId`: Зовнішній ключ власника з'єднання.
  * Метод `BuildConnectionString()`: Генерація безпечного рядка підключення StackExchange.Redis.
* **`Users/User`**: Сутність облікового запису користувача з полями `Email`, `PasswordHash`, `Role` (`UserRole`), `IsActive`, `LastLoginAt`.
* **`Audit/AuditLog`**: Сутність журналу аудиту операцій із міткою часу `Timestamp`, `UserId`, `UserEmail`, `IpAddress`, `ActionType`, `Resource`, `Details`, `IsSuccess`.
* **`RedisMetrics/RedisMetrics`**: Агрегат метрик продуктивності Redis, заповнюваний через спеціальні атрибути `[RedisMetric]` (Ops/sec, Used Memory, Peak Memory, CPU System/User, Connected Clients, Key Count, Network In/Out).
* **`Redis/RedisLoadTest`**: Моделі налаштування та результатів навантажувального тестування (`LoadTestResult`, `LoadTestOptions`, процентилі латентності `p50`, `p95`, `p99`, RPS, `ErrorCount`).
* **`Redis/RedisSlowlogItem`**: Модель повільних запитів Redis (`Id`, `Timestamp`, `DurationMicroseconds`, `Arguments`, `ClientIp`).
* **`Redis/SeedDataOptions`**: Параметри генерації демо-даних (`TotalKeys`, `KeyComplexity`, `PayloadComplexity`, `DataTypes`, `TtlStrategy`).

### Доменні інтерфейси та контракти:
* `IRedisConnectionRepository`: Контракт доступу до збережених підключень.
* `IUserRepository`: Контракт доступу до облікових записів.
* `IAuditLogRepository`: Контракт запису та фільтрації журналу аудиту.
* `IRedisValueService`: Контракт високорівневих операцій над значеннями ключів Redis.
* `IRedisCliService`: Контракт виконання команд через інтерактивний термінал CLI.
* `IRedisDiagnosticsService`: Контракт отримання діагностики сервера (Slowlog, Server Config).
* `IRedisLoadTestService`: Контракт синтетичного навантажувального тестування.
* `IRedisSeedDataService`: Контракт генерації демо-даних за типами та стратегіями.

---

## 2. `RedisGUI.Shared` (Спільне ядро та примітиви)
Містить базові будівельні блоки для забезпечення надійності та CQRS-пайплайну.
* **`Result<T>` та `Error`**: Паттерн залізничного програмування (Railway-Oriented Programming). Забезпечує відсутність неперехоплених `null` та замінює винятки контрольованими результатами з кодом та описом помилки.
* **Кастомний MediatR / In-Process Pipeline**:
  * Інтерфейси: `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>`, `IMediator`, `IPipelineBehavior`.
  * Дозволяє створювати пайплайни декораторів без сторонніх громіздких бібліотек.

---

## 3. `RedisGUI.Application` (Шар бізнес-логіки / CQRS)
Імплементує бізнес-сценарії (Use Cases) як окремі Команди (Commands) та Запити (Queries).

### Основні групи сценаріїв:
1. **`Connections/`**:
   * `CreateConnectionCommand`, `UpdateConnectionCommand`, `DeleteConnectionCommand`, `CheckConnectionCommand`, `GetAllConnectionsQuery`, `GetConnectionByIdQuery`, `GetConnectionHealthQuery`, `ToggleReadOnlyCommand`, `GetServerInfoQuery`, `GetServerConfigQuery`, `UpdateServerConfigCommand`.
2. **`Redis/` (Управління ключами та структурами)**:
   * `GetAllRedisKeysQuery`: Посторінковий пошук ключів за патерном через `SCAN`.
   * `GetRedisValueQuery`: Отримання значення довільного типу через фабрику хендлерів.
   * `AddRedisValueCommand`: Створення нового ключа з контролем типу, значення та TTL.
   * `UpdateRedisValueCommand`: Модифікація існуючого ключа.
   * `DeleteRedisKeyCommand`, `DeleteMultipleKeysCommand`, `DeleteKeysByPatternCommand`.
   * `RenameKeyCommand`: Безпечне перейменування ключа (`RENAME`).
   * `DuplicateKeyCommand`: Клонування ключа з типом та TTL (`DUMP` + `RESTORE`).
   * `BulkUpdateTtlCommand`: Масове оновлення або скидання часу життя ключів.
   * `ExecuteCliCommand`: Виконання команд терміналу з валідацією Safe Mode.
   * `RunLoadTestCommand`: Запуск навантажувального тестування з паралельними воркерами.
   * `SeedSampleDataCommand`: Генерація наборів даних.
   * `ExportRedisDataQuery` / `ImportRedisDataCommand`: Експорт/імпорт у JSON/CSV.
   * `GetKeyMemoryUsageQuery`: Оцінка споживання пам'яті через `MEMORY USAGE`.
   * `GetSlowlogQuery`: Отримання записів `SLOWLOG GET`.
3. **`Users/`**:
   * `LoginUserCommand`, `RegisterUserCommand`, `RefreshTokenCommand`, `ChangePasswordCommand`.
4. **`Audit/`**:
   * `GetAuditLogsQuery`: Посторінковий запит журналу аудиту з гнучкою фільтрацією.
5. **Pipeline Behaviors**:
   * `ValidationBehavior`: Автоматична валідація запитів за допомогою правил `FluentValidation`.
   * `AuditingBehavior`: Автоматична реєстрація записів аудиту для операцій з атрибутом `[Auditable]`.
   * `LoggingBehavior`: Структуроване логування запитів MediatR із часом виконання.

---

## 4. `RedisGUI.Infrastructure` (Шар інфраструктури)
Реалізує взаємодію з зовнішніми системами: Redis (StackExchange.Redis), базою даних EF Core, SignalR та операційною системою.

### Ключові сервіси та паттерни:
* **Пул з'єднань `IConnectionPool` (`ConnectionPool.cs`)**:
  * Забезпечує потокобезпечне кешування `ConnectionMultiplexer`.
  * Підтримує автоматичне відновлення після збоїв та плавний fallback з внутрішнього імені Docker (`redis:6379`) на локальний `127.0.0.1:6379` у dev-середовищі.
* **Паттерн Стратегія для типів Redis (`TypeHandlers/`)**:
  * `IRedisTypeHandler`, `IRedisTypeHandlerFactory`.
  * `StringTypeHandler`, `HashTypeHandler`, `ListTypeHandler`, `SetTypeHandler`, `SortedSetTypeHandler`, `StreamTypeHandler`.
  * Зареєстровані в DI-контейнері як `Singleton`.
* **Паттерни генерації даних (`Seeding/`)**:
  * `IKeyNamingStrategy`: Стратегії формування імен ключів (`SimpleKeyNamingStrategy`, `HierarchicalKeyNamingStrategy`, `UuidKeyNamingStrategy`).
  * `ISeedDataStrategy`: Стратегії наповнення корисним навантаженням для кожного типу Redis.
* **Навантажувальний тестер (`LoadTesting/`)**:
  * `RedisLoadTestService`: Запускає пул паралельних асинхронних задач з контрольованим розподілом Read/Write, фіксує таймінги за допомогою `Stopwatch` та розраховує перцентилі `p50`, `p95`, `p99`.
* **Телеметрія та фонові задачі (`BackgroundJobs/`)**:
  * `RedisMetricsBackgroundJob`: Фоновий сервіс (`BackgroundService`), що кожні 5 секунд збирає `INFO ALL` через `RedisMetricsCollector` (з `CultureInfo.InvariantCulture`) та транслює в SignalR Hub.
  * `ConnectionHealthMonitor`: Моніторинг доступності з'єднань.
* **SignalR Хаби (`SignalR/Hubs/`)**:
  * `RedisMetricsHub`: Хаб трансляції телеметрії (`/hub/metrics`) за методом `NotifyUserAsync`.
  * `KeyChangeHub`: Хаб сповіщень про модифікації простору ключів (`/hub/keys`).
* **Entity Framework Core (`Persistence/`)**:
  * `ApplicationDbContext`: Моделі для користувачів, з'єднань та аудит-логів з підтримкою In-Memory сховища, MySQL та MariaDB.

---

## 5. `RedisGUI.Server` (Шар хостингу / Web API)
ASP.NET Core 10 Minimal API хост.
* **Ендпоінти (`Endpoints/`)**:
  * `Connections/`: Керування з'єднаннями, статус здоров'я, інфо про сервер, конфігурація.
  * `RedisStorage/`: CRUD над ключами, CLI, імпорт/експорт, load-test, seed, slowlog.
  * `Auth/`: Вхід, реєстрація, токени.
  * `Audit/`: Журнали дій.
* **Middleware**:
  * `RequestLoggingMiddleware`: Логування кожного HTTP-запиту з Correlation ID.
  * `GlobalExceptionHandler`: Перехоплення винятків та повернення стандартизованого RFC 7807 Problem Details.
  * `JwtMiddleware`: Авторизація токенів.
* **OpenApi & Swagger**: Автоматична генерація схем OpenAPI з підтримкою версіонування.

---

## 6. `RedisGUI.Client` (Frontend / Angular 19)
Сучасний односторінковий застосунок (SPA) на **Angular 19** без `NgModule` (чисті Standalone Components).
* **Signals & State Management**:
  * `AuthStore`: Реактивний сигнал стану користувача та ролей.
  * `RedisKeysStore`: Керування списком ключів, пагінацією, фільтрами за типами та просторами імен.
  * `ThemeService`: Керування 6 кольоровими палітрами та режимами Dark/Light.
* **Value Handlers (Client Strategy)**:
  * Клієнтське дзеркало серверних стратегій: `StringTypeHandler`, `HashTypeHandler`, `ListTypeHandler`, `SetTypeHandler`, `SortedSetTypeHandler`, `StreamTypeHandler`.
  * Забезпечують валідацію, конвертацію візуального інтерфейсу в строгий JSON та назад.
* **UI-компоненти**:
  * `DashboardComponent`: Віджети здоров'я та списки підключень.
  * `StorageManagementComponent`: Табличний та деревоподібний режими, панель ресурсів, живі метрики.
  * `StorageValueEditorComponent`: Візуальні та JSON-редактори для кожного типу даних.
  * `RedisCliDrawerComponent`: Висувний інтерактивний термінал консолі Redis з історією.
  * `LoadTestDialogComponent`: Інтерфейс запуску тестів продуктивності.
  * `SeedDataDialogComponent`: Майстер генерації демо-даних.
  * `SlowlogDialogComponent`: Аналізатор повільних запитів.
  * `CommandPaletteComponent`: Швидка палітра команд за комбінацією `Ctrl+K`.

---

## 7. `test/` (Тестові проєкти)
Покриття юніт- та інтеграційними тестами:
* **`RedisGUI.Domain.Tests`**: 132 тести. Тестування сутностей `RedisConnection`, `User`, `AuditLog`, моделей метрик, Value Objects, перевірок ролей.
* **`RedisGUI.Shared.Tests`**: 98 тестів. Тестування `Result<T>`, обробки помилок, власного пайплайну MediatR.
* **`RedisGUI.Application.Tests`**: 98 тестів. Тестування CQRS-хендлерів (AddKey, DuplicateKey, RenameKey, ExecuteCli, LoadTest, SeedData, GetKeyspaceInfo, GetKeyMemoryUsage) з використанням `Moq`.
* **`RedisGUI.Infrastructure.Tests`**: 32 тести. Тестування пулу з'єднань, шифрування паролів, парсерів телеметрії, безпечного fallback'у хостів.
* **Загальний підсумок**: 350+ успішних тестів (`0 failed`).
