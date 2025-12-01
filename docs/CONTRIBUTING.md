# Contributing to Redis GUI

Thank you for your interest in contributing to Redis GUI! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Project Structure](#project-structure)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Documentation](#documentation)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please be respectful and constructive in your interactions.

### Expected Behavior

- Use welcoming and inclusive language
- Be respectful of differing viewpoints
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

### Unacceptable Behavior

- Harassment, discrimination, or hate speech
- Trolling, insulting comments, or personal attacks
- Public or private harassment
- Publishing others' private information
- Other conduct that could be considered inappropriate

## Getting Started

### Prerequisites

Before contributing, ensure you have:

1. Read the [Setup Guide](SETUP.md)
2. Set up your development environment
3. Familiarized yourself with the [Architecture](ARCHITECTURE.md)
4. Read this contributing guide

### Finding Issues to Work On

1. **Good First Issues**: Look for issues labeled `good first issue`
2. **Help Wanted**: Check issues labeled `help wanted`
3. **Bugs**: Look for issues labeled `bug`
4. **Feature Requests**: Check issues labeled `enhancement`

### Claiming an Issue

1. Comment on the issue saying you'd like to work on it
2. Wait for a maintainer to assign it to you
3. Start working on it within 7 days or it may be reassigned

## Development Workflow

### 1. Fork the Repository

```bash
# Fork on GitHub, then clone your fork
git clone https://github.com/YOUR-USERNAME/redis-gui.git
cd redis-gui

# Add upstream remote
git remote add upstream https://github.com/dellik95/redis-gui.git
```

### 2. Create a Branch

```bash
# Update your main branch
git checkout main
git pull upstream main

# Create a feature branch
git checkout -b feature/your-feature-name

# Or for bug fixes
git checkout -b fix/bug-description
```

**Branch Naming Convention**:
- `feature/feature-name` - New features
- `fix/bug-name` - Bug fixes
- `docs/description` - Documentation updates
- `refactor/description` - Code refactoring
- `test/description` - Test additions or updates

### 3. Make Your Changes

Follow the [Coding Standards](#coding-standards) section below.

### 4. Test Your Changes

```bash
# Run backend tests
dotnet test

# Run frontend tests
cd src/RedisGUI.Client
npm test

# Run build to ensure no compilation errors
dotnet build
```

### 5. Commit Your Changes

Follow the [Commit Message Guidelines](#commit-message-guidelines).

```bash
git add .
git commit -m "feat: add new feature description"
```

### 6. Push to Your Fork

```bash
git push origin feature/your-feature-name
```

### 7. Create a Pull Request

1. Go to the original repository
2. Click "New Pull Request"
3. Select your fork and branch
4. Fill in the PR template
5. Submit the pull request

## Coding Standards

### C# / .NET Backend

#### General Guidelines

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods small and focused (single responsibility)
- Add XML documentation comments for public APIs
- Use nullable reference types
- Prefer `async/await` for asynchronous operations

#### Naming Conventions

```csharp
// Classes and Interfaces - PascalCase
public class ConnectionService { }
public interface IConnectionService { }

// Methods - PascalCase
public async Task<Result> CreateConnectionAsync() { }

// Properties - PascalCase
public string ConnectionName { get; set; }

// Private fields - _camelCase
private readonly IRepository _repository;

// Local variables and parameters - camelCase
public void DoSomething(string connectionName)
{
    var isValid = true;
}

// Constants - PascalCase
public const int MaxRetryAttempts = 3;
```

#### Code Organization

```csharp
// 1. Using statements (sorted)
using System;
using System.Threading.Tasks;
using RedisGUI.Domain.Abstraction;

// 2. Namespace
namespace RedisGUI.Application.Connections;

// 3. Class documentation
/// <summary>
/// Handles creation of Redis connections
/// </summary>
public class CreateConnectionCommandHandler
{
    // 4. Private fields
    private readonly IRepository _repository;
    
    // 5. Constructor
    public CreateConnectionCommandHandler(IRepository repository)
    {
        _repository = repository;
    }
    
    // 6. Public methods
    public async Task<Result> Handle()
    {
        // Implementation
    }
    
    // 7. Private methods
    private bool ValidateConnection()
    {
        // Implementation
    }
}
```

#### CQRS Handlers

```csharp
// Command Handler Example
public class CreateConnectionCommandHandler 
    : ICommandHandler<CreateConnectionCommand, Guid>
{
    private readonly IRedisConnectionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateConnectionCommandHandler(
        IRedisConnectionRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateConnectionCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Create domain entity
        var connection = RedisConnection.Create(
            command.Name,
            command.Host,
            command.Port);

        if (connection.IsFailure)
        {
            return Result.Failure<Guid>(connection.Error);
        }

        // 2. Add to repository
        _repository.Add(connection.Value);

        // 3. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Return result
        return Result.Success(connection.Value.Id);
    }
}
```

#### Validation

Use FluentValidation:

```csharp
public class CreateConnectionCommandValidator 
    : AbstractValidator<CreateConnectionCommand>
{
    public CreateConnectionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Connection name is required")
            .Length(3, 100)
            .WithMessage("Connection name must be between 3 and 100 characters");

        RuleFor(x => x.Host)
            .NotEmpty()
            .WithMessage("Host is required");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535)
            .WithMessage("Port must be between 1 and 65535");
    }
}
```

#### Error Handling

Use the Result pattern:

```csharp
// Good
public Result<Connection> GetConnection(Guid id)
{
    var connection = _repository.GetById(id);
    
    if (connection is null)
    {
        return Result.Failure<Connection>(
            DomainErrors.Connection.NotFound(id));
    }
    
    return Result.Success(connection);
}

// Avoid throwing exceptions for business logic
// Bad
public Connection GetConnection(Guid id)
{
    var connection = _repository.GetById(id);
    
    if (connection is null)
    {
        throw new NotFoundException($"Connection {id} not found");
    }
    
    return connection;
}
```

### TypeScript / Angular Frontend

#### General Guidelines

- Follow [Angular Style Guide](https://angular.io/guide/styleguide)
- Use TypeScript strict mode
- Prefer reactive programming with RxJS
- Use Angular Material components
- Keep components focused and small

#### Naming Conventions

```typescript
// Components - PascalCase with suffix
export class ConnectionListComponent { }

// Services - PascalCase with suffix
export class ConnectionService { }

// Interfaces - PascalCase with prefix I
export interface IConnection { }

// Types - PascalCase
export type ConnectionStatus = 'active' | 'inactive';

// Variables and functions - camelCase
const connectionName = 'Redis';
function getConnection() { }

// Constants - UPPER_SNAKE_CASE
const MAX_RETRY_ATTEMPTS = 3;
```

#### Component Structure

```typescript
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-connection-list',
  templateUrl: './connection-list.component.html',
  styleUrls: ['./connection-list.component.scss']
})
export class ConnectionListComponent implements OnInit, OnDestroy {
  // 1. Public properties
  connections: Connection[] = [];
  isLoading = false;

  // 2. Private properties
  private destroy$ = new Subject<void>();

  // 3. Constructor with dependency injection
  constructor(
    private connectionService: ConnectionService,
    private router: Router
  ) {}

  // 4. Lifecycle hooks
  ngOnInit(): void {
    this.loadConnections();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // 5. Public methods
  loadConnections(): void {
    this.isLoading = true;
    this.connectionService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (connections) => {
          this.connections = connections;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load connections', error);
          this.isLoading = false;
        }
      });
  }

  // 6. Private methods
  private navigateToDetails(id: string): void {
    this.router.navigate(['/connections', id]);
  }
}
```

#### RxJS Best Practices

```typescript
// Always unsubscribe
export class MyComponent implements OnDestroy {
  private destroy$ = new Subject<void>();

  ngOnInit() {
    this.service.getData()
      .pipe(takeUntil(this.destroy$))
      .subscribe();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

// Use async pipe in templates
// Good
<div *ngFor="let item of items$ | async">{{ item.name }}</div>

// Avoid manual subscriptions when possible
```

#### Service Pattern

```typescript
@Injectable({
  providedIn: 'root'
})
export class ConnectionService {
  private readonly apiUrl = `${environment.apiUrl}/connections`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Connection[]> {
    return this.http.get<Connection[]>(this.apiUrl);
  }

  getById(id: string): Observable<Connection> {
    return this.http.get<Connection>(`${this.apiUrl}/${id}`);
  }

  create(connection: CreateConnectionRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, connection);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
```

### Code Formatting

#### .NET

Use `.editorconfig` settings (already configured in the project).

Format code before committing:

```bash
dotnet format
```

#### Angular

Use Prettier (configuration in `.prettierrc`).

Format code:

```bash
npm run format
```

## Project Structure

### Backend Structure

```
src/
├── RedisGUI.Domain/           # Domain layer (no dependencies)
│   ├── Entities/             # Domain entities
│   ├── ValueObjects/         # Value objects
│   ├── Events/               # Domain events
│   └── Abstractions/         # Interfaces
│
├── RedisGUI.Application/      # Application layer
│   ├── Commands/             # Write operations
│   ├── Queries/              # Read operations
│   ├── Behaviors/            # MediatR behaviors
│   └── Abstractions/         # CQRS interfaces
│
├── RedisGUI.Infrastructure/   # Infrastructure layer
│   ├── Persistence/          # EF Core, repositories
│   ├── Redis/                # Redis client
│   ├── SignalR/              # Real-time communication
│   └── BackgroundJobs/       # Hosted services
│
└── RedisGUI.Server/          # Presentation layer
    ├── Endpoints/            # API endpoints
    └── Extensions/           # Dependency injection
```

### Frontend Structure

```
src/RedisGUI.Client/
├── src/
│   ├── app/
│   │   ├── core/             # Singleton services, guards
│   │   ├── shared/           # Shared components, directives
│   │   ├── features/         # Feature modules
│   │   │   ├── connections/
│   │   │   └── redis/
│   │   └── models/           # TypeScript interfaces
│   ├── assets/               # Static assets
│   └── environments/         # Environment configs
```

## Testing Guidelines

### Backend Tests

#### Unit Tests

```csharp
public class CreateConnectionCommandHandlerTests
{
    private readonly Mock<IRedisConnectionRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateConnectionCommandHandler _handler;

    public CreateConnectionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRedisConnectionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateConnectionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResult()
    {
        // Arrange
        var command = new CreateConnectionCommand(
            "Test",
            "localhost",
            6379,
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.Add(It.IsAny<RedisConnection>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Frontend Tests

#### Component Tests

```typescript
describe('ConnectionListComponent', () => {
  let component: ConnectionListComponent;
  let fixture: ComponentFixture<ConnectionListComponent>;
  let service: jasmine.SpyObj<ConnectionService>;

  beforeEach(async () => {
    const serviceSpy = jasmine.createSpyObj('ConnectionService', ['getAll']);

    await TestBed.configureTestingModule({
      declarations: [ConnectionListComponent],
      providers: [
        { provide: ConnectionService, useValue: serviceSpy }
      ]
    }).compileComponents();

    service = TestBed.inject(ConnectionService) as jasmine.SpyObj<ConnectionService>;
    fixture = TestBed.createComponent(ConnectionListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load connections on init', () => {
    const mockConnections = [
      { id: '1', name: 'Test', host: 'localhost', port: 6379 }
    ];
    service.getAll.and.returnValue(of(mockConnections));

    component.ngOnInit();

    expect(component.connections).toEqual(mockConnections);
    expect(service.getAll).toHaveBeenCalled();
  });
});
```

### Test Coverage

- Aim for at least 80% code coverage
- All business logic must be tested
- Test edge cases and error scenarios
- Mock external dependencies

## Pull Request Process

### Before Creating a PR

1. ✅ All tests pass
2. ✅ Code follows style guidelines
3. ✅ No compilation warnings
4. ✅ Documentation updated (if needed)
5. ✅ Commit messages follow guidelines

### PR Template

When creating a PR, include:

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
How has this been tested?

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings
- [ ] Tests added/updated
- [ ] All tests pass
```

### Review Process

1. A maintainer will review your PR
2. Address any feedback
3. Once approved, a maintainer will merge

### After PR is Merged

1. Delete your branch
2. Update your local main branch
3. Thank you for contributing! 🎉

## Commit Message Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/).

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `ci`: CI/CD changes

### Examples

```bash
# Feature
git commit -m "feat(connections): add connection pooling"

# Bug fix
git commit -m "fix(redis): resolve memory leak in connection pool"

# Documentation
git commit -m "docs(api): update API documentation for v2"

# Breaking change
git commit -m "feat(api)!: change connection API response format

BREAKING CHANGE: Response format changed from array to paginated object"
```

### Scope

Use the module or feature name:
- `connections`
- `redis`
- `metrics`
- `api`
- `ui`

## Documentation

### Code Documentation

#### C# XML Comments

```csharp
/// <summary>
/// Creates a new Redis connection
/// </summary>
/// <param name="name">Connection name</param>
/// <param name="host">Redis host address</param>
/// <param name="port">Redis port number</param>
/// <returns>Result containing the created connection ID</returns>
public async Task<Result<Guid>> CreateConnectionAsync(
    string name,
    string host,
    int port)
{
    // Implementation
}
```

#### TypeScript JSDoc

```typescript
/**
 * Retrieves all Redis connections
 * @returns Observable of connection array
 */
getAll(): Observable<Connection[]> {
  return this.http.get<Connection[]>(this.apiUrl);
}
```

### Updating Documentation

If your changes affect:
- **API**: Update [API.md](API.md)
- **Architecture**: Update [ARCHITECTURE.md](ARCHITECTURE.md)
- **Setup**: Update [SETUP.md](SETUP.md)
- **README**: Update main [README.md](../README.md)

## Questions?

If you have questions:
1. Check existing documentation
2. Search closed issues
3. Ask in a new issue with label `question`

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (MIT License).

---

**Thank you for contributing to Redis GUI!** 🚀
