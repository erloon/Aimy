# FRONTEND (Electron + React + Vite)

## OVERVIEW
Desktop and web app built with Electron + React 18 + Vite + Tailwind + shadcn/ui. Uses vite-plugin-electron for integrated build.

## STRUCTURE
```
frontend/
├── electron/               # Electron process files
│   ├── main.ts             # Main process (window creation, lifecycle)
│   └── preload.ts          # Context bridge for IPC
├── src/                    # React application
│   ├── components/         # UI components
│   │   ├── ui/             # shadcn/ui primitives → see ui/AGENTS.md
│   │   └── app-sidebar.tsx # Main navigation component
│   ├── hooks/              # Custom hooks (useIsMobile)
│   ├── lib/                # Utilities (cn helper)
│   ├── App.tsx             # Root component
│   ├── main.tsx            # React entry point
│   └── index.css           # Tailwind imports + CSS variables
├── public/                 # Static assets
├── dist/                   # Vite build output (renderer)
├── dist-electron/          # Compiled Electron main/preload
├── release/                # Electron-builder output
├── vite.config.ts          # Vite + Electron plugin config
├── electron-builder.json5  # Desktop packaging config
├── components.json         # shadcn/ui CLI config
└── tailwind.config.cjs     # Tailwind + CSS variables
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Add new page/view | `src/App.tsx` | Single route, early stage |
| Add UI component | `src/components/ui/` | Use `npx shadcn add <name>` |
| Add custom hook | `src/hooks/` | Follow `use-*.tsx` naming |
| Modify Electron window | `electron/main.ts` | BrowserWindow options |
| Add IPC channel | `electron/preload.ts` | Use contextBridge |
| Change Tailwind theme | `tailwind.config.cjs` | CSS variables in index.css |
| Update build targets | `electron-builder.json5` | Mac/Win/Linux options |

## CONVENTIONS

**Imports:**
- Path alias: `@/*` → `./src/*`
- UI components: `import { X } from "@/components/ui/x"`
- Utils: `import { cn } from "@/lib/utils"`

**Component Pattern:**
```tsx
import { cn } from "@/lib/utils"

interface Props { ... }

const Component = React.forwardRef<HTMLElement, Props>(
  ({ className, ...props }, ref) => (
    <element ref={ref} className={cn("base-classes", className)} {...props} />
  )
)
Component.displayName = "Component"
export { Component }
```

**Styling:**
- Tailwind utilities only — no CSS modules
- CSS variables for theming (defined in index.css)
- Use `cn()` for conditional/merged classes
- CVA (class-variance-authority) for component variants

**Electron:**
- Dev: `VITE_DEV_SERVER_URL` env → load from Vite dev server
- Prod: Load from `dist/index.html`
- IPC via contextBridge in preload.ts

## ANTI-PATTERNS
- Don't use `as any` / `@ts-ignore` — strict mode
- Don't import from `@radix-ui/*` directly — use ui/ wrappers
- Don't skip `cn()` for dynamic classes — merge conflicts
- Don't put business logic in electron/main.ts — keep it minimal

## COMMANDS
```bash
npm run dev       # Vite dev server + Electron (hot reload)
npm run build     # tsc → vite build → electron-builder
npm run lint      # ESLint (max-warnings 0)
npm run preview   # Preview production build
```

## ADDING COMPONENTS
```bash
# Add shadcn/ui component
npx shadcn@latest add <component>

# Examples
npx shadcn@latest add dialog
npx shadcn@latest add select
```

## NOTES
- No routing library yet — single view app
- No tests configured (add Vitest when needed)
- Builds to `release/{version}/` directory
- NSIS installer on Windows (configurable in electron-builder.json5)
