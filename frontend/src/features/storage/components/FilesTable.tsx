import { FileText, File, FileCode, FileIcon } from 'lucide-react'
import { format } from 'date-fns'
import { filesize } from 'filesize'
import { cn } from '@/lib/utils'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Skeleton } from '@/components/ui/skeleton'
import type { NormalizedFileItem } from '../api/storage-api'

interface FilesTableProps {
  files: NormalizedFileItem[]
  isLoading?: boolean
  renderActions: (file: NormalizedFileItem) => React.ReactNode
  className?: string
}

// Map extensions to icons
const extensionIcons: Record<string, typeof FileText> = {
  '.pdf': FileText,
  '.txt': FileText,
  '.md': FileCode,
  '.docx': File,
}

function getFileIcon(filename: string | undefined): typeof FileText {
  if (!filename) return FileIcon
  const dotIndex = filename.lastIndexOf('.')
  if (dotIndex === -1 || dotIndex === 0) return FileIcon
  const ext = filename.slice(dotIndex).toLowerCase()
  return extensionIcons[ext] || FileIcon
}

function formatFileSize(bytes: number | undefined): string {
  if (bytes === undefined || bytes === null || isNaN(bytes)) return '-'
  return String(filesize(bytes))
}

function formatDate(dateString: string | undefined): string {
  if (!dateString) return '-'
  try {
    return format(new Date(dateString), 'MMM d, yyyy')
  } catch {
    return dateString
  }
}

export function FilesTable({ files, isLoading, renderActions, className }: FilesTableProps) {
  if (isLoading) {
    return (
      <div className={cn('space-y-3', className)}>
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="flex items-center gap-4 p-4 border rounded-lg">
            <Skeleton className="h-10 w-10 rounded" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-3 w-1/4" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  if (files.length === 0) {
    return (
      <div className={cn('text-center py-12', className)}>
        <FileIcon className="mx-auto h-12 w-12 text-muted-foreground/50" />
        <h3 className="mt-4 text-lg font-medium">No files</h3>
        <p className="mt-2 text-sm text-muted-foreground">
          Upload files to get started
        </p>
      </div>
    )
  }

  return (
    <div className={cn('rounded-md border', className)}>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-12"></TableHead>
            <TableHead>Name</TableHead>
            <TableHead className="w-24">Size</TableHead>
            <TableHead className="w-32">Date</TableHead>
            <TableHead className="w-20"></TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {files.map((file) => {
            const Icon = getFileIcon(file.filename)
            return (
              <TableRow key={file.id}>
                <TableCell>
                  <div className="flex items-center justify-center">
                    <Icon className="h-5 w-5 text-muted-foreground" />
                  </div>
                </TableCell>
                <TableCell>
                  <span className="font-medium truncate max-w-xs block">
                    {file.filename}
                  </span>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {formatFileSize(file.size)}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {formatDate(file.uploadedAt)}
                </TableCell>
                <TableCell>
                  {renderActions(file)}
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}
