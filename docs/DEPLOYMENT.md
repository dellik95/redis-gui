# Deployment Guide

## Table of Contents

- [Overview](#overview)
- [Deployment Strategies](#deployment-strategies)
- [Building for Production](#building-for-production)
- [Linux Deployment](#linux-deployment)
- [Windows Deployment](#windows-deployment)
- [Docker Deployment](#docker-deployment)
- [Cloud Deployment](#cloud-deployment)
- [Configuration Management](#configuration-management)
- [Performance Optimization](#performance-optimization)
- [Monitoring and Logging](#monitoring-and-logging)
- [Security Considerations](#security-considerations)

## Overview

This guide covers deployment of Redis GUI to various production environments. The application is a .NET 9 backend with Angular 19 frontend.

### Deployment Prerequisites

- Server with .NET 9 Runtime
- MySQL database server
- SSL/TLS certificates
- Domain name (recommended)
- Firewall configuration
- Reverse proxy (recommended)

## Deployment Strategies

### 1. Self-Contained Deployment

Application includes .NET runtime - no need to install .NET on server.

**Pros**:
- No runtime dependencies
- Consistent across environments
- Easier to deploy

**Cons**:
- Larger file size (~100 MB)
- Updates require full redeployment

### 2. Framework-Dependent Deployment

Application requires .NET runtime on server.

**Pros**:
- Smaller file size (~10 MB)
- Shared runtime across applications
- Security patches via runtime updates

**Cons**:
- Requires .NET installation
- Runtime version compatibility

## Building for Production

### 1. Build Backend

#### Self-Contained Deployment

```bash
# Windows
dotnet publish src/RedisGUI.Server/RedisGUI.Server.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o ./publish/win-x64

# Linux
dotnet publish src/RedisGUI.Server/RedisGUI.Server.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./publish/linux-x64

# macOS
dotnet publish src/RedisGUI.Server/RedisGUI.Server.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -o ./publish/osx-x64
```

#### Framework-Dependent Deployment

```bash
dotnet publish src/RedisGUI.Server/RedisGUI.Server.csproj \
  -c Release \
  -o ./publish
```

### 2. Build Frontend

The Angular application is automatically built and included with the backend during publish.

To build separately:

```bash
cd src/RedisGUI.Client
npm run build -- --configuration production
```

Output: `src/RedisGUI.Client/dist/`

### 3. Optimize Build

```bash
# Enable ReadyToRun compilation (faster startup)
dotnet publish src/RedisGUI.Server/RedisGUI.Server.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishReadyToRun=true \
  -p:PublishSingleFile=true \
  -o ./publish

# Single file deployment
dotnet publish src/RedisGUI.Server/RedisGUI.Server.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish
```

## Linux Deployment

### Ubuntu/Debian Server Setup

#### 1. Install .NET Runtime (if framework-dependent)

```bash
# Add Microsoft repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET Runtime
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-9.0
```

#### 2. Create Application Directory

```bash
sudo mkdir -p /var/www/redis-gui
sudo chown -R $USER:$USER /var/www/redis-gui
```

#### 3. Copy Application Files

```bash
# From local machine
scp -r ./publish/* user@server:/var/www/redis-gui/

# Or use rsync
rsync -avz --delete ./publish/ user@server:/var/www/redis-gui/
```

#### 4. Create Configuration File

Create `/var/www/redis-gui/appsettings.Production.json`:

```json
{
  "Database": {
    "IsInMemory": false,
    "ConnectionString": "server=localhost;database=redis-gui;user=redis_gui_user;password=SECURE_PASSWORD;SslMode=Required;"
  },
  "Cryptography": {
    "SecurityKey": "GENERATE_SECURE_KEY_HERE"
  },
  "MetricsCollector": {
    "PoolingIntervalInSec": 5
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

#### 5. Set Permissions

```bash
sudo chown -R www-data:www-data /var/www/redis-gui
sudo chmod -R 755 /var/www/redis-gui

# Make executable
sudo chmod +x /var/www/redis-gui/RedisGUI.Server
```

#### 6. Create systemd Service

Create `/etc/systemd/system/redis-gui.service`:

```ini
[Unit]
Description=Redis GUI Application
After=network.target mysql.service

[Service]
Type=notify
WorkingDirectory=/var/www/redis-gui
ExecStart=/var/www/redis-gui/RedisGUI.Server
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=redis-gui
User=www-data
Group=www-data

# Environment variables
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://localhost:5000

# Security settings
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/www/redis-gui

[Install]
WantedBy=multi-user.target
```

#### 7. Start Service

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable redis-gui

# Start service
sudo systemctl start redis-gui

# Check status
sudo systemctl status redis-gui

# View logs
sudo journalctl -u redis-gui -f
```

### Configure Nginx Reverse Proxy

#### 1. Install Nginx

```bash
sudo apt-get install nginx
```

#### 2. Create Nginx Configuration

Create `/etc/nginx/sites-available/redis-gui`:

```nginx
# HTTP to HTTPS redirect
server {
    listen 80;
    listen [::]:80;
    server_name yourdomain.com www.yourdomain.com;

    return 301 https://$server_name$request_uri;
}

# HTTPS server
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;

    # SSL certificates
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Logging
    access_log /var/log/nginx/redis-gui-access.log;
    error_log /var/log/nginx/redis-gui-error.log;

    # Proxy to Kestrel
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # SignalR WebSocket support
    location /hub/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        
        # WebSocket timeouts
        proxy_connect_timeout 7d;
        proxy_send_timeout 7d;
        proxy_read_timeout 7d;
    }

    # Static files caching
    location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot)$ {
        proxy_pass http://localhost:5000;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

#### 3. Enable Site and Restart Nginx

```bash
# Create symbolic link
sudo ln -s /etc/nginx/sites-available/redis-gui /etc/nginx/sites-enabled/

# Test configuration
sudo nginx -t

# Restart Nginx
sudo systemctl restart nginx
```

### SSL Certificate with Let's Encrypt

```bash
# Install Certbot
sudo apt-get install certbot python3-certbot-nginx

# Obtain certificate
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com

# Auto-renewal (already configured by certbot)
sudo systemctl status certbot.timer
```

## Windows Deployment

### IIS Deployment

#### 1. Install Prerequisites

- IIS with ASP.NET Core Module
- .NET 9 Hosting Bundle
- URL Rewrite Module (optional)

Download: https://dotnet.microsoft.com/download/dotnet/9.0

#### 2. Create Application Pool

```powershell
# Import IIS module
Import-Module WebAdministration

# Create application pool
New-WebAppPool -Name "RedisGUIAppPool"

# Configure app pool
Set-ItemProperty IIS:\AppPools\RedisGUIAppPool -name "managedRuntimeVersion" -value ""
Set-ItemProperty IIS:\AppPools\RedisGUIAppPool -name "enable32BitAppOnWin64" -value $false
```

#### 3. Create Website

```powershell
# Create website
New-Website -Name "RedisGUI" `
  -Port 80 `
  -HostHeader "redis-gui.local" `
  -PhysicalPath "C:\inetpub\redis-gui" `
  -ApplicationPool "RedisGUIAppPool"

# Set permissions
icacls "C:\inetpub\redis-gui" /grant "IIS_IUSRS:(OI)(CI)F" /T
```

#### 4. Configure HTTPS

1. Obtain SSL certificate
2. Bind to IIS site
3. Force HTTPS redirect

#### 5. web.config

IIS requires `web.config` (auto-generated during publish):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\RedisGUI.Server.exe" 
                  stdoutLogEnabled="false" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### Windows Service

Run as Windows Service using `sc.exe`:

```powershell
# Create service
sc.exe create RedisGUI binPath="C:\Apps\redis-gui\RedisGUI.Server.exe" start=auto

# Start service
sc.exe start RedisGUI

# Stop service
sc.exe stop RedisGUI

# Delete service
sc.exe delete RedisGUI
```

## Docker Deployment

### Using Docker Compose (Recommended)

#### 1. Create docker-compose.yml

```yaml
version: '3.8'

services:
  app:
    image: redis-gui:latest
    build:
      context: .
      dockerfile: src/RedisGUI.Server/Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/certificate.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword
    volumes:
      - ./appsettings.Production.json:/app/appsettings.Production.json:ro
      - ./https:/https:ro
      - app-logs:/app/logs
    depends_on:
      - db
    restart: unless-stopped
    networks:
      - redis-gui-network

  db:
    image: mysql:8.0
    environment:
      MYSQL_DATABASE: redis-gui
      MYSQL_USER: redis_gui_user
      MYSQL_PASSWORD: secure_password
      MYSQL_ROOT_PASSWORD: root_password
    volumes:
      - mysql-data:/var/lib/mysql
    ports:
      - "3306:3306"
    restart: unless-stopped
    networks:
      - redis-gui-network

  phpmyadmin:
    image: phpmyadmin:latest
    environment:
      PMA_HOST: db
      MYSQL_ROOT_PASSWORD: root_password
    ports:
      - "8090:80"
    depends_on:
      - db
    restart: unless-stopped
    networks:
      - redis-gui-network

volumes:
  mysql-data:
  app-logs:

networks:
  redis-gui-network:
    driver: bridge
```

#### 2. Build and Run

```bash
# Build image
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f app

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Standalone Docker

#### 1. Build Image

```bash
docker build -t redis-gui:latest -f src/RedisGUI.Server/Dockerfile .
```

#### 2. Run Container

```bash
docker run -d \
  --name redis-gui \
  -p 8080:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v $(pwd)/appsettings.Production.json:/app/appsettings.Production.json:ro \
  redis-gui:latest
```

### Docker Health Check

Add to Dockerfile:

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1
```

## Cloud Deployment

### Azure App Service

#### Using Azure CLI

```bash
# Login
az login

# Create resource group
az group create --name redis-gui-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name redis-gui-plan \
  --resource-group redis-gui-rg \
  --sku B1 \
  --is-linux

# Create web app
az webapp create \
  --name redis-gui-app \
  --resource-group redis-gui-rg \
  --plan redis-gui-plan \
  --runtime "DOTNET|9.0"

# Deploy application
az webapp deployment source config-zip \
  --resource-group redis-gui-rg \
  --name redis-gui-app \
  --src ./publish.zip

# Configure app settings
az webapp config appsettings set \
  --resource-group redis-gui-rg \
  --name redis-gui-app \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    Database__ConnectionString="connection-string-here"
```

### AWS Elastic Beanstalk

#### 1. Install EB CLI

```bash
pip install awsebcli
```

#### 2. Initialize and Deploy

```bash
# Initialize
eb init -p "64bit Amazon Linux 2 v2.5.0 running .NET Core" redis-gui

# Create environment
eb create redis-gui-env

# Deploy
eb deploy

# Open in browser
eb open
```

### Google Cloud Run

```bash
# Build container
gcloud builds submit --tag gcr.io/PROJECT-ID/redis-gui

# Deploy
gcloud run deploy redis-gui \
  --image gcr.io/PROJECT-ID/redis-gui \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated
```

## Configuration Management

### Environment Variables

Set via:
- `appsettings.{Environment}.json`
- Environment variables
- Azure Key Vault
- Docker secrets

**Priority**: Environment variables > appsettings.{Env}.json > appsettings.json

### Secrets Management

#### Using .NET User Secrets (Development)

```bash
dotnet user-secrets init --project src/RedisGUI.Server
dotnet user-secrets set "Database:ConnectionString" "connection-string"
dotnet user-secrets set "Cryptography:SecurityKey" "security-key"
```

#### Using Azure Key Vault (Production)

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

#### Using Docker Secrets

```yaml
services:
  app:
    secrets:
      - db_password
      - security_key

secrets:
  db_password:
    file: ./secrets/db_password.txt
  security_key:
    file: ./secrets/security_key.txt
```

## Performance Optimization

### 1. Enable Response Compression

Already configured in the application.

### 2. Enable Output Caching

```csharp
app.UseOutputCache();
```

### 3. Database Connection Pooling

Configure in connection string:

```
server=localhost;database=redis-gui;user=admin;password=admin;Pooling=true;Min Pool Size=5;Max Pool Size=100;
```

### 4. Redis Connection Pooling

Already implemented via `ConnectionPool`.

### 5. CDN for Static Assets

Configure CDN URL in production:

```json
{
  "StaticFiles": {
    "CDN": "https://cdn.yourdomain.com"
  }
}
```

## Monitoring and Logging

### Application Insights (Azure)

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Structured Logging with Serilog

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/redis-gui-.txt", rollingInterval: RollingInterval.Day));
```

### Health Checks

Add endpoint for monitoring:

```csharp
app.MapHealthChecks("/health");
```

Monitor with:
- Uptime Robot
- Azure Monitor
- AWS CloudWatch
- Prometheus/Grafana

## Security Considerations

### 1. HTTPS Only

```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

### 2. Security Headers

Configured in Nginx or use middleware:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    await next();
});
```

### 3. CORS Policy

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder.WithOrigins("https://yourdomain.com")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("Production");
```

### 4. Rate Limiting

```csharp
builder.Services.AddRateLimiter(options => { /* configure */ });
```

### 5. Database Security

- Use strong passwords
- Enable SSL/TLS for database connections
- Restrict database access by IP
- Regular security updates

---

## Troubleshooting Deployment

### Common Issues

#### 1. Application Won't Start

**Check**:
```bash
# Linux
sudo journalctl -u redis-gui -n 50

# Docker
docker logs redis-gui
```

#### 2. Database Connection Failed

- Verify connection string
- Check firewall rules
- Test database connectivity

#### 3. Permission Denied

```bash
sudo chown -R www-data:www-data /var/www/redis-gui
sudo chmod -R 755 /var/www/redis-gui
```

---

**Next**: [Troubleshooting Guide](TROUBLESHOOTING.md) | [Architecture](ARCHITECTURE.md)
