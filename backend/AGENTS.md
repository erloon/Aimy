# BACKEND (.NET 10)

## OVERVIEW
ASP.NET Core minimal API + domain library. Early-stage, references Aspire ServiceDefaults for telemetry/health.

## STRUCTURE
```
backend/
├── Aimy.API/           # ASP.NET Core Web API
│   ├── Program.cs      # Entry point, minimal API endpoints
│   └── Aimy.API.csproj # References: Core, ServiceDefaults
└── Aimy.Core/          # Domain/business logic (empty placeholder)
    └── Class1.cs       # Template file — replace with domain models
```

## WHERE TO LOOK
| Task | Location                | Notes |
|------|-------------------------|-------|
| Add API endpoint | `Aimy.API/Endpoints`    | Use `app.MapGet/Post/etc.` |
| Add domain model | `Aimy.Core/Model`       | Create new files, no folder convention yet |
| Add service/DI | `Aimy.Core/Services`    | Register before `builder.Build()` |
| Add middleware | `Aimy.Core/Middlewares` | After `builder.Build()`, before `app.Run()` |

## CONVENTIONS

**Project References:**
- API → Core → (shared business logic)
- API → ServiceDefaults (telemetry, health, resilience)
- Storage we use Minio as storage for files. https://github.com/minio/minio-dotnet | 

**Record Types:**
- Use C# records for DTOs/models (immutable, concise)


## ANTI-PATTERNS
- Don't add controllers — stick to minimal API
- Don't put business logic in Program.cs — extract to Core

## NOTES
- OpenAPI enabled in dev (`app.MapOpenApi()`)
- HTTPS redirection enforced
- No authentication configured yet
- ServiceDefaults adds: OpenTelemetry, health checks, service discovery, HTTP resilience
