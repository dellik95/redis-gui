# Redis GUI

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-19-DD0031?logo=angular)](https://angular.io/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A modern, feature-rich graphical user interface for Redis built with .NET 9 and Angular 19. This application provides an intuitive interface for managing Redis connections, viewing real-time metrics, and performing CRUD operations on Redis data.

## 🚀 Features

- **Connection Management**: Create, edit, and delete Redis connections with support for both anonymous and authenticated connections
- **Real-time Metrics**: Monitor Redis server metrics with live updates via SignalR
- **Data Operations**: Browse, create, update, and delete Redis keys and values
- **Multiple Database Support**: Store connection configurations in MySQL or in-memory database
- **Secure Credentials**: Encrypted password storage for Redis connections
- **RESTful API**: Well-documented API with Swagger/OpenAPI support
- **Clean Architecture**: Built following CQRS, DDD, and Clean Architecture principles
- **Type Safety**: Full TypeScript support in the Angular frontend

## 📋 Table of Contents

- [Tech Stack](#-tech-stack)
- [Prerequisites](#-prerequisites)
- [Quick Start](#-quick-start)
- [Documentation](#-documentation)
- [Project Structure](#-project-structure)
- [Contributing](#-contributing)
- [License](#-license)

## 🛠 Tech Stack

### Backend
- **.NET 9** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 9** - ORM with MySQL provider
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **SignalR** - Real-time communication
- **NRedisStack** - Redis client library
- **Swashbuckle** - API documentation

### Frontend
- **Angular 19** - Modern web framework
- **Angular Material** - UI component library
- **TypeScript 5.6** - Type-safe JavaScript
- **RxJS** - Reactive programming
- **SignalR Client** - Real-time updates

### Infrastructure
- **MySQL** - Primary database
- **Docker & Docker Compose** - Containerization
- **PHPMyAdmin** - Database management

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for running MySQL)
- [Redis Server](https://redis.io/download) (for testing connections)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/dellik95/redis-gui.git
cd redis-gui
```

### 2. Start Database Services

```bash
cd build/RedisGUI.Docker
docker-compose up -d
```

This will start:
- MySQL database on port 3306
- PHPMyAdmin on http://localhost:8090

### 3. Configure Application

Update the connection string in `src/RedisGUI.Server/appsettings.json` if needed:

```json
{
  "Database": {
    "IsInMemory": false,
    "ConnectionString": "server=localhost;database=redis-gui;user=admin;password=admin;"
  }
}
```

### 4. Run the Application

```bash
cd src/RedisGUI.Server
dotnet run
```

The application will start:
- Backend API: https://localhost:7234
- Swagger UI: https://localhost:7234/swagger
- Angular Dev Server: https://localhost:4200 (auto-launched)

### 5. Access the Application

Open your browser and navigate to:
- **Application**: https://localhost:4200
- **API Documentation**: https://localhost:7234/swagger

## 📚 Documentation

For detailed information, please refer to the following documentation:

- [**Architecture Guide**](docs/ARCHITECTURE.md) - System design, patterns, and architecture diagrams
- [**Setup Guide**](docs/SETUP.md) - Detailed installation and configuration instructions
- [**API Documentation**](docs/API.md) - Complete API reference and examples
- [**Contributing Guide**](docs/CONTRIBUTING.md) - Development workflow and coding standards
- [**Deployment Guide**](docs/DEPLOYMENT.md) - Production deployment instructions
- [**Troubleshooting**](docs/TROUBLESHOOTING.md) - Common issues and solutions

## 📁 Project Structure

```
redis-gui/
├── src/
│   ├── RedisGUI.Domain/          # Domain entities, value objects, and interfaces
│   ├── RedisGUI.Application/     # Application logic, CQRS handlers, and DTOs
│   ├── RedisGUI.Infrastructure/  # Data access, external services, and SignalR
│   ├── RedisGUI.Server/          # API endpoints and Web host
│   └── RedisGUI.Client/          # Angular frontend application
├── build/
│   └── RedisGUI.Docker/          # Docker configuration files
└── docs/                          # Documentation files
```

### Layer Responsibilities

- **Domain**: Core business logic and entities (no dependencies)
- **Application**: Use cases, CQRS handlers, validation
- **Infrastructure**: Database, Redis, caching, external services
- **Server**: API endpoints, dependency injection configuration
- **Client**: Angular UI components and services

## 🏗 Architecture Highlights

- **Clean Architecture**: Separation of concerns with dependency inversion
- **CQRS Pattern**: Command Query Responsibility Segregation with MediatR
- **Domain-Driven Design**: Rich domain models with business logic
- **Repository Pattern**: Data access abstraction
- **Unit of Work**: Transaction management
- **Domain Events**: Event-driven architecture
- **Background Jobs**: Periodic Redis metrics collection
- **Caching Strategy**: Query result caching with interceptors

## 🔧 Development

### Build the Solution

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Database Migrations

```bash
cd src/RedisGUI.Server
dotnet ef migrations add MigrationName --project ../RedisGUI.Infrastructure
dotnet ef database update
```

### Frontend Development

```bash
cd src/RedisGUI.Client
npm install
npm start
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](docs/CONTRIBUTING.md) for details on:

- Code of Conduct
- Development workflow
- Coding standards
- Pull request process
- Testing guidelines

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Dellik95** - [GitHub](https://github.com/dellik95)

## 🙏 Acknowledgments

- Built with ❤️ using .NET and Angular
- Inspired by modern Redis management tools
- Thanks to all contributors and the open-source community

## 📞 Support

If you encounter any issues or have questions:

1. Check the [Troubleshooting Guide](docs/TROUBLESHOOTING.md)
2. Search [existing issues](https://github.com/dellik95/redis-gui/issues)
3. Open a [new issue](https://github.com/dellik95/redis-gui/issues/new)

---

**Note**: This project is currently under active development. Features and documentation may change.
