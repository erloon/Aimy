# Learnings

- Frontend routing in Electron requires `HashRouter` to handle file system paths correctly in production builds.
- Vite proxy configuration for Aspire services uses `process.env.services__{serviceName}__{scheme}__{index}` pattern.
- Separating layouts (`MainLayout` vs `AuthLayout`) keeps the Sidebar logic contained to authenticated routes.
- `shadcn/ui` components (`card`, `label`, `alert`) require explicit installation via `npx shadcn@latest add <component>`.
- `lucide-react` icons (like `Loader2`) integrate seamlessly with shadcn components for loading states.
- Controlled inputs with `useState` require state setters to be used (e.g., cleared on submit) to pass strict linting rules.

## Authentication Implementation
- Implemented `RequireAuth` component using `Outlet` pattern for protected routes.
- Connected `Login` page to `/api/auth/login` endpoint.
- Used `localStorage` for token storage.
- Protected the root route `/` with `RequireAuth`.
