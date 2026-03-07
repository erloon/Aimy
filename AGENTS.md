# PROJECT KNOWLEDGE BASE

## OVERVIEW

You are an expert full-stack engineer and software architect specializing in .NET 10 Minimal APIs and React/Electron applications.

Desktop and web application with Electron/React frontend + .NET 10 backend, orchestrated via .NET Aspire. Early-stage project using Hexagonal Architecture and shadcn/ui.

## STRUCTURE

```
Aimy/
├── aimy.sln                 # .NET solution
├── frontend/                # Electron + React + Vite → frontend/AGENTS.md
├── backend/                 # .NET 10 → backend/AGENTS.md
├── aspire/                  # .NET Aspire orchestration
└── docs/                    # Project documentation structure
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

## NEW FEATURE WORKFLOW

1. **Brainstorm**: Check `docs/` for the current project state. Discuss with user to finalize description and requirements.
2. **Plan**: Create implementation plan based on `docs/` and discussion.
3. **Implement**: Write code + **unit tests** (mandatory).
4. **Test**: Run app via `aspire run`. Use Aspire MCP server and `playwright` to test UI.
5. **Document**: Update `docs/` following conventions.
6. **Complete**: Task ends only when unit tests pass and feature is verified.

## CRITICAL RULES

- **DO NOT** use placeholders (e.g., `// TODO: Implement later`) in generated code. Always provide fully working implementations.
- **DO NOT** modify anything in `docs/` without first verifying the actual codebase state.
- **NEVER** skip unit testing - code without tests is considered unfinished.

## GENERAL CONVENTIONS

**Naming:**
- PascalCase for C# public members
- camelCase for TypeScript
- kebab-case for files

**Testing:**
- Backend tests: NUnit + Moq + FluentAssertions
- Frontend tests: Vitest + Testing Library

## CREDENTIALS

**UI Login:**
- **Username:** `admin`
- **Password:** `admin123`

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
| `docs/general/plans/archive/`| Completed, rejected, or obsolete plans |
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
