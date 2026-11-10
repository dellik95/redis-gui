# RedisGUI — Docker Deployment & Containerization Guide

This guide describes how to run and deploy **RedisGUI** using Docker and Docker Compose for development and production environments.

---

## 1. Quickstart with Docker Compose

The simplest way to run RedisGUI alongside a local Redis 7 instance is via `docker-compose.yml`:

```bash
docker compose up -d --build
```

Once running, navigate to:
- **Web UI & API**: [http://localhost:5000](http://localhost:5000)
- **Redis Server**: `localhost:6379`

### Default Login
- **Email**: `admin@redisgui.local`
- **Password**: `AdminPassword123!`

---

## 2. Docker Compose Configuration Overview

The included `docker-compose.yml` orchestrates the backend server, the pre-built Angular frontend (served through ASP.NET Core static files), and a Redis instance with persistent volume storage:

```yaml
services:
  redis:
    image: redis:7-alpine
    container_name: redisgui-redis
    restart: unless-stopped
    ports:
      - "6379:6379"
    command: ["redis-server", "--appendonly", "yes"]
    volumes:
      - redis_data:/data
    networks:
      - redisgui-network

  redisgui:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: redisgui-app
    restart: unless-stopped
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5000
      - Redis__DefaultHost=redis
      - Redis__DefaultPort=6379
    depends_on:
      - redis
    networks:
      - redisgui-network

volumes:
  redis_data:

networks:
  redisgui-network:
    driver: bridge
```

---

## 3. Multi-Stage Dockerfile Architecture

The `Dockerfile` employs multi-stage builds to optimize image size and build caching:

1. **Stage 1 (Node.js Build)**: Builds the Angular 19 client into optimized production static bundles.
2. **Stage 2 (.NET SDK Build & Publish)**: Restores packages, compiles C# projects using the .NET 10 SDK, and publishes the server assembly.
3. **Stage 3 (Runtime Image)**: Copies the published .NET binaries and the client static files into a minimal `aspnet:10.0` Alpine or Debian base image.

---

## 4. Useful Docker Commands

### View Logs
```bash
# View backend application logs
docker compose logs -f redisgui

# View Redis engine logs
docker compose logs -f redis
```

### Stop Containers
```bash
docker compose down
```

### Reset Data Volumes
```bash
docker compose down -v
```
