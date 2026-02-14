import { useState, useCallback, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { uploadFile, UploadResponse } from '../api/storage-api'

export type UploadStatus = 'pending' | 'uploading' | 'success' | 'error'

export interface UploadTask {
  id: string
  file: File
  status: UploadStatus
  progress: number
  error?: string
  response?: UploadResponse
}

const MAX_CONCURRENT_UPLOADS = 3
const MAX_FILE_SIZE = 50 * 1024 * 1024 // 50MB
const ALLOWED_EXTENSIONS = ['.txt', '.docx', '.md', '.pdf']

interface UseUploadReturn {
  tasks: UploadTask[]
  isUploading: boolean
  uploadFiles: (files: File[]) => void
  retryTask: (taskId: string) => void
  removeTask: (taskId: string) => void
  clearCompleted: () => void
  validateFile: (file: File) => { valid: boolean; error?: string }
}

function getFileExtension(filename: string): string {
  return filename.slice(filename.lastIndexOf('.')).toLowerCase()
}

export function useUpload(): UseUploadReturn {
  const queryClient = useQueryClient()
  const [tasks, setTasks] = useState<UploadTask[]>([])
  const activeCountRef = useRef(0)
  const queueRef = useRef<UploadTask[]>([])

  const validateFile = useCallback((file: File): { valid: boolean; error?: string } => {
    if (file.size > MAX_FILE_SIZE) {
      return { valid: false, error: `File size must not exceed 50MB` }
    }
    
    const ext = getFileExtension(file.name)
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      return { valid: false, error: `File type not allowed. Allowed: ${ALLOWED_EXTENSIONS.join(', ')}` }
    }
    
    return { valid: true }
  }, [])

  const processQueue = useCallback(async () => {
    while (activeCountRef.current < MAX_CONCURRENT_UPLOADS && queueRef.current.length > 0) {
      const task = queueRef.current.shift()
      if (!task) break

      activeCountRef.current++
      
      setTasks(prev => prev.map(t => 
        t.id === task.id ? { ...t, status: 'uploading' as UploadStatus } : t
      ))

      try {
        const response = await uploadFile(task.file)
        setTasks(prev => prev.map(t => 
          t.id === task.id ? { ...t, status: 'success' as UploadStatus, response } : t
        ))
        queryClient.invalidateQueries({ queryKey: ['files'] })
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Upload failed'
        setTasks(prev => prev.map(t => 
          t.id === task.id ? { ...t, status: 'error' as UploadStatus, error: errorMessage } : t
        ))
      } finally {
        activeCountRef.current--
        processQueue()
      }
    }
  }, [queryClient])

  const uploadFiles = useCallback((files: File[]) => {
    const newTasks: UploadTask[] = files
      .filter(file => validateFile(file).valid)
      .map(file => ({
        id: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
        file,
        status: 'pending' as UploadStatus,
        progress: 0
      }))

    if (newTasks.length === 0) return

    setTasks(prev => [...prev, ...newTasks])
    queueRef.current.push(...newTasks)
    processQueue()
  }, [validateFile, processQueue])

  const retryTask = useCallback((taskId: string) => {
    setTasks(prev => prev.map(t => 
      t.id === taskId ? { ...t, status: 'pending' as UploadStatus, error: undefined } : t
    ))
    
    const task = tasks.find(t => t.id === taskId)
    if (task) {
      queueRef.current.push({ ...task, status: 'pending' })
      processQueue()
    }
  }, [tasks, processQueue])

  const removeTask = useCallback((taskId: string) => {
    setTasks(prev => prev.filter(t => t.id !== taskId))
    queueRef.current = queueRef.current.filter(t => t.id !== taskId)
  }, [])

  const clearCompleted = useCallback(() => {
    setTasks(prev => prev.filter(t => t.status !== 'success' && t.status !== 'error'))
  }, [])

  const isUploading = tasks.some(t => t.status === 'uploading' || t.status === 'pending')

  return {
    tasks,
    isUploading,
    uploadFiles,
    retryTask,
    removeTask,
    clearCompleted,
    validateFile,
  }
}
