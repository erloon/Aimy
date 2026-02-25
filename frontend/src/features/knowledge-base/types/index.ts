// Item types
export type KnowledgeItemType = 'File' | 'Note'

export interface KnowledgeItem {
  id: string
  folderId: string
  folderName?: string | null
  title: string
  itemType: KnowledgeItemType
  content: string | null
  metadata: string | null
  sourceUploadId: string | null
  sourceUploadFileName?: string | null
  sourceUploadMetadata?: string | null
  createdAt: string
  updatedAt: string
  sourceMarkdown?: string | null
  summary?: string | null
  chunkCount?: number | null
  chunks?: UploadChunkResponse[] | null
}

export interface UploadChunkResponse {
  id: string
  chunkIndex: number
  content: string
  summary?: string | null
  context?: string | null
  tokenCount?: number | null
}

// Folder types
export interface Folder {
  id: string
  knowledgeBaseId: string
  parentFolderId: string | null
  name: string
  createdAt: string
  updatedAt: string
}

export interface FolderTreeNode {
  id: string
  name: string
  children: FolderTreeNode[]
}

export interface FolderTreeResponse {
  rootFolders: FolderTreeNode[]
}
export interface FolderContentSummary {
  itemCount: number
  subfolderCount: number
  hasContent: boolean
}



// Request types
export interface CreateFolderRequest {
  name: string
  parentFolderId?: string | null
}

export interface UpdateFolderRequest {
  name: string
}

export interface MoveFolderRequest {
  newParentFolderId: string | null
}

export interface CreateNoteRequest {
  folderId: string
  title: string
  content?: string
  metadata?: string
}

export interface CreateItemFromUploadRequest {
  folderId: string
  uploadId: string
  title?: string
  metadata?: string
}

export interface UpdateItemRequest {
  title?: string
  content?: string
  metadata?: string
  folderId?: string
}

export interface ItemSearchRequest {
  folderId?: string
  includeSubFolders?: boolean
  search?: string
  metadata?: string
  type?: KnowledgeItemType
  page?: number
  pageSize?: number
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface SemanticSearchResult {
  id: string
  title: string
  itemType: KnowledgeItemType
  content: string | null
  metadata: string | null
  folderName: string | null
  sourceUploadId: string | null
  sourceUploadFileName: string | null
  score: number
  createdAt: string
  updatedAt: string
}
