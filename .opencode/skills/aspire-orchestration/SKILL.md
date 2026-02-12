---
name: aspire-orchestration
description: .NET Aspire orchestration for distributed applications. Use when adding container resources (Postgres, Redis, Minio), configuring service references, managing startup order with WaitFor, injecting connection strings, or setting up parameters and secrets. Handles AppHost configuration and service discovery patterns.
---

# .NET Aspire Orchestration

## Core Concept

Aspire AppHost orchestrates distributed application resources (containers, projects, executables) and manages their lifecycle and inter-service communication.

## AppHost Structure

```csharp
// aspire/Aimy.AppHost/AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

// Add resources here

builder.Build().Run();
```

## Adding Container Resources

### PostgreSQL

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("aimy-postgres");  // Persists data

var db = postgres.AddDatabase("aimydb");

builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(db)
    .WaitFor(postgres);  // API waits for Postgres to be ready
```

### Redis

```csharp
var redis = builder.AddRedis("redis")
    .WithDataVolume("aimy-redis");

builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(redis);
```

### MinIO (S3-Compatible Storage)

```csharp
var minioUser = builder.AddParameter("minio-username", secret: true);
var minioPass = builder.AddParameter("minio-password", secret: true);

var storage = builder.AddMinioContainer("storage", minioUser, minioPass)
    .WithDataVolume("aimy-storage");

builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(storage)
    .WaitFor(storage);
```

## Service References

### WithReference Pattern

Injects connection information into the target service:

```csharp
var db = postgres.AddDatabase("aimydb");

builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(db);  // Injects ConnectionStrings:aimydb
```

In the API, access via configuration:

```csharp
// Program.cs in API
builder.AddNpgsqlDbContext<ApplicationDbContext>("aimydb");
```

### Multiple References

```csharp
builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(db)
    .WithReference(redis)
    .WithReference(storage)
    .WaitFor(db)      // Wait for specific resource
    .WaitFor(redis);
```

## Startup Sequencing

### WaitFor Pattern

Ensures a service waits for dependencies to be healthy:

```csharp
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("aimydb");

builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(db)
    .WaitFor(postgres);  // Waits for Postgres container to accept connections
```

### WaitFor vs WaitForCompletion

| Method | Use Case |
|--------|----------|
| `WaitFor(resource)` | Wait until resource is healthy (ready to accept connections) |
| `WaitForCompletion(resource)` | Wait until resource finishes (for migrations, seeders) |

## Parameters (Secrets)

### Define Parameters

```csharp
var dbPassword = builder.AddParameter("db-password", secret: true);
var jwtKey = builder.AddParameter("jwt-key", secret: true);
```

### Use Parameters

```csharp
var postgres = builder.AddPostgres("postgres", password: dbPassword);
```

### Configure Values

via User Secrets (for local dev):**
```bash
dotnet user-secrets set "Parameters:db-password" "secure_password"
```

**Or environment variables for production:**
```bash
export Parameters__db-password=secure_password
```

## Common Resource Types

| Method | Resource | Provides |
|--------|----------|----------|
| `AddPostgres()` | PostgreSQL container | Connection string |
| `AddRedis()` | Redis container | Connection string |
| `AddRabbitMQ()` | RabbitMQ container | Connection string |
| `AddMinioContainer()` | MinIO container | S3 endpoint |
| `AddProject<T>()` | .NET project | Service URL |
| `AddExecutable()` | External executable | Custom endpoint |
| `AddContainer()` | Generic Docker container | Custom config |

## Complete Example

```csharp
// aspire/Aimy.AppHost/AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

// Parameters
var dbPassword = builder.AddParameter("db-password", secret: true);
var minioUser = builder.AddParameter("minio-username", secret: true);
var minioPass = builder.AddParameter("minio-password", secret: true);

// Infrastructure
var postgres = builder.AddPostgres("postgres", password: dbPassword)
    .WithDataVolume("aimy-postgres");

var db = postgres.AddDatabase("aimydb");

var storage = builder.AddMinioContainer("storage", minioUser, minioPass)
    .WithDataVolume("aimy-storage");

// Application
var api = builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(db)
    .WithReference(storage)
    .WaitFor(postgres);

var frontend = builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api);

builder.Build().Run();
```

## Service Discovery

Services discover each other via environment variables injected by Aspire:

```
// In API, to call another service:
// Service URL is available as environment variable
// e.g., services__frontend__http__0 = "http://localhost:5001"
```

## Data Volumes

Persist container data between restarts:

```csharp
.AddPostgres("postgres")
    .WithDataVolume("volume-name")           // Named volume
    .WithDataBindMount("./data")             // Host folder
    .WithLifetime(ContainerLifetime.Persistent);  // Survives app restarts
```

## Health Checks

Aspire automatically creates health checks for resources. Access at:

```
http://localhost:5000/health
```

## Common Patterns

### Frontend + Backend

```csharp
var api = builder.AddProject<Projects.Aimy_API>("api");

builder.AddViteApp("frontend", "../frontend")
    .WithReference(api);  // Frontend can call API
```

### Multiple APIs

```csharp
var api1 = builder.AddProject<Projects.Aimy_API1>("api1");
var api2 = builder.AddProject<Projects.Aimy_API2>("api2")
    .WithReference(api1);  // api2 calls api1
```

### External Service

```csharp
var externalApi = builder.AddExternalService("external", "https://api.example.com");

builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(externalApi);
```

## Debugging

View all resources and their status:
- Dashboard: `http://localhost:15000` (started automatically)
- Logs: Check console output for resource startup messages
- Health: `http://localhost:PORT/health` per service
