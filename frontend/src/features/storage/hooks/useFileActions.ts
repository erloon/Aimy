import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteFile, updateMetadata, NormalizedFileItem } from '../api/storage-api'

interface UseDeleteFileOptions {
  onSuccess?: () => void
  onError?: (error: Error) => void
}

export function useDeleteFile(options: UseDeleteFileOptions = {}) {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: (id: string) => deleteFile(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['files'] })
      options.onSuccess?.()
    },
    onError: (error) => {
      options.onError?.(error as Error)
    }
  })
}

interface UseUpdateMetadataOptions {
  onSuccess?: (data: NormalizedFileItem) => void
  onError?: (error: Error) => void
}

export function useUpdateMetadata(options: UseUpdateMetadataOptions = {}) {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ id, metadata }: { id: string; metadata: Record<string, unknown> }) => 
      updateMetadata(id, metadata),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['files'] })
      options.onSuccess?.(data)
    },
    onError: (error) => {
      options.onError?.(error as Error)
    }
  })
}
