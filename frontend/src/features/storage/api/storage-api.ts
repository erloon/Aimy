import { fetchClient } from '@/lib/api-client'

// Types
export interface FileItem {
  id: string
  fileName: string      // Backend uses fileName (PascalCase -> camelCase)
  sizeBytes: number     // Backend uses sizeBytes
  contentType: string
  uploadedAt: string
  metadata: string | null
  link: string
}

// Helper to normalize field names for components
export function normalizeFileItem(item: FileItem): NormalizedFileItem {
  return {
    id: item.id,
    filename: item.fileName,
    size: item.sizeBytes,
    contentType: item.contentType,
    uploadedAt: item.uploadedAt,
    metadata: item.metadata,
    link: item.link
  }
}

export interface NormalizedFileItem {
  id: string
  filename: string
  size: number
  contentType: string
  uploadedAt: string
  metadata: string | null
  link: string
}

export interface UploadResponse {
  id: string
  fileName: string     // Backend uses fileName
  link: string
  metadata: string | null
}

export interface ListFilesResponse {
  items: FileItem[]
  totalCount: number    // Backend uses totalCount
  page: number
  pageSize: number
  totalPages: number    // Backend also returns totalPages
}

// Normalized response for components
export interface NormalizedListFilesResponse {
  items: NormalizedFileItem[]
  total: number
  page: number
  pageSize: number
}

// API Functions
export async function getFiles(params: {
  page?: number
  pageSize?: number
}): Promise<NormalizedListFilesResponse> {
  const searchParams = new URLSearchParams()
  if (params.page) searchParams.set('page', String(params.page))
  if (params.pageSize) searchParams.set('pageSize', String(params.pageSize))
  
  const queryString = searchParams.toString()
  const response = fetchClient<ListFilesResponse>(`/uploads${queryString ? `?${queryString}` : ''}`)
  
  // Normalize the response
  const data = await response
  return {
    items: data.items.map(normalizeFileItem),
    total: data.totalCount,
    page: data.page,
    pageSize: data.pageSize
  }
}

export async function uploadFile(
  file: File,
  metadata?: Record<string, unknown>
): Promise<UploadResponse> {
  const formData = new FormData()
  formData.append('file', file)
  if (metadata) {
    formData.append('metadata', JSON.stringify(metadata))
  }
  
  return fetchClient<UploadResponse>('/upload', {
    method: 'POST',
    body: formData
  })
}

export async function deleteFile(id: string): Promise<void> {
  await fetchClient<void>(`/uploads/${id}`, {
    method: 'DELETE'
  })
}

export async function downloadFile(id: string): Promise<Blob> {
  const token = localStorage.getItem('authToken')
  const response = await fetch(`/api/uploads/${id}/download`, {
    headers: {
      ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    }
  })
  
  if (!response.ok) {
    throw new Error('Download failed')
  }
  
  return response.blob()
}

export async function updateMetadata(
  id: string,
  metadata: Record<string, unknown>
): Promise<NormalizedFileItem> {
  const result = fetchClient<FileItem>(`/uploads/${id}/metadata`, {
    method: 'PATCH',
    body: { metadata: JSON.stringify(metadata) }
  })
  return normalizeFileItem(await result)
}
