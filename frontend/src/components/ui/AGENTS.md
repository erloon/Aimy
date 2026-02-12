# UI COMPONENTS (shadcn/ui)

## OVERVIEW
shadcn/ui primitives built on Radix UI + Tailwind. Not a library — copy/paste components with full control.

## STRUCTURE
```
ui/
├── button.tsx        # Primary action (variants: default, destructive, outline, ghost, link)
├── sidebar.tsx       # Complex sidebar primitive (~770 lines)
├── dropdown-menu.tsx # Context menus, selects
├── sheet.tsx         # Side panel overlay
├── dialog.tsx        # Modal overlay
├── avatar.tsx        # User images with fallback
├── input.tsx         # Text input
├── tooltip.tsx       # Hover tooltips
├── separator.tsx     # Visual divider
├── skeleton.tsx      # Loading placeholder
└── collapsible.tsx   # Expandable sections
```

## WHERE TO LOOK
| Task | File | Pattern |
|------|------|---------|
| Add button variant | `button.tsx` | Extend `buttonVariants` CVA |
| Customize sidebar | `sidebar.tsx` | Use context (`useSidebar`) |
| Add dropdown item | `dropdown-menu.tsx` | Radix DropdownMenuItem |
| Style loading state | `skeleton.tsx` | Animate pulse classes |

## CONVENTIONS
- **Import from `@/components/ui/...`** — never relative paths
- **Use `cn()` for classes** — handles Tailwind merge conflicts
- **Export `*Variants` separately** — e.g., `buttonVariants` for styled usage
- **forwardRef always** — enables ref forwarding for all primitives
- **asChild pattern** — use Radix Slot for composition

```tsx
// Standard component pattern
const Component = React.forwardRef<HTMLElement, Props>(
  ({ className, ...props }, ref) => (
    <Element ref={ref} className={cn(baseClasses, className)} {...props} />
  )
)
Component.displayName = "Component"
```

## ANTI-PATTERNS
- Don't import from `@radix-ui/*` directly — use these wrappers
- Don't skip `cn()` — class conflicts will occur
- Don't modify base variants — add new ones instead

## SIDEBAR CONTEXT
Complex component with built-in state. Use hooks:
- `useSidebar()` — access `isMobile`, `toggleSidebar`, `open`, `collapsed`
- `SidebarProvider` wraps app, `collapsible="icon"` for mini-mode

## ADDING NEW COMPONENTS
1. `npx shadcn@latest add <component>` in frontend/
2. Or copy from [ui.shadcn.com](https://ui.shadcn.com)
3. Ensure `cn` import: `import { cn } from "@/lib/utils"`
