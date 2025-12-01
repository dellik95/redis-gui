# Architecture Documentation

## Table of Contents

- [Overview](#overview)
- [Architectural Patterns](#architectural-patterns)
- [System Architecture](#system-architecture)
- [Layer Architecture](#layer-architecture)
- [Data Flow](#data-flow)
- [Design Patterns](#design-patterns)
- [Real-time Communication](#real-time-communication)
- [Caching Strategy](#caching-strategy)
- [Security](#security)

## Overview

Redis GUI is built using **Clean Architecture** principles combined with **Domain-Driven Design (DDD)** and **CQRS** patterns. The architecture emphasizes separation of concerns, testability, and maintainability.

### Core Principles

1. **Dependency Inversion**: Dependencies point inward toward the domain
2. **Framework Independence**: Business logic is isolated from frameworks
3. **Testability**: All layers can be tested independently
4. **UI Independence**: Business logic is decoupled from UI
5. **Database Independence**: Can switch databases without affecting business logic

## Architectural Patterns

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation                          │
│              (RedisGUI.Server + Client)                  │
│  ┌───────────────────────────────────────────────────┐  │
│  │            Application Layer                       │  │
│  │         (RedisGUI.Application)                     │  │
│  │  ┌─────────────────────────────────────────────┐  │  │
│  │  │         Domain Layer                        │  │  │
│  │  │      (RedisGUI.Domain)                      │  │  │
│  │  │                                             │  │  │
│  │  │  - Entities                                 │  │  │
│  │  │  - Value Objects                            │  │  │
│  │  │  - Domain Events                            │  │  │
│  │  │  - Interfaces                               │  │  │
│  │  └─────────────────────────────────────────────┘  │  │
│  │                                                    │  │
│  │  - Use Cases (Commands/Queries)                   │  │
│  │  - DTOs                                            │  │
│  │  - Validation                                      │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  - API Endpoints                                         │
│  - SignalR Hubs                                          │
│  - Dependency Injection                                  │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│             Infrastructure Layer                         │
│          (RedisGUI.Infrastructure)                       │
│                                                          │
│  - Database (EF Core)                                    │
│  - Redis Client                                          │
│  - Caching                                               │
│  - External Services                                     │
│  - Background Jobs                                       │
└─────────────────────────────────────────────────────────┘
```

### CQRS Pattern

Commands and Queries are separated to optimize read and write operations independently.

```
┌──────────────┐
│   Client     │
└──────┬───────┘
       │
       ├─────────────────┐
       │                 │
       ▼                 ▼
┌─────────────┐   ┌─────────────┐
│  Commands   │   │   Queries   │
│  (Write)    │   │   (Read)    │
└──────┬──────┘   └──────┬──────┘
       │                 │
       ▼                 ▼
┌─────────────┐   ┌─────────────┐
│ Command     │   │ Query       │
│ Handlers    │   │ Handlers    │
└──────┬──────┘   └──────┬──────┘
       │                 │
       └────────┬────────┘
                ▼
        ┌───────────────┐
        │  Repository   │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │   Database    │
        └───────────────┘
```

## System Architecture

### High-Level System Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Browser                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Angular Application (Port 4200)                          │  │
│  │  - Components                                             │  │
│  │  - Services                                               │  │
│  │  - SignalR Client                                         │  │
│  └────────────────┬──────────────────────┬───────────────────┘  │
└───────────────────┼──────────────────────┼──────────────────────┘
                    │ HTTPS                │ WebSocket
                    │ REST API             │ SignalR
                    ▼                      ▼
┌─────────────────────────────────────────────────────────────────┐
│            ASP.NET Core Web API (Port 7234)                     │
│  ┌──────────────────────┐          ┌─────────────────────┐     │
│  │   API Endpoints      │          │   SignalR Hubs      │     │
│  │   - Connections      │          │   - MetricsHub      │     │
│  │   - Redis Storage    │          └──────────┬──────────┘     │
│  └──────────┬───────────┘                     │                │
│             │                                 │                │
│             ▼                                 ▼                │
│  ┌────────────────────────────────────────────────────────┐   │
│  │         Application Layer (MediatR)                    │   │
│  │  ┌──────────────┐         ┌──────────────────┐        │   │
│  │  │  Commands    │         │     Queries      │        │   │
│  │  │  - Create    │         │     - GetAll     │        │   │
│  │  │  - Update    │         │     - GetById    │        │   │
│  │  │  - Delete    │         │     - Search     │        │   │
│  │  └──────┬───────┘         └────────┬─────────┘        │   │
│  └─────────┼──────────────────────────┼──────────────────┘   │
│            │                          │                       │
│            ▼                          ▼                       │
│  ┌────────────────────────────────────────────────────────┐   │
│  │         Infrastructure Layer                           │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │   │
│  │  │ Repositories │  │ Redis Client │  │   Caching   │  │   │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬──────┘  │   │
│  └─────────┼──────────────────┼──────────────────┼────────┘   │
└────────────┼──────────────────┼──────────────────┼────────────┘
             │                  │                  │
             ▼                  ▼                  ▼
┌────────────────────┐  ┌──────────────┐  ┌──────────────┐
│   MySQL Database   │  │ Redis Server │  │ Memory Cache │
│   (Port 3306)      │  │              │  │              │
└────────────────────┘  └──────────────┘  └──────────────┘
```

## Layer Architecture

### 1. Domain Layer (`RedisGUI.Domain`)

**Responsibility**: Core business logic and rules

**Key Components**:

```
RedisGUI.Domain/
├── Entities
│   ├── Entity.cs                    # Base entity with identity
│   ├── RedisConnection.cs           # Base connection entity
│   ├── AnonymousRedisConnection.cs  # Connection without auth
│   └── RedisConnectionWithCredentials.cs
│
├── Value Objects
│   ├── ConnectionName.cs
│   ├── ConnectionHost.cs
│   ├── ConnectionPort.cs
│   └── ConnectionCredentials.cs
│
├── Domain Events
│   ├── ConnectionCreatedDomainEvent.cs
│   ├── ConnectionEstablishedDomainEvent.cs
│   └── ConnectionAvailabilityChanged.cs
│
├── Abstractions
│   ├── IRepository.cs
│   ├── IUnitOfWork.cs
│   └── IDomainEvent.cs
│
└── Primitives
    ├── Result.cs                    # Result pattern
    └── Error.cs                     # Error handling
```

**Design Principles**:
- No dependencies on other layers
- Rich domain models with behavior
- Immutable value objects
- Domain events for cross-aggregate communication

### 2. Application Layer (`RedisGUI.Application`)

**Responsibility**: Application logic and use cases

**Key Components**:

```
RedisGUI.Application/
├── Commands
│   ├── CreateConnection
│   │   ├── CreateConnectionCommand.cs
│   │   ├── CreateConnectionCommandHandler.cs
│   │   └── CreateConnectionCommandValidation.cs
│   │
│   └── DeleteConnection
│       ├── DeleteConnectionCommand.cs
│       └── DeleteConnectionCommandHandler.cs
│
├── Queries
│   ├── GetAllConnections
│   │   ├── GetAllConnectionsQuery.cs
│   │   └── GetAllConnectionsQueryHandler.cs
│   │
│   └── GetConnectionById
│       ├── GetConnectionByIdQuery.cs
│       └── GetConnectionByIdQueryHandler.cs
│
├── Behaviors
│   ├── ValidationBehavior.cs        # FluentValidation pipeline
│   └── LoggingBehavior.cs          # Request/response logging
│
└── Abstractions
    ├── ICommand.cs
    ├── ICommandHandler.cs
    ├── IQuery.cs
    └── IQueryHandler.cs
```

**CQRS Implementation**:

```csharp
// Command Example
public record CreateConnectionCommand(
    string Name,
    string Host,
    int Port,
    string? Username,
    string? Password
) : ICommand<Guid>;

public class CreateConnectionCommandHandler 
    : ICommandHandler<CreateConnectionCommand, Guid>
{
    private readonly IRedisConnectionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(
        CreateConnectionCommand command,
        CancellationToken cancellationToken)
    {
        // Business logic here
    }
}

// Query Example
public record GetAllConnectionsQuery : IQuery<List<GetConnectionResponse>>;

public class GetAllConnectionsQueryHandler 
    : IQueryHandler<GetAllConnectionsQuery, List<GetConnectionResponse>>
{
    private readonly IRedisConnectionRepository _repository;

    public async Task<Result<List<GetConnectionResponse>>> Handle(
        GetAllConnectionsQuery query,
        CancellationToken cancellationToken)
    {
        // Query logic here
    }
}
```

### 3. Infrastructure Layer (`RedisGUI.Infrastructure`)

**Responsibility**: External concerns and implementations

**Key Components**:

```
RedisGUI.Infrastructure/
├── Persistence
│   ├── ApplicationDbContext.cs
│   ├── Repositories/
│   │   ├── RepositoryBase.cs
│   │   └── RedisConnectionRepository.cs
│   ├── Interceptors/
│   │   └── CachingInterceptor.cs
│   └── Migrations/
│
├── Redis
│   ├── ConnectionPool.cs
│   ├── ConnectionService.cs
│   └── IConnectionPool.cs
│
├── Caching
│   ├── CacheKeyProvider.cs
│   ├── DbCommandInterceptorProcessor.cs
│   └── CacheConfiguration.cs
│
├── SignalR
│   ├── Hubs/
│   │   └── RedisMetricsHub.cs
│   ├── NotificationService.cs
│   └── HubSubscribersManager.cs
│
├── BackgroundJobs
│   └── RedisMetricsBackgroundJob.cs
│
└── Cryptography
    └── PasswordEncryptor.cs
```

### 4. Presentation Layer (`RedisGUI.Server`)

**Responsibility**: API endpoints and HTTP concerns

**Minimal API Endpoints**:

```
RedisGUI.Server/
├── Endpoints
│   ├── Connections/
│   │   ├── CreateConnectionEndpoint.cs
│   │   ├── GetAllConnectionsEndpoint.cs
│   │   ├── GetConnectionEndpoint.cs
│   │   ├── DeleteConnectionEndpoint.cs
│   │   └── CheckConnectionEndpoint.cs
│   │
│   └── RedisStorage/
│       ├── GetAllRedisKeysEndpoint.cs
│       ├── GetRedisValueEndpoint.cs
│       ├── AddRedisValueEndpoint.cs
│       └── DeleteRedisValueEndpoint.cs
│
├── Extensions
│   ├── EndpointsExtensions.cs
│   └── ApplicationBuilderExtensions.cs
│
└── OpenApi
    ├── ConfigureSwaggerOptions.cs
    └── ConfigureSwaggerUiOptions.cs
```

**Endpoint Structure**:

```csharp
public class CreateConnectionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/connections", async (
            CreateConnectionRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateConnectionCommand(
                request.Name,
                request.Host,
                request.Port,
                request.Username,
                request.Password
            );

            var result = await sender.Send(command, cancellationToken);

            return result.IsSuccess 
                ? Results.Created($"/connections/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateConnection")
        .WithTags("Connections")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}
```

## Data Flow

### Command Flow (Write Operations)

```
1. HTTP POST Request
   └─> API Endpoint
       └─> MediatR Send Command
           └─> Validation Behavior (FluentValidation)
               └─> Logging Behavior
                   └─> Command Handler
                       └─> Domain Entity (business logic)
                           └─> Repository (persistence)
                               └─> Unit of Work (commit)
                                   └─> Domain Events Published
                                       └─> Response
```

### Query Flow (Read Operations)

```
1. HTTP GET Request
   └─> API Endpoint
       └─> MediatR Send Query
           └─> Logging Behavior
               └─> Query Handler
                   └─> Repository
                       └─> EF Core Query
                           └─> Cache Interceptor Check
                               ├─> Cache Hit: Return cached data
                               └─> Cache Miss: Query database
                                   └─> Store in cache
                                       └─> Response
```

### Real-time Metrics Flow

```
1. Background Job (Hosted Service)
   └─> Timer Trigger (every 5 seconds)
       └─> Metrics Collector
           └─> Connect to Redis Servers
               └─> Gather Metrics (INFO command)
                   └─> Process Metrics
                       └─> SignalR Hub
                           └─> Broadcast to Connected Clients
                               └─> Angular Service receives update
                                   └─> Update UI
```

## Design Patterns

### 1. Repository Pattern

Abstracts data access logic from business logic.

```csharp
public interface IRedisConnectionRepository : IRepository<RedisConnection>
{
    Task<RedisConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<RedisConnection>> GetAllAsync(CancellationToken cancellationToken);
    void Add(RedisConnection connection);
    void Remove(RedisConnection connection);
}
```

### 2. Unit of Work Pattern

Manages transactions across multiple repositories.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 3. Result Pattern

Eliminates exceptions for flow control.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public Error Error { get; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}
```

### 4. Specification Pattern

Encapsulates query logic.

```csharp
// Used in value objects and domain logic
public class ConnectionPort
{
    private const int MinPort = 1;
    private const int MaxPort = 65535;

    public int Value { get; }

    private ConnectionPort(int value) => Value = value;

    public static Result<ConnectionPort> Create(int value)
    {
        if (value < MinPort || value > MaxPort)
            return Result.Failure<ConnectionPort>(
                DomainErrors.ConnectionPort.Invalid);

        return Result.Success(new ConnectionPort(value));
    }
}
```

### 5. Factory Pattern

Creates complex objects.

```csharp
public static class RedisConnection
{
    public static Result<AnonymousRedisConnection> CreateAnonymous(
        ConnectionName name,
        ConnectionHost host,
        ConnectionPort port)
    {
        // Factory logic
    }

    public static Result<RedisConnectionWithCredentials> CreateWithCredentials(
        ConnectionName name,
        ConnectionHost host,
        ConnectionPort port,
        ConnectionCredentials credentials)
    {
        // Factory logic
    }
}
```

### 6. Decorator Pattern

MediatR pipeline behaviors act as decorators.

```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Validation logic
        // Then call next()
        return await next();
    }
}
```

### 7. Strategy Pattern

Different connection types (Anonymous vs Credentials).

### 8. Observer Pattern

Domain events notify interested parties.

```csharp
public record ConnectionCreatedDomainEvent(Guid ConnectionId) : IDomainEvent;
```

## Real-time Communication

### SignalR Architecture

```
┌─────────────────────────────────────────────────┐
│         Angular SignalR Client                  │
│  ┌─────────────────────────────────────┐       │
│  │  @microsoft/signalr                  │       │
│  │  - HubConnection                     │       │
│  │  - Automatic Reconnection            │       │
│  └─────────────┬───────────────────────┘       │
└────────────────┼────────────────────────────────┘
                 │ WebSocket/SSE
                 ▼
┌─────────────────────────────────────────────────┐
│         RedisMetricsHub                         │
│  - OnConnectedAsync()                           │
│  - OnDisconnectedAsync()                        │
│  - BroadcastMetrics()                           │
└────────────┬────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────┐
│     RedisMetricsBackgroundJob                   │
│  - IHostedService                               │
│  - Timer-based execution (5 sec)                │
│  - Collects metrics from all connections        │
└─────────────────────────────────────────────────┘
```

### Hub Implementation

```csharp
public class RedisMetricsHub : Hub<IClientMethods>
{
    private readonly IHubSubscribersManager _subscribersManager;

    public override async Task OnConnectedAsync()
    {
        await _subscribersManager.AddSubscriberAsync(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _subscribersManager.RemoveSubscriberAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
```

## Caching Strategy

### Multi-level Caching

```
┌──────────────┐
│   Request    │
└──────┬───────┘
       │
       ▼
┌─────────────────────────┐
│  Caching Interceptor    │
└──────┬─────────┬────────┘
       │         │
       ▼         ▼
  ┌────────┐  ┌──────────┐
  │ Cache  │  │ Database │
  │  Hit   │  │  Query   │
  └───┬────┘  └────┬─────┘
      │            │
      │            ▼
      │      ┌──────────┐
      │      │  Cache   │
      │      │  Store   │
      │      └────┬─────┘
      │           │
      └───────┬───┘
              ▼
        ┌──────────┐
        │ Response │
        └──────────┘
```

### Cache Configuration

```csharp
public class CacheConfiguration
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
    public List<string> CachedTables { get; set; } = new();
}

// Usage in appsettings.json
{
  "Cache": {
    "Duration": "00:05:00",
    "CachedTables": [
      "AnonymousRedisConnection",
      "RedisConnectionWithCredentials"
    ]
  }
}
```

## Security

### Password Encryption

```csharp
public class PasswordEncryptor : IPasswordEncryptor, IPasswordDecrypt
{
    private readonly byte[] _securityKey;

    public string Encrypt(string password)
    {
        // PBKDF2 with HMACSHA256
        var hashed = KeyDerivation.Pbkdf2(
            password: password,
            salt: _securityKey,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8);

        return Convert.ToBase64String(hashed);
    }
}
```

### HTTPS Configuration

- Development: Self-signed certificates
- Production: Valid SSL certificates required

### CORS Policy

Configured for Angular development server and production domains.

## Database Design

### Entity Relationship Diagram

```
┌─────────────────────────────────┐
│     RedisConnection (Base)       │
│──────────────────────────────────│
│ Id: Guid (PK)                    │
│ Name: string                     │
│ Host: string                     │
│ Port: int                        │
│ IsAvailable: bool                │
│ LastCheckedAt: DateTime?         │
│ Type: string (Discriminator)     │
└─────────┬───────────────────────┘
          │
          ├─────────────────────────────────┐
          │                                 │
          ▼                                 ▼
┌─────────────────────────┐   ┌─────────────────────────────┐
│ AnonymousRedisConnection │   │RedisConnectionWithCredentials│
│─────────────────────────│   │─────────────────────────────│
│ (No additional fields)   │   │ Username: string            │
│                          │   │ EncryptedPassword: string   │
└─────────────────────────┘   └─────────────────────────────┘
```

### Table Per Hierarchy (TPH)

EF Core uses TPH inheritance strategy for connection types.

---

## Performance Considerations

1. **Caching**: Reduces database load for frequently accessed data
2. **Connection Pooling**: Reuses Redis connections efficiently
3. **Async/Await**: Non-blocking I/O operations
4. **Background Jobs**: Offloads metric collection from request pipeline
5. **Query Optimization**: EF Core tracking disabled for read-only queries

## Scalability

- **Horizontal Scaling**: Stateless API can run multiple instances
- **Database Scaling**: MySQL replication for read scaling
- **SignalR Scaling**: Can use Redis backplane for multi-server scenarios
- **Load Balancing**: Standard load balancer compatible

---

**Next**: [Setup Guide](SETUP.md) | [API Documentation](API.md)
