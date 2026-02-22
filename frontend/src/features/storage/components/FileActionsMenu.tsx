import { MoreHorizontal, Download, Pencil, Trash2, Eye } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'
import type { NormalizedFileItem } from '../api/storage-api'

interface FileActionsMenuProps {
  file: NormalizedFileItem
  onDownload: (file: NormalizedFileItem) => void
  onEditDetails: (file: NormalizedFileItem) => void
  onDelete: (file: NormalizedFileItem) => void
  onView?: (file: NormalizedFileItem) => void
}

export function FileActionsMenu({ file, onDownload, onEditDetails, onDelete, onView }: FileActionsMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <MoreHorizontal className="h-4 w-4" />
          <span className="sr-only">Open menu</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {onView && (
          <DropdownMenuItem onClick={() => onView(file)}>
            <Eye className="mr-2 h-4 w-4" />
            View
          </DropdownMenuItem>
        )}
        <DropdownMenuItem onClick={() => onDownload(file)}>
          <Download className="mr-2 h-4 w-4" />
          Download
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => onEditDetails(file)}>
          <Pencil className="mr-2 h-4 w-4" />
          Edit Details
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={() => onDelete(file)}
          className="text-destructive focus:text-destructive"
        >
          <Trash2 className="mr-2 h-4 w-4" />
          Delete
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
