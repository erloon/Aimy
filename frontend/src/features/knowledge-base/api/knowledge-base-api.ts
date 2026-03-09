import { fetchClient } from '@/lib/api-client'
import type {
  Folder,
  FolderTreeResponse,
  CreateFolderRequest,
  UpdateFolderRequest,
  MoveFolderRequest,
  KnowledgeItem,
  CreateNoteRequest,
  CreateItemFromUploadRequest,
  UpdateItemRequest,
  ItemSearchRequest,
  PagedResult,
  SemanticSearchResult,
  FolderContentSummary,
} from '../types'

export interface MetadataKeyDefinition {
  key: string
  label: string
  type: string
  filterable: boolean
  allowFreeText: boolean
  required: boolean
  policy: 'Strict' | 'Permissive'
}

export interface MetadataKeysResponse {
  items: MetadataKeyDefinition[]
}

export interface MetadataValueSuggestionItem {
  value: string
  label: string
  aliases: string[]
  matchType: string
}

export interface MetadataValueSuggestionsResponse {
  key: string
  items: MetadataValueSuggestionItem[]
}

export interface MetadataNormalizeWarning {
  key: string
  message: string
  inputValue?: string
  resolvedValue?: string
  matchType: string
}

export interface MetadataNormalizeResponse {
  metadata: string | null
  hasChanges: boolean
  warnings: MetadataNormalizeWarning[]
}

// Folder APIs
export async function getFolderTree(): Promise<FolderTreeResponse> {
  return fetchClient<FolderTreeResponse>('/kb/folders/tree')
}

export async function createFolder(request: CreateFolderRequest): Promise<Folder> {
  return fetchClient<Folder>('/kb/folders', {
    method: 'POST',
    body: request,
  })
}

export async function updateFolder(id: string, request: UpdateFolderRequest): Promise<Folder> {
  return fetchClient<Folder>(`/kb/folders/${id}`, {
    method: 'PUT',
    body: request,
  })
}

export async function moveFolder(id: string, request: MoveFolderRequest): Promise<Folder> {
  return fetchClient<Folder>(`/kb/folders/${id}/move`, {
    method: 'POST',
    body: request,
  })
}

export async function getFolderContentSummary(id: string): Promise<FolderContentSummary> {
  return fetchClient<FolderContentSummary>(`/kb/folders/${id}/content-summary`)
}

export async function deleteFolder(id: string, force: boolean = false): Promise<void> {
  await fetchClient<void>(`/kb/folders/${id}${force ? '?force=true' : ''}`, {
    method: 'DELETE',
  })
}

// Item APIs
export async function searchItems(params: ItemSearchRequest): Promise<PagedResult<KnowledgeItem>> {
  const searchParams = new URLSearchParams()
  if (params.folderId) searchParams.set('folderId', params.folderId)
  if (params.includeSubFolders !== undefined) {
    searchParams.set('includeSubFolders', String(params.includeSubFolders))
  }
  if (params.search) searchParams.set('search', params.search)
  if (params.metadata) searchParams.set('metadata', params.metadata)
  if (params.type) searchParams.set('type', params.type)
  if (params.page) searchParams.set('page', String(params.page))
  if (params.pageSize) searchParams.set('pageSize', String(params.pageSize))

  const queryString = searchParams.toString()
  return fetchClient<PagedResult<KnowledgeItem>>(`/kb/items${queryString ? `?${queryString}` : ''}`)
}

export async function getItem(id: string): Promise<KnowledgeItem> {
  return fetchClient<KnowledgeItem>(`/kb/items/${id}`)
}

export async function createNote(request: CreateNoteRequest): Promise<KnowledgeItem> {
  return fetchClient<KnowledgeItem>('/kb/items/note', {
    method: 'POST',
    body: request,
  })
}

export async function createItemFromUpload(
  request: CreateItemFromUploadRequest
): Promise<KnowledgeItem> {
  return fetchClient<KnowledgeItem>('/kb/items/from-upload', {
    method: 'POST',
    body: request,
  })
}

export async function uploadToFolder(
  file: File,
  folderId: string,
  title?: string,
  metadata?: string
): Promise<KnowledgeItem> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('folderId', folderId)
  if (title) formData.append('title', title)
  if (metadata) formData.append('metadata', metadata)

  return fetchClient<KnowledgeItem>('/kb/items/upload', {
    method: 'POST',
    body: formData,
  })
}

export async function updateItem(id: string, request: UpdateItemRequest): Promise<KnowledgeItem> {
  return fetchClient<KnowledgeItem>(`/kb/items/${id}`, {
    method: 'PUT',
    body: request,
  })
}

export async function deleteItem(id: string): Promise<void> {
  await fetchClient<void>(`/kb/items/${id}`, {
    method: 'DELETE',
  })
}

export async function semanticSearch(query: string): Promise<SemanticSearchResult[]> {
  const searchParams = new URLSearchParams()
  searchParams.set('query', query)
  return fetchClient<SemanticSearchResult[]>(`/kb/search?${searchParams.toString()}`)
}

export async function getMetadataKeys(): Promise<MetadataKeyDefinition[]> {
  const result = await fetchClient<MetadataKeysResponse>('/kb/metadata/keys')
  return result.items
}

export async function getMetadataValues(key: string, query?: string): Promise<MetadataValueSuggestionsResponse> {
  const params = new URLSearchParams({ key })
  if (query) {
    params.set('q', query)
  }

  return fetchClient<MetadataValueSuggestionsResponse>(`/kb/metadata/values?${params.toString()}`)
}

export async function normalizeMetadata(metadata: Record<string, unknown>): Promise<MetadataNormalizeResponse> {
  return fetchClient<MetadataNormalizeResponse>('/kb/metadata/normalize', {
    method: 'POST',
    body: {
      metadata: JSON.stringify(metadata),
      defaultPolicy: 'Permissive'
    }
  })
}
