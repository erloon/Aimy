import { useState } from 'react'
import { Search, Upload as UploadIcon } from 'lucide-react'
import { useFiles } from '../hooks/useFiles'
import { useUpload } from '../hooks/useUpload'
import { useDeleteFile, useUpdateMetadata } from '../hooks/useFileActions'
import { downloadFile, type NormalizedFileItem } from '../api/storage-api'
import { UploadDropzone } from '../components/UploadDropzone'
import { UploadProgress } from '../components/UploadProgress'
import { FilesTable } from '../components/FilesTable'
import { FileActionsMenu } from '../components/FileActionsMenu'
import { MetadataSheet } from '../components/MetadataSheet'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useToast } from '@/hooks/use-toast'

export function StoragePage() {
  const [showDropzone, setShowDropzone] = useState(false)
  const [selectedFile, setSelectedFile] = useState<NormalizedFileItem | null>(null)
  const [showMetadataSheet, setShowMetadataSheet] = useState(false)
  const [fileToDelete, setFileToDelete] = useState<NormalizedFileItem | null>(null)

  const { toast } = useToast()

  // Files hook
  const {
    filteredFiles,
    total,
    isLoading,
    error,
    page,
    pageSize,
    setPage,
    searchQuery,
    setSearchQuery
  } = useFiles()

  // Upload hook
  const {
    tasks: uploadTasks,
    // isUploading is available but not used directly here, tasks length check is sufficient for UI
    uploadFiles,
    retryTask,
    removeTask,
    clearCompleted
  } = useUpload()

  // Delete hook
  const deleteMutation = useDeleteFile({
    onSuccess: () => {
      toast({ title: 'File deleted', description: 'The file has been removed.' })
      setFileToDelete(null)
    },
    onError: (error) => {
      toast({ title: 'Delete failed', description: error.message, variant: 'destructive' })
    }
  })

  // Update metadata hook
  const updateMetadataMutation = useUpdateMetadata({
    onSuccess: () => {
      toast({ title: 'Metadata updated', description: 'Changes have been saved.' })
      setShowMetadataSheet(false)
    },
    onError: (error) => {
      toast({ title: 'Update failed', description: error.message, variant: 'destructive' })
    }
  })

  // Handle file selection for upload
  const handleFilesSelected = (files: File[]) => {
    uploadFiles(files)
    setShowDropzone(false)
  }

  // Handle download
  const handleDownload = async (file: NormalizedFileItem) => {
    try {
      const blob = await downloadFile(file.id)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = file.filename
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
      toast({ title: 'Download started', description: file.filename })
    } catch (error) {
      toast({
        title: 'Download failed',
        description: error instanceof Error ? error.message : 'Unknown error',
        variant: 'destructive'
      })
    }
  }

  // Handle edit details
  const handleEditDetails = (file: NormalizedFileItem) => {
    setSelectedFile(file)
    setShowMetadataSheet(true)
  }

  // Handle delete request
  const handleDeleteRequest = (file: NormalizedFileItem) => {
    setFileToDelete(file)
  }

  // Confirm delete
  const handleConfirmDelete = () => {
    if (fileToDelete) {
      deleteMutation.mutate(fileToDelete.id)
    }
  }

  // Handle metadata save
  const handleSaveMetadata = (fileId: string, metadata: Record<string, string>) => {
    updateMetadataMutation.mutate({ id: fileId, metadata })
  }

  // Render actions for table
  const renderActions = (file: NormalizedFileItem) => (
    <FileActionsMenu
      file={file}
      onDownload={handleDownload}
      onEditDetails={handleEditDetails}
      onDelete={handleDeleteRequest}
    />
  )

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Storage</h1>
          <p className="text-muted-foreground">Manage your files and documents</p>
        </div>
        <Button onClick={() => setShowDropzone(!showDropzone)}>
          <UploadIcon className="h-4 w-4 mr-2" />
          Upload Files
        </Button>
      </div>

      {/* Upload Dropzone (toggleable) */}
      {showDropzone && (
        <UploadDropzone onFilesSelected={handleFilesSelected} />
      )}

      {/* Upload Progress */}
      {uploadTasks.length > 0 && (
        <UploadProgress
          tasks={uploadTasks}
          onRetry={retryTask}
          onRemove={removeTask}
          onClearCompleted={clearCompleted}
        />
      )}

      {/* Search and Filters */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search files..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9"
          />
        </div>
        <div className="text-sm text-muted-foreground">
          {total} file{total !== 1 ? 's' : ''}
        </div>
      </div>

      {/* Error state */}
      {error && (
        <div className="text-center py-8 text-destructive">
          <p>Error loading files: {error.message}</p>
        </div>
      )}

      {/* Files Table */}
      <FilesTable
        files={filteredFiles}
        isLoading={isLoading}
        renderActions={renderActions}
      />

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={page === 1}
            onClick={() => setPage(page - 1)}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {page} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage(page + 1)}
          >
            Next
          </Button>
        </div>
      )}

      {/* Metadata Sheet */}
      <MetadataSheet
        file={selectedFile}
        open={showMetadataSheet}
        onOpenChange={setShowMetadataSheet}
        onSave={handleSaveMetadata}
        isSaving={updateMetadataMutation.isPending}
      />

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!fileToDelete} onOpenChange={(open) => !open && setFileToDelete(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete File</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{fileToDelete?.filename}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setFileToDelete(null)}>Cancel</Button>
            <Button
              variant="destructive"
              onClick={handleConfirmDelete}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
