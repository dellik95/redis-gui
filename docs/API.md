# API Documentation

## Table of Contents

- [Overview](#overview)
- [Base URL](#base-url)
- [Authentication](#authentication)
- [API Versioning](#api-versioning)
- [Response Format](#response-format)
- [Error Handling](#error-handling)
- [Endpoints](#endpoints)
  - [Connections API](#connections-api)
  - [Redis Storage API](#redis-storage-api)
  - [Metrics API](#metrics-api)
- [SignalR Hubs](#signalr-hubs)
- [Examples](#examples)

## Overview

The Redis GUI API provides RESTful endpoints for managing Redis connections and performing operations on Redis data. It follows REST principles and uses JSON for request/response payloads.

**Features**:
- RESTful API design
- API versioning
- OpenAPI/Swagger documentation
- Real-time updates via SignalR
- Comprehensive error handling
- Request validation

## Base URL

### Development
```
https://localhost:7234/api/v1
```

### Production
```
https://your-domain.com/api/v1
```

## Authentication

Currently, the API does not require authentication. Future versions will include:
- JWT-based authentication
- Role-based authorization
- API key authentication

## API Versioning

The API uses URL-based versioning:

```
/api/v1/connections
/api/v2/connections  (future)
```

Current version: **v1**

## Response Format

### Success Response

```json
{
  "data": {
    // Response data
  },
  "isSuccess": true,
  "error": null
}
```

### Error Response

```json
{
  "isSuccess": false,
  "error": {
    "code": "CONNECTION_NOT_FOUND",
    "message": "Connection with ID '123' was not found"
  }
}
```

### Validation Error Response

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."],
    "Port": ["Port must be between 1 and 65535."]
  }
}
```

## Error Handling

### HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 | OK - Request successful |
| 201 | Created - Resource created successfully |
| 204 | No Content - Request successful, no content to return |
| 400 | Bad Request - Invalid input or validation error |
| 404 | Not Found - Resource not found |
| 500 | Internal Server Error - Server error |
| 503 | Service Unavailable - Service temporarily unavailable |

### Error Codes

| Error Code | Description |
|------------|-------------|
| `CONNECTION_NOT_FOUND` | Connection with specified ID not found |
| `CONNECTION_FAILED` | Failed to connect to Redis server |
| `INVALID_PORT` | Port number is invalid |
| `INVALID_HOST` | Host address is invalid |
| `INVALID_NAME` | Connection name is invalid |
| `KEY_NOT_FOUND` | Redis key not found |
| `OPERATION_FAILED` | Redis operation failed |

## Endpoints

### Connections API

#### Get All Connections

Retrieve all saved Redis connections.

**Endpoint**: `GET /api/v1/connections`

**Response**: `200 OK`

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Local Redis",
    "host": "localhost",
    "port": 6379,
    "username": null,
    "hasPassword": false,
    "isAvailable": true,
    "lastCheckedAt": "2024-02-15T10:30:00Z"
  },
  {
    "id": "7fb95f64-5717-4562-b3fc-2c963f66afa7",
    "name": "Production Redis",
    "host": "redis.example.com",
    "port": 6379,
    "username": "admin",
    "hasPassword": true,
    "isAvailable": false,
    "lastCheckedAt": "2024-02-15T10:25:00Z"
  }
]
```

**Example**:

```bash
curl -X GET "https://localhost:7234/api/v1/connections" \
  -H "accept: application/json"
```

---

#### Get Connection by ID

Retrieve a specific connection by its ID.

**Endpoint**: `GET /api/v1/connections/{id}`

**Parameters**:
- `id` (path, required) - Connection GUID

**Response**: `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Local Redis",
  "host": "localhost",
  "port": 6379,
  "username": null,
  "hasPassword": false,
  "isAvailable": true,
  "lastCheckedAt": "2024-02-15T10:30:00Z"
}
```

**Error Response**: `404 Not Found`

```json
{
  "isSuccess": false,
  "error": {
    "code": "CONNECTION_NOT_FOUND",
    "message": "Connection with ID '3fa85f64-5717-4562-b3fc-2c963f66afa6' was not found"
  }
}
```

**Example**:

```bash
curl -X GET "https://localhost:7234/api/v1/connections/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "accept: application/json"
```

---

#### Create Connection

Create a new Redis connection.

**Endpoint**: `POST /api/v1/connections`

**Request Body**:

```json
{
  "name": "My Redis Server",
  "host": "localhost",
  "port": 6379,
  "username": "admin",      // optional
  "password": "secret123"   // optional
}
```

**Request Schema**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Connection name (3-100 characters) |
| host | string | Yes | Redis host address |
| port | integer | Yes | Port number (1-65535) |
| username | string | No | Redis username (if using ACL) |
| password | string | No | Redis password |

**Response**: `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Headers**:
```
Location: /api/v1/connections/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Validation Errors**: `400 Bad Request`

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."],
    "Port": ["Port must be between 1 and 65535."],
    "Host": ["The Host field is required."]
  }
}
```

**Example**:

```bash
curl -X POST "https://localhost:7234/api/v1/connections" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Local Redis",
    "host": "localhost",
    "port": 6379
  }'
```

---

#### Check Connection

Test if a connection to Redis server is successful.

**Endpoint**: `POST /api/v1/connections/check`

**Request Body**:

```json
{
  "host": "localhost",
  "port": 6379,
  "username": "admin",      // optional
  "password": "secret123"   // optional
}
```

**Response**: `200 OK`

```json
{
  "isSuccess": true,
  "message": "Connection successful"
}
```

**Error Response**: `400 Bad Request`

```json
{
  "isSuccess": false,
  "error": {
    "code": "CONNECTION_FAILED",
    "message": "Failed to connect to Redis server at localhost:6379"
  }
}
```

**Example**:

```bash
curl -X POST "https://localhost:7234/api/v1/connections/check" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "host": "localhost",
    "port": 6379
  }'
```

---

#### Delete Connection

Delete a saved connection.

**Endpoint**: `DELETE /api/v1/connections/{id}`

**Parameters**:
- `id` (path, required) - Connection GUID

**Response**: `204 No Content`

**Error Response**: `404 Not Found`

```json
{
  "isSuccess": false,
  "error": {
    "code": "CONNECTION_NOT_FOUND",
    "message": "Connection with ID '3fa85f64-5717-4562-b3fc-2c963f66afa6' was not found"
  }
}
```

**Example**:

```bash
curl -X DELETE "https://localhost:7234/api/v1/connections/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "accept: application/json"
```

---

### Redis Storage API

#### Get All Keys

Retrieve all keys from a Redis server.

**Endpoint**: `GET /api/v1/redis/{connectionId}/keys`

**Parameters**:
- `connectionId` (path, required) - Connection GUID
- `pattern` (query, optional) - Key pattern (default: `*`)
- `database` (query, optional) - Database number (default: 0)

**Response**: `200 OK`

```json
{
  "keys": [
    "user:1001",
    "user:1002",
    "session:abc123",
    "cache:products",
    "counter:visits"
  ],
  "count": 5
}
```

**Example**:

```bash
# Get all keys
curl -X GET "https://localhost:7234/api/v1/redis/3fa85f64-5717-4562-b3fc-2c963f66afa6/keys"

# Get keys matching pattern
curl -X GET "https://localhost:7234/api/v1/redis/3fa85f64-5717-4562-b3fc-2c963f66afa6/keys?pattern=user:*"

# Get keys from specific database
curl -X GET "https://localhost:7234/api/v1/redis/3fa85f64-5717-4562-b3fc-2c963f66afa6/keys?database=1"
```

---

#### Get Value

Retrieve the value of a specific key.

**Endpoint**: `GET /api/v1/redis/{connectionId}/values/{key}`

**Parameters**:
- `connectionId` (path, required) - Connection GUID
- `key` (path, required) - Redis key
- `database` (query, optional) - Database number (default: 0)

**Response**: `200 OK`

```json
{
  "key": "user:1001",
  "value": "{\"id\":1001,\"name\":\"John Doe\",\"email\":\"john@example.com\"}",
  "type": "string",
  "ttl": 3600,
  "size": 65
}
```

**Response Fields**:
- `key`: The Redis key
- `value`: The value (formatted based on type)
- `type`: Redis data type (string, list, set, zset, hash, stream)
- `ttl`: Time to live in seconds (-1 if no expiration)
- `size`: Size in bytes

**Error Response**: `404 Not Found`

```json
{
  "isSuccess": false,
  "error": {
    "code": "KEY_NOT_FOUND",
    "message": "Key 'user:1001' not found in database 0"
  }
}
```

**Example**:

```bash
curl -X GET "https://localhost:7234/api/v1/redis/3fa85f64-5717-4562-b3fc-2c963f66afa6/values/user:1001" \
  -H "accept: application/json"
```

---

#### Set Value

Create or update a key-value pair.

**Endpoint**: `POST /api/v1/redis/{connectionId}/values`

**Parameters**:
- `connectionId` (path, required) - Connection GUID

**Request Body**:

```json
{
  "key": "user:1001",
  "value": "{\"id\":1001,\"name\":\"John Doe\"}",
  "type": "string",
  "ttl": 3600,        // optional, in seconds
  "database": 0       // optional, default 0
}
```

**Request Schema**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| key | string | Yes | Redis key name |
| value | string | Yes | Value to store |
| type | string | Yes | Data type (string, list, set, zset, hash) |
| ttl | integer | No | Expiration time in seconds |
| database | integer | No | Database number (0-15) |

**Response**: `201 Created`

```json
{
  "success": true,
  "message": "Key 'user:1001' created successfully"
}
```

**Example**:

```bash
curl -X POST "https://localhost:7234/api/v1/redis/3fa85f64-5717-4562-b3fc-2c963f66afa6/values" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "key": "user:1001",
    "value": "{\"name\":\"John\"}",
    "type": "string",
    "ttl": 3600
  }'
```

---

#### Delete Key

Delete a key from Redis.

**Endpoint**: `DELETE /api/v1/redis/{connectionId}/values/{key}`

**Parameters**:
- `connectionId` (path, required) - Connection GUID
- `key` (path, required) - Redis key to delete
- `database` (query, optional) - Database number (default: 0)

**Response**: `204 No Content`

**Error Response**: `404 Not Found`

```json
{
  "isSuccess": false,
  "error": {
    "code": "KEY_NOT_FOUND",
    "message": "Key 'user:1001' not found"
  }
}
```

**Example**:

```bash
curl -X DELETE "https://localhost:7234/api/v1/redis/3fa85f64-5717-4562-b3fc-2c963f66afa6/values/user:1001" \
  -H "accept: application/json"
```

---

### Metrics API

Metrics are primarily delivered via SignalR (see [SignalR Hubs](#signalr-hubs)), but can also be requested via REST.

#### Get Redis Metrics

Retrieve current metrics for a specific Redis connection.

**Endpoint**: `GET /api/v1/metrics/{connectionId}`

**Parameters**:
- `connectionId` (path, required) - Connection GUID

**Response**: `200 OK`

```json
{
  "connectionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-02-15T10:30:00Z",
  "metrics": {
    "version": "7.2.3",
    "uptime": 86400,
    "connectedClients": 5,
    "usedMemory": 1048576,
    "usedMemoryPeak": 2097152,
    "totalCommandsProcessed": 1000000,
    "instantaneousOpsPerSec": 1500,
    "keyspaceHits": 950000,
    "keyspaceMisses": 50000,
    "evictedKeys": 100,
    "expiredKeys": 500,
    "totalKeys": 10000
  }
}
```

**Example**:

```bash
curl -X GET "https://localhost:7234/api/v1/metrics/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "accept: application/json"
```

---

## SignalR Hubs

### Metrics Hub

Real-time Redis metrics broadcasting.

**Hub URL**: `wss://localhost:7234/hub/metrics`

#### Client Methods

Methods that the server calls on connected clients.

##### ReceiveMetrics

Receives updated metrics for all monitored connections.

**Payload**:

```json
{
  "timestamp": "2024-02-15T10:30:00Z",
  "connections": [
    {
      "connectionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Local Redis",
      "metrics": {
        "version": "7.2.3",
        "uptime": 86400,
        "connectedClients": 5,
        "usedMemory": 1048576,
        "usedMemoryPeak": 2097152,
        "totalCommandsProcessed": 1000000,
        "instantaneousOpsPerSec": 1500,
        "keyspaceHits": 950000,
        "keyspaceMisses": 50000,
        "evictedKeys": 100,
        "expiredKeys": 500,
        "totalKeys": 10000
      }
    }
  ]
}
```

#### Usage Example (TypeScript)

```typescript
import * as signalR from '@microsoft/signalr';

// Create connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:7234/hub/metrics')
  .withAutomaticReconnect()
  .build();

// Handle incoming metrics
connection.on('ReceiveMetrics', (data) => {
  console.log('Metrics received:', data);
  // Update UI with metrics
});

// Start connection
connection.start()
  .then(() => console.log('Connected to metrics hub'))
  .catch(err => console.error('Connection error:', err));

// Cleanup on component destroy
connection.stop();
```

#### Connection Lifecycle

```
Client Connect
  └─> OnConnectedAsync()
      └─> Add to subscribers list
          └─> Background job broadcasts metrics every 5 seconds
              └─> ReceiveMetrics() called on all clients

Client Disconnect
  └─> OnDisconnectedAsync()
      └─> Remove from subscribers list
```

---

## Examples

### Complete Workflow Example

#### 1. Create a Connection

```bash
CONNECTION_ID=$(curl -X POST "https://localhost:7234/api/v1/connections" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Redis",
    "host": "localhost",
    "port": 6379
  }' | jq -r '.id')

echo "Created connection: $CONNECTION_ID"
```

#### 2. Test the Connection

```bash
curl -X POST "https://localhost:7234/api/v1/connections/check" \
  -H "Content-Type: application/json" \
  -d '{
    "host": "localhost",
    "port": 6379
  }'
```

#### 3. Set a Value

```bash
curl -X POST "https://localhost:7234/api/v1/redis/$CONNECTION_ID/values" \
  -H "Content-Type: application/json" \
  -d '{
    "key": "user:1001",
    "value": "{\"name\":\"John Doe\",\"email\":\"john@example.com\"}",
    "type": "string",
    "ttl": 3600
  }'
```

#### 4. Get the Value

```bash
curl -X GET "https://localhost:7234/api/v1/redis/$CONNECTION_ID/values/user:1001"
```

#### 5. List All Keys

```bash
curl -X GET "https://localhost:7234/api/v1/redis/$CONNECTION_ID/keys?pattern=user:*"
```

#### 6. Delete the Key

```bash
curl -X DELETE "https://localhost:7234/api/v1/redis/$CONNECTION_ID/values/user:1001"
```

#### 7. Delete the Connection

```bash
curl -X DELETE "https://localhost:7234/api/v1/connections/$CONNECTION_ID"
```

---

### JavaScript/TypeScript Client Example

```typescript
class RedisGuiClient {
  private baseUrl = 'https://localhost:7234/api/v1';

  async createConnection(data: {
    name: string;
    host: string;
    port: number;
    username?: string;
    password?: string;
  }): Promise<string> {
    const response = await fetch(`${this.baseUrl}/connections`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      throw new Error(`Failed to create connection: ${response.statusText}`);
    }

    const result = await response.json();
    return result.id;
  }

  async getAllConnections() {
    const response = await fetch(`${this.baseUrl}/connections`);
    return await response.json();
  }

  async setValue(connectionId: string, data: {
    key: string;
    value: string;
    type: string;
    ttl?: number;
  }) {
    const response = await fetch(
      `${this.baseUrl}/redis/${connectionId}/values`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      }
    );

    return await response.json();
  }

  async getValue(connectionId: string, key: string) {
    const response = await fetch(
      `${this.baseUrl}/redis/${connectionId}/values/${encodeURIComponent(key)}`
    );

    if (!response.ok) {
      throw new Error(`Key not found: ${key}`);
    }

    return await response.json();
  }
}

// Usage
const client = new RedisGuiClient();

// Create connection
const connectionId = await client.createConnection({
  name: 'My Redis',
  host: 'localhost',
  port: 6379
});

// Set value
await client.setValue(connectionId, {
  key: 'user:1001',
  value: JSON.stringify({ name: 'John' }),
  type: 'string',
  ttl: 3600
});

// Get value
const value = await client.getValue(connectionId, 'user:1001');
console.log(value);
```

---

## Interactive API Documentation

For interactive API testing, use the Swagger UI:

**URL**: https://localhost:7234/swagger

Features:
- Try out API endpoints directly from browser
- View request/response schemas
- See example requests and responses
- Download OpenAPI specification

---

## Rate Limiting

Currently, there is no rate limiting implemented. Future versions will include:
- Request rate limiting per client
- Concurrent connection limits
- Throttling for expensive operations

---

## Changelog

### Version 1.0 (Current)
- Initial API release
- CRUD operations for connections
- Redis key-value operations
- Real-time metrics via SignalR

---

**Next**: [Contributing Guide](CONTRIBUTING.md) | [Troubleshooting](TROUBLESHOOTING.md)
