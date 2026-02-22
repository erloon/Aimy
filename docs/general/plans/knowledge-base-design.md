# Knowledge Base Design (Hybrid: Folders + Tags)

## Goal
Create a knowledge base for logged-in users with **folders as the source of truth**, tags as cross-cutting categories, and support for semantic search for both users and AI agents. The MVP should support existing uploads and ad-hoc Markdown notes.

## MVP Scope
- **One knowledge base per user** (no sharing).
- **Folders as the source of truth**: every item must belong to exactly one folder.
- **Tags as cross-sections**: facilitate filtering and searching.
- Item Types:
  - `file` → linked to an existing Upload.
  - `note` → automatic upload of a `.md` file.
- UI:
  - Folder tree.
  - List of items within a folder.
  - **“New Note”** button in the folder context.
  - Markdown editor (minimal, a text area is sufficient).
- Search:
  - Filters: folder, tags.
  - Semantics: content + metadata.

## User Mental Model
- I think of the knowledge base as a repository.
- Folders reflect projects/areas/topics.
- An item is always in **one** folder.
- Tags are just cross-sections (e.g., #security, #howto).
- Semantic search works on content and metadata (folder, tags, type).

## Conceptual Data Model

### KnowledgeBase
- One per user.
- Owner: user.

### Folder
- Folder tree (`parentId`).
- Belongs to a KnowledgeBase.
- The folder is the **canonical** location of an item.

### KnowledgeItem (Base Type)
- `type`: `file` | `note` (extensible: `link`, `video`).
- `folderId`: required.
- `title`: name of the item.
- `tags`: list of tags (or relationship).
- `sourceRef`: points to the source (e.g., Upload).
- `metadata`: JSON with type-specific fields.
- `createdBy`, `createdAt`, `updatedAt`.

### ItemContent (Logical Component)
- Content for the semantic index (e.g., text from a .md file).
- Can be versioned independently of the UI.

## Item Types and Metadata

### file
- `sourceRef.uploadId`
- `metadata`: `{ fileId, mimeType, sizeBytes }`

### note
- `sourceRef.uploadId` (auto-upload `.md`)
- `metadata`: `{ editorVersion, wordCount, excerpt }`

> Extensibility:
> - `link`: `{ url, domain, lastFetched }`
> - `video`: `{ url, durationSec, transcriptStatus }`

## Relationship with Existing Upload
- The knowledge base **does not duplicate storage**.
- A `file` item points to an existing Upload.
- A `note` item creates a `.md` file, which is **uploaded** and linked identically.

## User Flows

### 1) Creating Folders
- User creates folders and subfolders.
- Moving an item = changing the folder.

### 2) Adding a File to the Knowledge Base
- Selecting an existing upload.
- Setting folder, tags, and title.
- Creates a `KnowledgeItem` of type `file`.

### 3) New Note (Markdown)
- Clicking **“New Note”** within a folder.
- Entering content.
- System creates a `.md` file, **automatically uploads it**, and creates a `KnowledgeItem` of type `note`.

### 4) Searching
- Filters: folder, tags.
- Semantics: ranking by content and metadata.
- Results in a uniform format regardless of type.

## Semantic Search — Principles
- Index content (`ItemContent`) + metadata (folder, tags, type).
- Filters limit the search space before vector ranking.
- This ensures consistent behavior for the user and AI agents.

## UI — Minimal Scope
- Left panel: folder tree.
- Top: search bar + filters (tags, type).
- Main list: items in the folder.
- Details panel: metadata, tags, source.
- **“New Note”** button in the folder context.

## Risks and Decisions
- Folder as the source of truth simplifies structure and prevents disorder.
- No sharing eliminates ACL complexity in MVP.
- Maintaining `.md` as an Upload ensures consistency with existing functionality.

## Open Topics (For Later)
- Multi-base per user.
- Permissions per folder.
- Automatic content extraction from non-.md files.
- Note versioning.
