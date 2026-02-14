const API_BASE = '/api'

interface FetchOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public statusText: string,
    message: string
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

export async function fetchClient<T>(
  endpoint: string,
  options: FetchOptions = {}
): Promise<T> {
  const token = localStorage.getItem('authToken')
  
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  }
  
  // Don't set Content-Type for FormData - browser sets it with boundary
  if (!(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json'
  }
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  
  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
    body: options.body instanceof FormData 
      ? options.body 
      : options.body ? JSON.stringify(options.body) : undefined
  })
  
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}))
    throw new ApiError(
      response.status,
      response.statusText,
      errorData.error || errorData.message || 'An error occurred'
    )
  }
  
  // Handle empty responses (204 No Content)
  if (response.status === 204) {
    return {} as T
  }
  
  return response.json()
}
