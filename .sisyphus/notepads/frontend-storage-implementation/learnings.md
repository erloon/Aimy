# Learnings

## Components

### FilesTable
- Created a reusable table component for displaying files.
- Used Shadcn `Table` component for consistent UI.
- Implemented loading skeleton for better UX during data fetching.
- Handled empty states gracefully.
- Used `renderActions` prop to decouple action logic from display logic, allowing for flexibility in parent components.
- Integrated `lucide-react` for file type icons and visual indicators.

### MetadataSheet
- Implemented a side panel using shadcn `Sheet` for viewing and editing file metadata.
- Handles JSON string parsing and serialization for key-value editing.
- Used `filesize` and `date-fns` for consistent formatting.
- Integrated `lucide-react` for add/remove icons.
- Handles read-only fields and editable metadata separately.

## 2026-02-14: Implementation Complete

### Summary
Successfully implemented the Storage UI feature with all components.

### Architecture
- **Feature-based structure**: `src/features/storage/` with api/, hooks/, components/, pages/ subdirectories
- **Clean separation**: API layer, hooks for state management, presentational components

### Key Decisions
1. Used native fetch instead of Axios (lighter weight)
2. Client-side search/filter (backend doesn't support server-side search)
3. Concurrency limit of 3 for batch uploads (implemented with queue pattern)
4. Dialog instead of AlertDialog for delete confirmation (AlertDialog not in shadcn)

### Notes
- Lint warnings exist in pre-existing shadcn/ui components (button.tsx, sidebar.tsx) - not from our implementation
- Build passes successfully
- All TypeScript compilation clean

### Backend Contract
- Max file size: 50MB
- Allowed extensions: .txt, .docx, .md, .pdf
- Auth required for all endpoints (Bearer token)
