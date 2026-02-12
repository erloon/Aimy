# BACKEND (.NET 10)

## OVERVIEW

ASP.NET Core minimal API using Hexagonal Architecture (Ports & Adapters). Core layer is pure domain with zero external dependencies. Infrastructure implements ports with EF Core and external services.

## ARCHITECTURE

```
backend/
├── Aimy.API/              # Primary Adapter (Composition Root)
│   ├── Program.cs         # DI registration, endpoints wiring
│   └── Endpoints/         # HTTP endpoint definitions
├── Aimy.Core/             # Hexagon (pure C#, no deps)
│   ├── Domain/
│   │   └── Entities/      # POCO entities
│   └── Application/
│       ├── Interfaces/    # Port interfaces (IUserRepository, etc.)
│       └── Services/      # Application logic
├── Aimy.Infrastructure/   # Adapters
│   ├── Data/              # DbContext, EF configurations
│   ├── Repositories/      # Repository implementations
│   ├── Security/          # External service adapters
│   └── DependencyInjection.cs
└── Aimy.Tests/            # NUnit + Moq + FluentAssertions
```

## SKILLS (USE THESE)

| Skill | Trigger |
|-------|---------|
| **hexagonal-dotnet** | Deciding where entity/interface goes, architecture questions |
| **minimal-api-organization** | Adding/organizing API endpoints |
| **minio-storage** | File storage operations |
| **aspire-orchestration** | Container configuration (see AppHost) |

## QUICK REFERENCE

| Task | Location | Pattern |
|------|----------|---------|
| Add entity | `Core/Domain/Entities/` | POCO, no attributes |
| Add port | `Core/Application/Interfaces/` | Interface only |
| Add service | `Core/Application/Services/` | Uses ports only |
| Add adapter | `Infrastructure/` | Implements port |
| Add endpoint | `API/Endpoints/` | Extension method + TypedResults |
| EF config | `Infrastructure/Data/Configurations/` | Fluent API |

## MIGRATIONS

```bash
dotnet ef migrations add Name \
  --project backend/Aimy.Infrastructure \
  --startup-project backend/Aimy.API
```

## VERIFICATION

```bash
# Core must have NO external deps
dotnet list backend/Aimy.Core/Aimy.Core.csproj package
dotnet list backend/Aimy.Core/Aimy.Core.csproj reference
```

## ANTI-PATTERNS

| Avoid | Solution |
|-------|----------|
| `[Key]` on entity | Use Fluent API in Infrastructure |
| `BCrypt.*` in Core | Create `IPasswordHasher` port |
| `using EF;` in Core | Keep Core pure, configure in Infrastructure |
| Controllers | Use minimal API + extension methods |
| Logic in Program.cs | Extract to services |

## PROJECT REFERENCES

```
API → Infrastructure → Core
API → ServiceDefaults (Aspire)
Tests → Core
```
