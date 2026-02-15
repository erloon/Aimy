import React, { useCallback, useState } from 'react'
import { Upload, FileText } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { useToast } from '@/hooks/use-toast'

const ALLOWED_EXTENSIONS = ['.txt', '.docx', '.md', '.pdf']
const MAX_FILE_SIZE = 50 * 1024 * 1024 // 50MB

interface UploadDropzoneProps {
  onFilesSelected: (files: File[]) => void
  className?: string
}

export function UploadDropzone({ onFilesSelected, className }: UploadDropzoneProps) {
  const [isDragOver, setIsDragOver] = useState(false)
  const { toast } = useToast()

  const validateFiles = useCallback((files: File[]): File[] => {
    const validFiles: File[] = []
    const errors: string[] = []

    files.forEach(file => {
      // Check file size
      if (file.size > MAX_FILE_SIZE) {
        errors.push(`${file.name}: File size exceeds 50MB limit`)
        return
      }

      // Check file extension
      const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
      if (!ALLOWED_EXTENSIONS.includes(ext)) {
        errors.push(`${file.name}: File type not allowed. Allowed: ${ALLOWED_EXTENSIONS.join(', ')}`)
        return
      }

      validFiles.push(file)
    })

    // Show error toast for invalid files
    if (errors.length > 0) {
      toast({
        title: 'Invalid files',
        description: errors.join('\n'),
        variant: 'destructive'
      })
    }

    return validFiles
  }, [toast])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragOver(false)

    const files = Array.from(e.dataTransfer.files)
    const validFiles = validateFiles(files)
    
    if (validFiles.length > 0) {
      onFilesSelected(validFiles)
    }
  }, [validateFiles, onFilesSelected])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragOver(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragOver(false)
  }, [])

  const handleFileInput = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || [])
    const validFiles = validateFiles(files)
    
    if (validFiles.length > 0) {
      onFilesSelected(validFiles)
    }
    
    // Reset input so same file can be selected again
    e.target.value = ''
  }, [validateFiles, onFilesSelected])

  return (
    <div
      className={cn(
        'relative border-2 border-dashed rounded-lg p-8 text-center transition-colors',
        isDragOver 
          ? 'border-primary bg-primary/5' 
          : 'border-muted-foreground/25 hover:border-muted-foreground/50',
        className
      )}
      onDrop={handleDrop}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
    >
      <input
        type="file"
        multiple
        accept={ALLOWED_EXTENSIONS.join(',')}
        onChange={handleFileInput}
        className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
      />
      
      <div className="flex flex-col items-center gap-4">
        <div className={cn(
          'p-4 rounded-full transition-colors',
          isDragOver ? 'bg-primary/10' : 'bg-muted'
        )}>
          {isDragOver ? (
            <Upload className="h-8 w-8 text-primary" />
          ) : (
            <FileText className="h-8 w-8 text-muted-foreground" />
          )}
        </div>
        
        <div>
          <p className="text-lg font-medium">
            {isDragOver ? 'Drop files here' : 'Drag & drop files here'}
          </p>
          <p className="text-sm text-muted-foreground mt-1">
            or click to browse
          </p>
        </div>
        
        <Button type="button" variant="secondary" className="mt-2">
          <Upload className="h-4 w-4 mr-2" />
          Select Files
        </Button>
        
        <p className="text-xs text-muted-foreground">
          Allowed: {ALLOWED_EXTENSIONS.join(', ')} • Max 50MB
        </p>
      </div>
    </div>
  )
}
