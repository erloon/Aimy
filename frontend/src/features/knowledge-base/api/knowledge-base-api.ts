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
} from '../types'

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

export async function deleteFolder(id: string): Promise<void> {
  await fetchClient<void>(`/kb/folders/${id}`, {
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
