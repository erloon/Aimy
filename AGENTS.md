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

| Skill                      | Layer   | When to Use                                        |
| -------------------------- | ------- | -------------------------------------------------- |
| `hexagonal-dotnet`         | Backend | Architecture decisions, entity/interface placement |
| `minimal-api-organization` | Backend | API endpoints, TypedResults, extension methods     |
| `minio-storage`            | Backend | File storage, S3 operations                        |
| `aspire-orchestration`     | Aspire  | Container resources, service references            |

## KNOWLEDGE HIERARCHY

| Module        | File                                   | Scope                                 |
| ------------- | -------------------------------------- | ------------------------------------- |
| Root          | `./AGENTS.md`                          | Project overview, general conventions |
| Docs          | `docs/AGENTS.md`                       | Documentation structure & workflow    |
| Frontend      | `frontend/AGENTS.md`                   | Electron, React, Vite, Tailwind       |
| UI Components | `frontend/src/components/ui/AGENTS.md` | shadcn/ui patterns                    |
| Backend       | `backend/AGENTS.md`                    | .NET architecture, skills reference   |

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
aspire run

# Frontend only
cd frontend && npm run dev

# Backend only
dotnet run --project backend/Aimy.API

# Build all
cd frontend && npm run build
dotnet build
```

## ANTI-PATTERNS (PROJECT-WIDE)

| Avoid                        | Reason                          |
| ---------------------------- | ------------------------------- |
| External deps in Core        | Violates hexagonal architecture |
| `as any` / `@ts-ignore`      | Bypasses type safety            |
| Business logic in Program.cs | Violates separation of concerns |
| EF attributes on entities    | ORM leak into domain            |
| Controllers                  | Use minimal API                 |

## NOTES

- No CI/CD configured yet
- Frontend expects backend at localhost (Aspire handles in dev)
- Electron packages to `frontend/release/` on build

---

## DOCUMENTATION STANDARDS

We follow a strict documentation-first approach. All features and architectural decisions must be documented in `docs/`.

### Documentation Structure
| Path                         | Content                                |
| ---------------------------- | -------------------------------------- |
| `docs/backend/api/`          | Contracts, DTOs, API specs             |
| `docs/backend/architecture/` | Diagrams, patterns, decisions          |
| `docs/backend/database/`     | Schema, migrations, logic              |
| `docs/backend/features/`     | Feature specs (Logic & API)            |
| `docs/ui/components/`        | Reusable components & design system    |
| `docs/ui/features/`          | Feature specs (UI flows & interaction) |
| `docs/general/plans/`        | RFCs, roadmaps, high-level designs     |
| `docs/general/standards/`    | Coding standards & conventions         |

### Documentation Workflow
1. **Discovery & Planning Phase**:
   - Check `docs/` for relevant plans or specs.
   - Create high-level plans in `docs/general/plans/` (RFCs).
   - Define specs in `docs/backend/features/` or `docs/ui/features/`.

2. **Implementation Phase**:
   - Update documentation **simultaneously** with code changes.
   - Update API docs in `docs/backend/api/` when DTOs change.
   - Document new UI components in `docs/ui/components/`.

3. **Review Phase**:
   - Verify code matches the documentation.
   - If the implementation diverged, **update the docs** to reflect reality.
