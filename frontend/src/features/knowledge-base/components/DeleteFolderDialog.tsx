import { useEffect, useState } from 'react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { getFolderContentSummary } from '../api/knowledge-base-api'
import { useDeleteFolder } from '../hooks/useFolders'
import { FolderContentSummary } from '../types'

interface DeleteFolderDialogProps {
  folderId: string | null
  folderName: string
  open: boolean
  onOpenChange: (open: boolean) => void
  onDeleted: () => void
}

export function DeleteFolderDialog({
  folderId,
  folderName,
  open,
  onOpenChange,
  onDeleted,
}: DeleteFolderDialogProps) {
  const [summary, setSummary] = useState<FolderContentSummary | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  
  const { mutate: deleteFolder, isPending: isDeleting } = useDeleteFolder()

  useEffect(() => {
    if (open && folderId) {
      setIsLoading(true)
      getFolderContentSummary(folderId)
        .then((data) => {
          setSummary(data)
        })
        .catch((err) => {
          console.error('Failed to get folder summary:', err)
        })
        .finally(() => {
          setIsLoading(false)
        })
    } else {
      setSummary(null)
    }
  }, [open, folderId])

  const handleDelete = () => {
    if (!folderId) return
    deleteFolder(
      { id: folderId, force: true },
      {
        onSuccess: () => {
          onDeleted()
          onOpenChange(false)
        },
      }
    )
  }

  // This dialog only opens when the folder has content (parent checks hasContent).
  // If somehow opened for empty folder, there's a fallback message.
  
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete folder?</AlertDialogTitle>
          <AlertDialogDescription>
            {isLoading ? (
              'Loading folder contents...'
            ) : summary?.hasContent ? (
              <>
                Folder "{folderName}" contains {summary.itemCount} items and {summary.subfolderCount} subfolders. Are you sure you want to delete it? Files will remain in storage.
              </>
            ) : (
              // This is a fallback just in case the dialog is opened for an empty folder
              `Are you sure you want to delete folder "${folderName}"?`
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              handleDelete()
            }}
            disabled={isDeleting || isLoading}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {isDeleting ? 'Deleting...' : 'Delete'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
