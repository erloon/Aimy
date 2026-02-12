# PROJECT KNOWLEDGE BASE

## OVERVIEW
Desktop and web app with Electron/React frontend + .NET 10 backend, orchestrated via .NET Aspire. Early-stage project using shadcn/ui components.

## STRUCTURE
```
Aimy/
├── aimy.sln                 # .NET solution (backend + aspire folders)
├── frontend/                # Electron + React + Vite → frontend/AGENTS.md
│   ├── electron/            # main.ts (process), preload.ts
│   ├── src/                 # React app
│   │   ├── components/ui/   # shadcn/ui → frontend/src/components/ui/AGENTS.md
│   │   ├── lib/             # utils (cn helper)
│   │   └── hooks/           # custom hooks
│   └── package.json
├── backend/                 # .NET 10 → backend/AGENTS.md
│   ├── Aimy.API/            # ASP.NET Core API (Program.cs)
│   └── Aimy.Core/           # Domain/business logic
└── aspire/                  # .NET Aspire orchestration
    ├── Aimy.AppHost/        # Distributed app host
    └── Aimy.ServiceDefaults/ # Shared telemetry, health checks
```

## KNOWLEDGE HIERARCHY
| Module | AGENTS.md | Scope |
|--------|-----------|-------|
| Root | `./AGENTS.md` | Project-wide overview, commands |
| Frontend | `frontend/AGENTS.md` | Electron, React, Vite, Tailwind |
| UI Components | `frontend/src/components/ui/AGENTS.md` | shadcn/ui patterns |
| Backend | `backend/AGENTS.md` | .NET API, domain, Aspire integration |

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Add UI component | `frontend/src/components/ui/` | shadcn/ui patterns, use `cn()` |
| Add page/screen | `frontend/src/App.tsx` | Single route, early stage |
| Electron main process | `frontend/electron/main.ts` | Window creation, IPC |
| API endpoints | `backend/Aimy.API/Program.cs` | Minimal API style |
| Business logic | `backend/Aimy.Core/` | Add domain classes here |
| Aspire config | `aspire/Aimy.AppHost/` | Service orchestration |
| Tailwind styles | `frontend/tailwind.config.js` | CSS variables for theming |

## CODE MAP

| Symbol | Type | Location | Role |
|--------|------|----------|------|
| `App` | Component | `frontend/src/App.tsx` | Root React component |
| `AppSidebar` | Component | `frontend/src/components/app-sidebar.tsx` | Main navigation |
| `Button` | Component | `frontend/src/components/ui/button.tsx` | Primary UI primitive |
| `cn` | Function | `frontend/src/lib/utils.ts` | Tailwind class merge |
| `createWindow` | Function | `frontend/electron/main.ts` | Electron window factory |
| `WeatherForecast` | Record | `backend/Aimy.API/Program.cs` | Sample API model |
| `AddServiceDefaults` | Method | `aspire/Aimy.ServiceDefaults/Extensions.cs` | Aspire service config |

## CONVENTIONS

**TypeScript/React:** Path alias `@/*` → `./src/*`, strict mode, forwardRef primitives. See `frontend/AGENTS.md`.

**.NET:** Minimal API, nullable enabled, ServiceDefaults shared. See `backend/AGENTS.md`.

**Styling:** Tailwind + CVA, use `cn()` from `@/lib/utils`. See `frontend/src/components/ui/AGENTS.md`.

## ANTI-PATTERNS (THIS PROJECT)
- No tests yet — early stage, add Vitest/xUnit when needed
- Deprecated npm packages in lockfile — update before adding deps
- Don't use `as any` or `@ts-ignore` — strict mode enforced

## UNIQUE STYLES
- Electron: `VITE_DEV_SERVER_URL` env var determines dev vs prod load
- Sidebar collapsible with `data-[state=open]` selectors (see ui/AGENTS.md)

## COMMANDS
```bash
# Frontend
cd frontend && npm run dev        # Vite dev server + Electron
cd frontend && npm run build      # tsc + vite build + electron-builder
cd frontend && npm run lint       # ESLint (max-warnings 0)

# Backend
dotnet build                      # Build .NET solution
dotnet run --project backend/Aimy.API  # Run API standalone

# Aspire (full stack orchestration)
dotnet run --project aspire/Aimy.AppHost
```

## NOTES
- No CI/CD configured yet
- Frontend expects backend at localhost (Aspire handles in dev)
- Electron packages to `frontend/release/` on build
