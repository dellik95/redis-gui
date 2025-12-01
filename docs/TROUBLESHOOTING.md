# Troubleshooting Guide

## Table of Contents

- [Common Issues](#common-issues)
  - [Installation and Setup](#installation-and-setup)
  - [Database Issues](#database-issues)
  - [Application Startup](#application-startup)
  - [Frontend Issues](#frontend-issues)
  - [Redis Connection Issues](#redis-connection-issues)
  - [SignalR Issues](#signalr-issues)
  - [Performance Issues](#performance-issues)
- [Debugging](#debugging)
- [Logs and Diagnostics](#logs-and-diagnostics)
- [Getting Help](#getting-help)

## Common Issues

### Installation and Setup

#### Issue: .NET SDK Not Found

**Error**:
```
The command 'dotnet' is either misspelled or could not be found.
```

**Solution**:

1. Verify .NET is installed:
   ```bash
   dotnet --version
   ```

2. Install .NET 9 SDK:
   - Windows: https://dotnet.microsoft.com/download/dotnet/9.0
   - macOS: `brew install dotnet@9`
   - Linux: See [Setup Guide](SETUP.md#prerequisites-installation)

3. Add to PATH if needed:
   ```bash
   # Linux/macOS
   export PATH="$PATH:$HOME/.dotnet"
   
   # Windows (PowerShell)
   $env:Path += ";C:\Program Files\dotnet"
   ```

---

#### Issue: Node.js Version Mismatch

**Error**:
```
The engine "node" is incompatible with this module. Expected version ">=18.0.0"
```

**Solution**:

1. Check Node version:
   ```bash
   node --version
   ```

2. Install Node.js 18+ or 20:
   ```bash
   # Using nvm (recommended)
   nvm install 20
   nvm use 20
   
   # Or download from https://nodejs.org/
   ```

3. Clear npm cache:
   ```bash
   npm cache clean --force
   cd src/RedisGUI.Client
   rm -rf node_modules package-lock.json
   npm install
   ```

---

#### Issue: Port Already in Use

**Error**:
```
Failed to bind to address https://127.0.0.1:7234: address already in use
```

**Solution**:

**Windows**:
```powershell
# Find process using port 7234
netstat -ano | findstr :7234

# Kill process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

**macOS/Linux**:
```bash
# Find process using port 7234
lsof -ti:7234

# Kill process
lsof -ti:7234 | xargs kill -9
```

**Alternative**: Change port in `launchSettings.json`:
```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:7235"
    }
  }
}
```

---

#### Issue: SSL Certificate Trust Issues

**Error**:
```
The SSL connection could not be established
```

**Solution**:

**Windows/macOS**:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

**Linux**:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --export-path ~/.aspnet/https/aspnetapp.pfx --password password
```

**Angular HTTPS**:
```bash
cd src/RedisGUI.Client
node aspnetcore-https
```

---

### Database Issues

#### Issue: Cannot Connect to MySQL

**Error**:
```
Unable to connect to any of the specified MySQL hosts
```

**Solutions**:

1. **Verify Docker containers are running**:
   ```bash
   cd build/RedisGUI.Docker
   docker-compose ps
   
   # If not running
   docker-compose up -d
   ```

2. **Check MySQL is accessible**:
   ```bash
   mysql -h localhost -u admin -p
   # Password: admin
   ```

3. **Verify connection string in `appsettings.json`**:
   ```json
   {
     "Database": {
       "ConnectionString": "server=localhost;database=redis-gui;user=admin;password=admin;"
     }
   }
   ```

4. **Check MySQL logs**:
   ```bash
   docker logs redisguidocker-db-1
   ```

5. **Recreate database container**:
   ```bash
   docker-compose down
   docker-compose up -d
   ```

---

#### Issue: Migration Failed

**Error**:
```
Failed executing DbCommand
```

**Solution**:

1. **Drop and recreate database**:
   ```bash
   mysql -h localhost -u admin -p
   DROP DATABASE IF EXISTS `redis-gui`;
   CREATE DATABASE `redis-gui`;
   exit;
   ```

2. **Reapply migrations**:
   ```bash
   cd src/RedisGUI.Server
   dotnet ef database update
   ```

3. **If migration is corrupted, remove and recreate**:
   ```bash
   # Remove last migration
   dotnet ef migrations remove --project ../RedisGUI.Infrastructure
   
   # Create new migration
   dotnet ef migrations add InitialCreate --project ../RedisGUI.Infrastructure
   
   # Apply migration
   dotnet ef database update
   ```

---

#### Issue: Database Connection Pool Exhausted

**Error**:
```
Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool.
```

**Solution**:

1. **Increase connection pool size** in connection string:
   ```json
   {
     "Database": {
       "ConnectionString": "server=localhost;database=redis-gui;user=admin;password=admin;Max Pool Size=200;"
     }
   }
   ```

2. **Check for connection leaks** - ensure `using` statements or proper disposal

3. **Restart application**:
   ```bash
   sudo systemctl restart redis-gui
   ```

---

### Application Startup

#### Issue: Application Crashes on Startup

**Error**:
```
Application startup exception
```

**Diagnosis**:

1. **Check logs**:
   ```bash
   # Linux
   sudo journalctl -u redis-gui -n 100
   
   # Docker
   docker logs redis-gui
   
   # Windows
   # Check Event Viewer > Application Logs
   ```

2. **Run in debug mode**:
   ```bash
   cd src/RedisGUI.Server
   ASPNETCORE_ENVIRONMENT=Development dotnet run
   ```

3. **Common causes**:
   - Missing configuration values
   - Database unavailable
   - Invalid security key
   - Port conflict

**Solutions**:

- Verify all required configuration in `appsettings.json`
- Ensure database is running
- Generate new security key if invalid
- Check port availability

---

#### Issue: Dependency Injection Error

**Error**:
```
Unable to resolve service for type 'IRepository'
```

**Solution**:

1. **Check service registration** in `Program.cs` or `DependencyInjection.cs`

2. **Ensure all dependencies are registered**:
   ```csharp
   builder.Services.AddApplication();
   builder.Services.AddInfrastructure(builder.Configuration);
   ```

3. **Rebuild solution**:
   ```bash
   dotnet clean
   dotnet build
   ```

---

### Frontend Issues

#### Issue: Angular Compilation Errors

**Error**:
```
Module not found: Error: Can't resolve '@angular/...'
```

**Solution**:

1. **Clean install**:
   ```bash
   cd src/RedisGUI.Client
   rm -rf node_modules package-lock.json
   npm cache clean --force
   npm install
   ```

2. **Check Node.js version**:
   ```bash
   node --version  # Should be 18.x or 20.x
   ```

3. **Update Angular CLI**:
   ```bash
   npm uninstall -g @angular/cli
   npm install -g @angular/cli@19
   ```

---

#### Issue: CORS Error

**Error**:
```
Access to XMLHttpRequest has been blocked by CORS policy
```

**Solution**:

1. **Verify backend CORS configuration** in `Program.cs`:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(builder =>
       {
           builder.WithOrigins("https://localhost:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
       });
   });
   ```

2. **Check Angular proxy configuration** in `proxy.conf.json`

3. **Verify API URLs** in Angular environment files

---

#### Issue: SignalR Connection Failed

**Error**:
```
Error: Failed to start the connection: Error: WebSocket failed to connect
```

**Solution**:

1. **Check WebSocket support**:
   - Ensure reverse proxy (Nginx/IIS) supports WebSockets
   - Verify firewall allows WebSocket connections

2. **Nginx configuration** for WebSockets:
   ```nginx
   location /hub/ {
       proxy_pass http://localhost:5000;
       proxy_http_version 1.1;
       proxy_set_header Upgrade $http_upgrade;
       proxy_set_header Connection "upgrade";
   }
   ```

3. **Check SignalR endpoint**:
   ```typescript
   // Verify URL is correct
   const connection = new signalR.HubConnectionBuilder()
     .withUrl('https://localhost:7234/hub/metrics')
     .build();
   ```

4. **Enable detailed SignalR logging**:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.AspNetCore.SignalR": "Debug",
         "Microsoft.AspNetCore.Http.Connections": "Debug"
       }
     }
   }
   ```

---

### Redis Connection Issues

#### Issue: Cannot Connect to Redis Server

**Error**:
```
It was not possible to connect to the redis server(s)
```

**Solutions**:

1. **Verify Redis is running**:
   ```bash
   redis-cli ping
   # Should return PONG
   ```

2. **Check Redis configuration**:
   ```bash
   # Check if Redis accepts connections
   redis-cli info server
   ```

3. **Test connection manually**:
   ```bash
   redis-cli -h localhost -p 6379 ping
   ```

4. **Check firewall rules**:
   ```bash
   # Allow Redis port
   sudo ufw allow 6379/tcp
   ```

5. **Verify connection details** in application

---

#### Issue: Redis Authentication Failed

**Error**:
```
NOAUTH Authentication required
```

**Solution**:

1. **Set password in Redis**:
   ```bash
   redis-cli
   CONFIG SET requirepass "your-password"
   ```

2. **Use password in connection**:
   - Enter password when creating connection in Redis GUI

3. **Test with redis-cli**:
   ```bash
   redis-cli -h localhost -p 6379 -a your-password ping
   ```

---

#### Issue: Redis Key Not Found

**Error**:
```
Key 'mykey' not found in database 0
```

**Solution**:

1. **Verify database number**:
   ```bash
   redis-cli -n 0 KEYS *
   ```

2. **Check if key exists**:
   ```bash
   redis-cli EXISTS mykey
   # Returns 1 if exists, 0 if not
   ```

3. **List all keys**:
   ```bash
   redis-cli KEYS *
   ```

4. **Check TTL**:
   ```bash
   redis-cli TTL mykey
   # -2 means key doesn't exist
   # -1 means no expiration
   # positive number means seconds until expiration
   ```

---

### SignalR Issues

#### Issue: Metrics Not Updating

**Symptoms**:
- Dashboard shows old/stale metrics
- Real-time updates not working

**Solutions**:

1. **Check SignalR connection status** in browser console:
   ```javascript
   // Should see: SignalR connection established
   ```

2. **Verify background job is running**:
   ```bash
   # Check logs for metrics collection
   sudo journalctl -u redis-gui | grep "Metrics"
   ```

3. **Check metrics collection interval** in `appsettings.json`:
   ```json
   {
     "MetricsCollector": {
       "PoolingIntervalInSec": 5
     }
   }
   ```

4. **Restart application**:
   ```bash
   sudo systemctl restart redis-gui
   ```

---

#### Issue: SignalR Disconnects Frequently

**Solution**:

1. **Increase timeout** in SignalR configuration:
   ```csharp
   builder.Services.AddSignalR(options =>
   {
       options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
       options.KeepAliveInterval = TimeSpan.FromSeconds(15);
   });
   ```

2. **Enable automatic reconnection** in client:
   ```typescript
   const connection = new signalR.HubConnectionBuilder()
     .withUrl('/hub/metrics')
     .withAutomaticReconnect()
     .build();
   ```

3. **Check network stability**

---

### Performance Issues

#### Issue: Slow API Responses

**Diagnosis**:

1. **Enable detailed logging**:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

2. **Check database query performance**:
   ```bash
   # MySQL slow query log
   mysql -u admin -p
   SET GLOBAL slow_query_log = 'ON';
   SET GLOBAL long_query_time = 1;
   ```

**Solutions**:

1. **Enable caching** - Already configured for connection queries

2. **Add database indexes** if needed

3. **Optimize queries**:
   ```csharp
   // Disable tracking for read-only queries
   var connections = await _context.Connections
       .AsNoTracking()
       .ToListAsync();
   ```

4. **Increase connection pool**:
   ```json
   {
     "Database": {
       "ConnectionString": "server=localhost;...;Max Pool Size=200;"
     }
   }
   ```

---

#### Issue: High Memory Usage

**Diagnosis**:

```bash
# Linux
ps aux | grep RedisGUI.Server

# Docker
docker stats redis-gui
```

**Solutions**:

1. **Check for memory leaks**:
   - Ensure proper disposal of resources
   - Use `using` statements
   - Unsubscribe from events

2. **Adjust cache duration**:
   ```json
   {
     "Cache": {
       "Duration": "00:02:00"
     }
   }
   ```

3. **Limit connection pool**:
   ```json
   {
     "Database": {
       "ConnectionString": "server=localhost;...;Max Pool Size=50;"
     }
   }
   ```

4. **Restart application periodically** (if needed)

---

#### Issue: Slow Frontend Loading

**Solutions**:

1. **Enable production build**:
   ```bash
   cd src/RedisGUI.Client
   ng build --configuration production
   ```

2. **Enable gzip compression** in Nginx:
   ```nginx
   gzip on;
   gzip_types text/css application/javascript application/json;
   ```

3. **Use CDN for static assets**

4. **Enable browser caching**:
   ```nginx
   location ~* \.(js|css|png|jpg|jpeg|gif|ico)$ {
       expires 1y;
       add_header Cache-Control "public, immutable";
   }
   ```

---

## Debugging

### Enable Detailed Logging

**appsettings.Development.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Microsoft.AspNetCore.SignalR": "Trace",
      "Microsoft.AspNetCore.Http.Connections": "Trace",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Debug in Visual Studio

1. Set breakpoints in code
2. Press F5 to start debugging
3. Step through code with F10/F11

### Debug in VS Code

1. Install C# Dev Kit extension
2. Press F5 to start debugging
3. Use debug console for evaluation

### Browser DevTools

1. Open Developer Tools (F12)
2. Check Console for errors
3. Check Network tab for API calls
4. Check Application > WebSockets for SignalR

---

## Logs and Diagnostics

### Application Logs

**Linux (systemd)**:
```bash
# View recent logs
sudo journalctl -u redis-gui -n 100

# Follow logs in real-time
sudo journalctl -u redis-gui -f

# Filter by date
sudo journalctl -u redis-gui --since "2024-02-15 10:00:00"
```

**Docker**:
```bash
# View logs
docker logs redis-gui

# Follow logs
docker logs -f redis-gui

# Last 100 lines
docker logs --tail 100 redis-gui
```

**Windows Event Viewer**:
1. Open Event Viewer
2. Windows Logs > Application
3. Filter by source: .NET Runtime

### Database Logs

**MySQL**:
```bash
# Docker
docker logs redisguidocker-db-1

# Linux
sudo tail -f /var/log/mysql/error.log

# Inside MySQL
SHOW VARIABLES LIKE 'log_error';
```

### Nginx Logs

```bash
# Access log
sudo tail -f /var/log/nginx/redis-gui-access.log

# Error log
sudo tail -f /var/log/nginx/redis-gui-error.log
```

---

## Getting Help

### Before Asking for Help

1. ✅ Check this troubleshooting guide
2. ✅ Review [Setup Guide](SETUP.md)
3. ✅ Check application logs
4. ✅ Search existing GitHub issues
5. ✅ Try to reproduce in clean environment

### When Reporting Issues

Include:

1. **Environment**:
   - OS and version
   - .NET version: `dotnet --version`
   - Node version: `node --version`
   - Docker version (if applicable)

2. **Steps to reproduce**:
   - What did you do?
   - What did you expect?
   - What actually happened?

3. **Logs and errors**:
   - Full error messages
   - Stack traces
   - Relevant log excerpts

4. **Configuration**:
   - Relevant `appsettings.json` (remove sensitive data)
   - Environment variables

### Where to Get Help

1. **GitHub Issues**: https://github.com/dellik95/redis-gui/issues
   - Search existing issues first
   - Create new issue with template

2. **Discussions**: Use for questions and general help

3. **Documentation**:
   - [Setup Guide](SETUP.md)
   - [Architecture](ARCHITECTURE.md)
   - [API Documentation](API.md)
   - [Contributing Guide](CONTRIBUTING.md)

---

## Quick Reference Commands

### Health Checks

```bash
# Check if application is running
curl https://localhost:7234/health

# Check database connection
mysql -h localhost -u admin -p -e "SELECT 1"

# Check Redis connection
redis-cli -h localhost -p 6379 ping

# Check Docker containers
docker-compose ps

# Check systemd service
sudo systemctl status redis-gui

# Check ports
netstat -tuln | grep -E ':(7234|4200|3306|6379)'
```

### Restart Services

```bash
# Application (systemd)
sudo systemctl restart redis-gui

# Docker services
docker-compose restart

# Nginx
sudo systemctl restart nginx

# MySQL (Docker)
docker-compose restart db
```

### Clean Reset

```bash
# Stop everything
docker-compose down -v
sudo systemctl stop redis-gui

# Clean build
dotnet clean
rm -rf src/RedisGUI.Client/node_modules

# Fresh start
dotnet restore
cd src/RedisGUI.Client && npm install
cd ../..
docker-compose up -d
dotnet run --project src/RedisGUI.Server
```

---

**Still having issues?** [Open an issue](https://github.com/dellik95/redis-gui/issues/new) on GitHub.
