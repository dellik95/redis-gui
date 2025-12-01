# Setup and Installation Guide

## Table of Contents

- [System Requirements](#system-requirements)
- [Prerequisites Installation](#prerequisites-installation)
- [Project Setup](#project-setup)
- [Database Configuration](#database-configuration)
- [Development Environment](#development-environment)
- [Production Deployment](#production-deployment)
- [Configuration Options](#configuration-options)
- [Troubleshooting Setup](#troubleshooting-setup)

## System Requirements

### Minimum Requirements

- **OS**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 20.04+)
- **RAM**: 4 GB minimum, 8 GB recommended
- **Disk Space**: 2 GB free space
- **CPU**: 2 cores minimum, 4 cores recommended

### Software Requirements

- **.NET 9 SDK** - Latest version
- **Node.js** - Version 18.x or 20.x
- **npm** - Version 9.x or higher
- **Docker Desktop** - Latest stable version
- **Git** - For version control

## Prerequisites Installation

### 1. Install .NET 9 SDK

#### Windows

```powershell
# Download and install from Microsoft
winget install Microsoft.DotNet.SDK.9
```

Or download from: https://dotnet.microsoft.com/download/dotnet/9.0

#### macOS

```bash
# Using Homebrew
brew install dotnet@9
```

#### Linux (Ubuntu/Debian)

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

Verify installation:

```bash
dotnet --version
# Should show 9.0.x
```

### 2. Install Node.js and npm

#### Windows

```powershell
# Using winget
winget install OpenJS.NodeJS.LTS
```

Or download from: https://nodejs.org/

#### macOS

```bash
# Using Homebrew
brew install node@20
```

#### Linux

```bash
# Using NodeSource repository
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs
```

Verify installation:

```bash
node --version  # Should show v20.x.x or v18.x.x
npm --version   # Should show 9.x.x or higher
```

### 3. Install Docker Desktop

#### Windows

Download and install from: https://www.docker.com/products/docker-desktop

Or using winget:

```powershell
winget install Docker.DockerDesktop
```

#### macOS

```bash
brew install --cask docker
```

Or download from: https://www.docker.com/products/docker-desktop

#### Linux

```bash
# Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add user to docker group
sudo usermod -aG docker $USER

# Start Docker service
sudo systemctl enable docker
sudo systemctl start docker
```

Verify installation:

```bash
docker --version
docker-compose --version
```

### 4. Install Git

#### Windows

```powershell
winget install Git.Git
```

#### macOS

```bash
brew install git
```

#### Linux

```bash
sudo apt-get install git
```

### 5. Install Angular CLI (Optional)

```bash
npm install -g @angular/cli@19
```

Verify:

```bash
ng version
```

## Project Setup

### 1. Clone the Repository

```bash
git clone https://github.com/dellik95/redis-gui.git
cd redis-gui
```

### 2. Restore .NET Dependencies

```bash
# Restore all projects in the solution
dotnet restore
```

### 3. Install Frontend Dependencies

```bash
cd src/RedisGUI.Client
npm install
```

This will install all Angular dependencies including:
- Angular 19 framework
- Angular Material
- SignalR client
- Development tools

### 4. Trust Development Certificates

#### For ASP.NET Core HTTPS

```bash
dotnet dev-certs https --trust
```

#### For Angular HTTPS

The project uses `aspnetcore-https` script to generate certificates automatically on first run.

**Windows**: Certificates stored in `%APPDATA%\ASP.NET\https\`

**macOS/Linux**: Certificates stored in `~/.aspnet/https/`

## Database Configuration

### Option 1: Using Docker (Recommended for Development)

#### Start MySQL with Docker Compose

```bash
cd build/RedisGUI.Docker
docker-compose up -d
```

This starts:
- **MySQL**: Port 3306
  - Database: `redis-gui`
  - Username: `admin`
  - Password: `admin`
- **PHPMyAdmin**: http://localhost:8090
  - Server: `db`
  - Username: `admin`
  - Password: `admin`

#### Verify Docker Containers

```bash
docker-compose ps
```

You should see both `db` and `phpmyadmin` running.

#### Stop Docker Services

```bash
docker-compose down
```

#### Stop and Remove Data

```bash
docker-compose down -v
```

### Option 2: Using Local MySQL Installation

#### Install MySQL

**Windows**: Download from https://dev.mysql.com/downloads/mysql/

**macOS**:
```bash
brew install mysql
brew services start mysql
```

**Linux**:
```bash
sudo apt-get install mysql-server
sudo systemctl start mysql
```

#### Create Database

```sql
mysql -u root -p

CREATE DATABASE `redis-gui`;
CREATE USER 'admin'@'localhost' IDENTIFIED BY 'admin';
GRANT ALL PRIVILEGES ON `redis-gui`.* TO 'admin'@'localhost';
FLUSH PRIVILEGES;
```

### Option 3: Using In-Memory Database (Testing Only)

Update `appsettings.json`:

```json
{
  "Database": {
    "IsInMemory": true
  }
}
```

⚠️ **Warning**: Data will be lost when application stops.

### Database Migrations

#### Apply Migrations

```bash
cd src/RedisGUI.Server
dotnet ef database update
```

The application will also apply migrations automatically in Development mode on startup.

#### Create New Migration

```bash
dotnet ef migrations add MigrationName --project ../RedisGUI.Infrastructure
```

#### Remove Last Migration

```bash
dotnet ef migrations remove --project ../RedisGUI.Infrastructure
```

## Development Environment

### Configuration Files

#### 1. appsettings.json

Located at: `src/RedisGUI.Server/appsettings.json`

```json
{
  "Database": {
    "IsInMemory": false,
    "ConnectionString": "server=localhost;database=redis-gui;user=admin;password=admin;"
  },
  "Cache": {
    "Duration": "00:05:00",
    "CachedTables": [
      "AnonymousRedisConnection",
      "RedisConnectionWithCredentials"
    ]
  },
  "Cryptography": {
    "SecurityKey": "xD4lE9U7+AB87zy3MKqvQZ2KlC3Qrw/jH+oQ/VQX7bE="
  },
  "MetricsCollector": {
    "PoolingIntervalInSec": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### 2. appsettings.Development.json

Create for development-specific settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```

### Running the Application

#### Method 1: Using .NET CLI (Recommended)

```bash
cd src/RedisGUI.Server
dotnet run
```

This will:
1. Build the backend
2. Start the ASP.NET Core server on https://localhost:7234
3. Automatically start the Angular dev server on https://localhost:4200
4. Open the browser automatically

#### Method 2: Running Backend and Frontend Separately

**Terminal 1 - Backend**:
```bash
cd src/RedisGUI.Server
dotnet run --no-launch-profile
```

**Terminal 2 - Frontend**:
```bash
cd src/RedisGUI.Client
npm start
```

#### Method 3: Using Visual Studio 2022

1. Open `redis-gui.sln`
2. Set `RedisGUI.Server` as startup project
3. Press F5 or click "Start Debugging"

#### Method 4: Using Visual Studio Code

1. Open the project folder
2. Install recommended extensions:
   - C# Dev Kit
   - Angular Language Service
   - ESLint
3. Press F5 to start debugging

### Development URLs

- **Frontend**: https://localhost:4200
- **Backend API**: https://localhost:7234
- **Swagger UI**: https://localhost:7234/swagger
- **SignalR Hub**: wss://localhost:7234/hub/metrics
- **PHPMyAdmin**: http://localhost:8090

### Hot Reload

Both .NET and Angular support hot reload:

- **.NET**: Changes to C# files automatically reload
- **Angular**: Changes to TypeScript/HTML/CSS files automatically refresh

## Production Deployment

### Build for Production

#### 1. Build Backend

```bash
dotnet publish src/RedisGUI.Server/RedisGUI.Server.csproj \
  -c Release \
  -o ./publish
```

#### 2. Build Frontend

```bash
cd src/RedisGUI.Client
npm run build
```

The Angular build output will be automatically included in the ASP.NET Core publish output.

### Configuration for Production

#### 1. Create appsettings.Production.json

```json
{
  "Database": {
    "IsInMemory": false,
    "ConnectionString": "server=your-production-server;database=redis-gui;user=your-user;password=your-secure-password;SslMode=Required;"
  },
  "Cryptography": {
    "SecurityKey": "GENERATE-A-NEW-SECURE-KEY-HERE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "yourdomain.com"
}
```

⚠️ **Important**: Generate a new security key for production:

```bash
dotnet user-secrets init
dotnet user-secrets set "Cryptography:SecurityKey" "$(openssl rand -base64 32)"
```

#### 2. Set Environment Variables

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="https://+:443;http://+:80"
```

### Running in Production

#### Using systemd (Linux)

Create service file: `/etc/systemd/system/redis-gui.service`

```ini
[Unit]
Description=Redis GUI Application
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/redis-gui
ExecStart=/usr/bin/dotnet /var/www/redis-gui/RedisGUI.Server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=redis-gui
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl enable redis-gui
sudo systemctl start redis-gui
sudo systemctl status redis-gui
```

#### Using Docker

Create `Dockerfile` (if not exists):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/RedisGUI.Server/RedisGUI.Server.csproj", "RedisGUI.Server/"]
RUN dotnet restore "RedisGUI.Server/RedisGUI.Server.csproj"
COPY . .
WORKDIR "/src/RedisGUI.Server"
RUN dotnet build "RedisGUI.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedisGUI.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedisGUI.Server.dll"]
```

Build and run:

```bash
docker build -t redis-gui:latest .
docker run -d -p 8080:80 --name redis-gui redis-gui:latest
```

## Configuration Options

### Database Configuration

```json
{
  "Database": {
    "IsInMemory": false,           // Use in-memory database (testing only)
    "ConnectionStringMSQL": "",    // Alternative MySQL connection string
    "ConnectionString": ""         // Primary connection string
  }
}
```

### Cache Configuration

```json
{
  "Cache": {
    "Duration": "00:05:00",        // Cache duration (hh:mm:ss)
    "CachedTables": [              // Tables to cache
      "AnonymousRedisConnection",
      "RedisConnectionWithCredentials"
    ]
  }
}
```

### Cryptography Configuration

```json
{
  "Cryptography": {
    "SecurityKey": "base64-encoded-key"  // 32-byte key for password encryption
  }
}
```

### Metrics Collector Configuration

```json
{
  "MetricsCollector": {
    "PoolingIntervalInSec": 5      // Interval for collecting Redis metrics
  }
}
```

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```

## Environment-Specific Configuration

### Development

- Detailed logging enabled
- Swagger UI available
- Auto-migration enabled
- CORS relaxed for local development

### Staging

- Moderate logging
- Swagger UI available
- Manual migrations
- CORS configured for staging domain

### Production

- Minimal logging (Warning and Error only)
- Swagger UI disabled
- Manual migrations required
- Strict CORS policy
- HTTPS required

## IDE Setup

### Visual Studio 2022

1. Install Visual Studio 2022 (17.8 or later)
2. Install workloads:
   - ASP.NET and web development
   - .NET desktop development
3. Install extensions:
   - ReSharper (optional)
   - Web Essentials (optional)

### Visual Studio Code

Recommended extensions:

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
    "ms-dotnettools.csharp",
    "angular.ng-template",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "ms-azuretools.vscode-docker",
    "humao.rest-client"
  ]
}
```

### JetBrains Rider

1. Install Rider 2024.1 or later
2. Open solution file
3. All required plugins are detected automatically

## Troubleshooting Setup

### Common Issues

#### 1. Port Already in Use

**Error**: "Failed to bind to address https://127.0.0.1:7234"

**Solution**:
```bash
# Windows
netstat -ano | findstr :7234
taskkill /PID <PID> /F

# macOS/Linux
lsof -ti:7234 | xargs kill -9
```

#### 2. Database Connection Failed

**Error**: "Unable to connect to MySQL server"

**Solution**:
- Verify Docker containers are running: `docker-compose ps`
- Check connection string in `appsettings.json`
- Test MySQL connection: `mysql -h localhost -u admin -p`

#### 3. npm Install Fails

**Error**: "EACCES: permission denied"

**Solution**:
```bash
# Fix npm permissions (macOS/Linux)
sudo chown -R $USER:$GROUP ~/.npm
sudo chown -R $USER:$GROUP ~/.config

# Or use npx/npm with --legacy-peer-deps
npm install --legacy-peer-deps
```

#### 4. Certificate Errors

**Error**: "The SSL connection could not be established"

**Solution**:
```bash
# Trust development certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# For Angular
cd src/RedisGUI.Client
node aspnetcore-https
```

#### 5. Angular Compilation Errors

**Error**: TypeScript compilation errors

**Solution**:
```bash
cd src/RedisGUI.Client
rm -rf node_modules package-lock.json
npm cache clean --force
npm install
```

### Getting Help

If you encounter issues not covered here:

1. Check [Troubleshooting Guide](TROUBLESHOOTING.md)
2. Search [GitHub Issues](https://github.com/dellik95/redis-gui/issues)
3. Create a [new issue](https://github.com/dellik95/redis-gui/issues/new)

---

**Next**: [Architecture Guide](ARCHITECTURE.md) | [API Documentation](API.md)
