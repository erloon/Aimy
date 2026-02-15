import { useQuery } from '@tanstack/react-query'
import { useState, useMemo } from 'react'
import { getFiles, NormalizedFileItem, NormalizedListFilesResponse } from '../api/storage-api'

interface UseFilesOptions {
  page?: number
  pageSize?: number
}

interface UseFilesReturn {
  files: NormalizedFileItem[]
  total: number
  isLoading: boolean
  error: Error | null
  page: number
  pageSize: number
  setPage: (page: number) => void
  // Client-side filter
  searchQuery: string
  setSearchQuery: (query: string) => void
  filteredFiles: NormalizedFileItem[]
}

export function useFiles(options: UseFilesOptions = {}): UseFilesReturn {
  const initialPage = options.page ?? 1
  const initialPageSize = options.pageSize ?? 10

  const [page, setPage] = useState(initialPage)
  const [pageSize] = useState(initialPageSize)
  const [searchQuery, setSearchQuery] = useState('')

  const { data, isLoading, error } = useQuery<NormalizedListFilesResponse>({
    queryKey: ['files', page, pageSize],
    queryFn: () => getFiles({ page, pageSize }),
  })

  // Client-side filtering by filename
  const filteredFiles = useMemo(() => {
    if (!data?.items || !searchQuery.trim()) {
      return data?.items ?? []
    }

    const query = searchQuery.toLowerCase()
    return data.items.filter(file =>
      file.filename.toLowerCase().includes(query) ||
      (file.metadata && file.metadata.toLowerCase().includes(query))
    )
  }, [data?.items, searchQuery])

  return {
    files: data?.items ?? [],
    total: data?.total ?? 0,
    isLoading,
    error: error as Error | null,
    page,
    pageSize,
    setPage,
    searchQuery,
    setSearchQuery,
    filteredFiles,
  }
}
