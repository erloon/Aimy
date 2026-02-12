# PROJECT KNOWLEDGE BASE

## OVERVIEW

Desktop and web application with Electron/React frontend + .NET 10 backend, orchestrated via .NET Aspire. Early-stage project using Hexagonal Architecture and shadcn/ui.

## STRUCTURE

```
Aimy/
├── aimy.sln                 # .NET solution
├── frontend/                # Electron + React + Vite → frontend/AGENTS.md
├── backend/                 # .NET 10 → backend/AGENTS.md
│   ├── Aimy.API/            # ASP.NET Core API (entry point)
│   ├── Aimy.Core/           # Domain (entities, ports, services)
│   └── Aimy.Infrastructure/ # Adapters (EF Core, external libs)
└── aspire/                  # .NET Aspire orchestration
    ├── Aimy.AppHost/        # Container orchestration
    └── Aimy.ServiceDefaults/ # Telemetry, health, resilience
```

## SKILLS (Project-Specific)

| Skill | Layer | When to Use |
|-------|-------|-------------|
| `hexagonal-dotnet` | Backend | Architecture decisions, entity/interface placement |
| `minimal-api-organization` | Backend | API endpoints, TypedResults, extension methods |
| `minio-storage` | Backend | File storage, S3 operations |
| `aspire-orchestration` | Aspire | Container resources, service references |

## KNOWLEDGE HIERARCHY

| Module | File | Scope |
|--------|------|-------|
| Root | `./AGENTS.md` | Project overview, general conventions |
| Frontend | `frontend/AGENTS.md` | Electron, React, Vite, Tailwind |
| UI Components | `frontend/src/components/ui/AGENTS.md` | shadcn/ui patterns |
| Backend | `backend/AGENTS.md` | .NET architecture, skills reference |

## GENERAL CONVENTIONS

**All Layers:**
- Strict typing (no `as any`, `@ts-ignore`, dynamic)
- Nullable enabled everywhere
- No business logic in entry points (Program.cs, main.ts)

**Dependency Direction:**
```
API → Infrastructure → Core ← Tests
```
Core has ZERO external dependencies.

**Naming:**
- PascalCase for C# public members
- camelCase for TypeScript
- kebab-case for files

## COMMANDS

```bash
# Full stack (recommended)
dotnet run --project aspire/Aimy.AppHost

# Frontend only
cd frontend && npm run dev

# Backend only
dotnet run --project backend/Aimy.API

# Build all
cd frontend && npm run build
dotnet build
```

## ANTI-PATTERNS (PROJECT-WIDE)

| Avoid | Reason |
|-------|--------|
| External deps in Core | Violates hexagonal architecture |
| `as any` / `@ts-ignore` | Bypasses type safety |
| Business logic in Program.cs | Violates separation of concerns |
| EF attributes on entities | ORM leak into domain |
| Controllers | Use minimal API |

## NOTES

- No CI/CD configured yet
- No tests yet (add Vitest/NUnit when needed)
- Frontend expects backend at localhost (Aspire handles in dev)
- Electron packages to `frontend/release/` on build
