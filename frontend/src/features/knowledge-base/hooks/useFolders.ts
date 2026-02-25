import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createFolder,
  deleteFolder,
  getFolderTree,
  moveFolder,
  updateFolder,
} from '../api/knowledge-base-api'
import type {
  CreateFolderRequest,
  Folder,
  FolderTreeResponse,
  MoveFolderRequest,
  UpdateFolderRequest,
} from '../types'

export function useFolderTree() {
  return useQuery<FolderTreeResponse>({
    queryKey: ['folderTree'],
    queryFn: getFolderTree,
  })
}

export function useCreateFolder() {
  const queryClient = useQueryClient()

  return useMutation<Folder, Error, CreateFolderRequest>({
    mutationFn: createFolder,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['folderTree'] })
    },
  })
}

export function useUpdateFolder() {
  const queryClient = useQueryClient()

  return useMutation<Folder, Error, { id: string; request: UpdateFolderRequest }>({
    mutationFn: ({ id, request }) => updateFolder(id, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['folderTree'] })
    },
  })
}

export function useMoveFolder() {
  const queryClient = useQueryClient()

  return useMutation<Folder, Error, { id: string; request: MoveFolderRequest }>({
    mutationFn: ({ id, request }) => moveFolder(id, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['folderTree'] })
    },
  })
}

export function useDeleteFolder() {
  const queryClient = useQueryClient()

  return useMutation<void, Error, { id: string; force?: boolean }>({
    mutationFn: ({ id, force }) => deleteFolder(id, force),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['folderTree'] })
      await queryClient.invalidateQueries({ queryKey: ['items'] })
    },
  })
}
