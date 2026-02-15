import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createItemFromUpload,
  createNote,
  deleteItem,
  getItem,
  searchItems,
  updateItem,
} from '../api/knowledge-base-api'
import type {
  CreateItemFromUploadRequest,
  CreateNoteRequest,
  ItemSearchRequest,
  KnowledgeItem,
  PagedResult,
  UpdateItemRequest,
} from '../types'

export function useItems(searchParams: ItemSearchRequest) {
  return useQuery<PagedResult<KnowledgeItem>>({
    queryKey: ['items', searchParams],
    queryFn: () => searchItems(searchParams),
  })
}

export function useItem(id: string) {
  return useQuery<KnowledgeItem>({
    queryKey: ['item', id],
    queryFn: () => getItem(id),
    enabled: !!id,
  })
}

export function useCreateNote() {
  const queryClient = useQueryClient()

  return useMutation<KnowledgeItem, Error, CreateNoteRequest>({
    mutationFn: createNote,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['items'] })
    },
  })
}

export function useCreateItemFromUpload() {
  const queryClient = useQueryClient()

  return useMutation<KnowledgeItem, Error, CreateItemFromUploadRequest>({
    mutationFn: createItemFromUpload,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['items'] })
    },
  })
}

export function useUpdateItem() {
  const queryClient = useQueryClient()

  return useMutation<KnowledgeItem, Error, { id: string; request: UpdateItemRequest }>({
    mutationFn: ({ id, request }) => updateItem(id, request),
    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({ queryKey: ['items'] })
      await queryClient.invalidateQueries({ queryKey: ['item', variables.id] })
    },
  })
}

export function useDeleteItem() {
  const queryClient = useQueryClient()

  return useMutation<void, Error, string>({
    mutationFn: deleteItem,
    onSuccess: async (_, id) => {
      await queryClient.invalidateQueries({ queryKey: ['items'] })
      await queryClient.invalidateQueries({ queryKey: ['item', id] })
    },
  })
}
