# RedisGUI — Повний довідник конфігурацій та налаштувань

У цьому документі наведено детальний опис усіх можливих параметрів конфігурації платформи **RedisGUI** через файли `appsettings.json`, `appsettings.Development.json`, змінні середовища (Environment Variables) та параметри Docker.

---

## 1. Структура `appsettings.json` (Головний файл конфігурації)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "StackExchange.Redis": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=redisgui;Uid=root;Pwd=root_password;"
  },
  "DatabaseProvider": "InMemory",
  "Redis": {
    "DefaultHost": "127.0.0.1",
    "DefaultPort": 6379,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "KeepAlive": 60,
    "AllowAdmin": true,
    "AbortOnConnectFail": false,
    "DefaultConnectionId": "ae3201ab-c3f1-4594-8f80-bbffd8d5eb65"
  },
  "Jwt": {
    "Secret": "SuperSecretKeyForDevelopmentOnlyPleaseChangeInProductionEnvironment12345!",
    "Issuer": "RedisGUI",
    "Audience": "RedisGUIClient",
    "AccessTokenExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "MetricsCollector": {
    "PoolingIntervalInSec": 5,
    "Enabled": true,
    "MaxHistoryDataPoints": 100
  },
  "HealthCheck": {
    "IntervalInSeconds": 15,
    "TimeoutInSeconds": 3
  },
  "Observability": {
    "ServiceName": "RedisGUI.Server",
    "ServiceVersion": "1.0.0",
    "EnableMetrics": true,
    "EnableTracing": true
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowInSeconds": 10,
    "QueueLimit": 20
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "http://localhost:5000",
      "http://127.0.0.1:4200",
      "http://127.0.0.1:5000"
    ]
  }
}
```

---

## 2. Детальний опис кожної секції конфігурації

### 2.1. Секція `DatabaseProvider` та `ConnectionStrings`
Визначає сховище метаданих системи (збережені підключення Redis, користувачі, журнали аудиту).

| Параметр | Можливі значення | За замовчуванням | Опис |
| :--- | :--- | :--- | :--- |
| `DatabaseProvider` | `InMemory`, `MySql`, `MariaDb` | `InMemory` | Двигун бази даних. `InMemory` ідеально підходить для швидкого старту та тестування; `MySql` забезпечує постійне зберігання на диску. |
| `ConnectionStrings:DefaultConnection` | Рядок підключення ADO.NET | — | Рядок підключення до реляційної СУБД (використовується при `DatabaseProvider != InMemory`). |

---

### 2.2. Секція `Redis` (Налаштування двигуна Redis за замовчуванням)
Параметри автоматично посіяного локального з'єднання та базові тайм-аути StackExchange.Redis.

| Параметр | Тип | За замовчуванням | Опис |
| :--- | :--- | :--- | :--- |
| `DefaultHost` | `string` | `127.0.0.1` | Хост для первинного автопосіву з'єднання. У середовищі Docker автоматично змінюється на ім'я контейнера `redis`. |
| `DefaultPort` | `int` | `6379` | Порт Redis сервера. |
| `ConnectTimeout` | `int` (мс) | `5000` | Максимальний час очікування встановлення TCP сокета з Redis перед генерацією помилки. |
| `SyncTimeout` | `int` (мс) | `5000` | Максимальний час очікування відповіді на синхронні команди Redis. |
| `KeepAlive` | `int` (сек) | `60` | Інтервал відправки PING-пакетів для підтримки TCP з'єднання активним. |
| `AllowAdmin` | `bool` | `true` | Дозволяє виконання адміністративних команд (`INFO`, `CONFIG GET`, `SLOWLOG GET`, `CLIENT LIST`). Обов'язково для повноцінної діагностики. |
| `AbortOnConnectFail`| `bool` | `false` | Якщо `false`, StackExchange.Redis продовжує спроби підключення у фоні при тимчасовій недоступності сервера, не перериваючи роботу застосунку. |
| `DefaultConnectionId`| `Guid` | `ae3201ab-c3f1-4594-8f80-bbffd8d5eb65` | Стабільний GUID первинного з'єднання, що дозволяє зберігати закладки та прямі посилання у браузері. |

---

### 2.3. Секція `Jwt` (Автентифікація та Безпека)
Керує створенням та перевіркою криптографічних JSON Web Tokens.

| Параметр | Тип | За замовчуванням | Опис |
| :--- | :--- | :--- | :--- |
| `Secret` | `string` | — | Секретний ключ для підпису JWT токенів алгоритмом HMAC-SHA256 (має містити щонайменше 256 біт / 32 символи). |
| `Issuer` | `string` | `RedisGUI` | Видавець токена. |
| `Audience` | `string` | `RedisGUIClient` | Цільова аудиторія токена. |
| `AccessTokenExpirationInMinutes` | `int` | `60` | Тривалість дії Access Token у хвилинах. |
| `RefreshTokenExpirationInDays` | `int` | `7` | Тривалість дії Refresh Token у днях для автоматичного поновлення сесії. |

---

### 2.4. Секція `MetricsCollector` (Збір живої телеметрії)
Керує фоновим сервісом `RedisMetricsBackgroundJob`.

| Параметр | Тип | За замовчуванням | Опис |
| :--- | :--- | :--- | :--- |
| `PoolingIntervalInSec` | `int` | `5` | Інтервал опитування Redis команди `INFO ALL` у секундах для оновлення телеметрії через SignalR. |
| `Enabled` | `bool` | `true` | Прапорець активації фонового збору метрик. |
| `MaxHistoryDataPoints` | `int` | `100` | Максимальна кількість збережених точок у пам'яті для побудови графіків. |

---

### 2.5. Секція `HealthCheck` (Моніторинг доступності)
Керує фоновим сервісом `ConnectionHealthMonitor`.

| Параметр | Тип | За замовчуванням | Опис |
| :--- | :--- | :--- | :--- |
| `IntervalInSeconds` | `int` | `15` | Частота перевірки доступності всіх зареєстрованих Redis серверів. |
| `TimeoutInSeconds` | `int` | `3` | Час очікування пінгу перед позначенням з'єднання як `Degraded` або `Unhealthy`. |

---

### 2.6. Секція `RateLimiting` (Захист API від перевантаження)
Вбудований rate limiter ASP.NET Core для захисту ендпоінтів.

| Параметр | Тип | За замовчуванням | Опис |
| :--- | :--- | :--- | :--- |
| `PermitLimit` | `int` | `100` | Кількість дозволених запитів на клієнта у межах часового вікна. |
| `WindowInSeconds` | `int` | `10` | Тривалість ковзного вікна у секундах. |
| `QueueLimit` | `int` | `20` | Розмір черги запитів, що очікують обробки при перевищенні ліміту. |

---

## 3. Змінні середовища (Environment Variables)

Будь-який параметр з `appsettings.json` можна перекрити за допомогою змінних середовища. У середовищі Docker або Kubernetes розділювач `:` замінюється на подвійне підкреслення `__`:

| Змінна середовища | Відповідне поле в JSON | Приклад значення |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | — | `Production` або `Development` |
| `ASPNETCORE_URLS` | — | `http://+:5000` |
| `DatabaseProvider` | `DatabaseProvider` | `MySql` |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | `Server=db;Port=3306;Database=redisgui;Uid=root;Pwd=secret;` |
| `Redis__DefaultHost` | `Redis:DefaultHost` | `redis` або `10.0.0.5` |
| `Redis__DefaultPort` | `Redis:DefaultPort` | `6379` |
| `Jwt__Secret` | `Jwt:Secret` | `MyProductionHighEntropySecretKey123456789!` |
| `MetricsCollector__PoolingIntervalInSec` | `MetricsCollector:PoolingIntervalInSec` | `3` |

---

## 4. Конфігурація клієнтської частини (Angular 19)

Файл конфігурації клієнта знаходиться у:
`src/RedisGUI.Client/src/environments/environment.ts` (dev)
`src/RedisGUI.Client/src/environments/environment.prod.ts` (prod)

```typescript
export const environment = {
  production: true,
  apiUrl: '', // Порожній рядок вказує на використання відносного кореня (Same-Origin)
  signalrHubs: {
    metrics: '/hub/metrics',
    keys: '/hub/keys'
  },
  defaultTheme: 'magenta-violet',
  storageKeyPrefix: 'redisgui_'
};
```
