import { useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { FileSelector } from "@/features/storage/components/FileSelector"
import { useCreateItemFromUpload } from "../hooks/useItems"
import { Loader2 } from "lucide-react"
import { NormalizedFileItem } from "@/features/storage/api/storage-api"

interface FileLinkDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  folderId: string
  onSuccess?: () => void
}

export function FileLinkDialog({ open, onOpenChange, folderId, onSuccess }: FileLinkDialogProps) {
  const createItem = useCreateItemFromUpload()
  
  const [selectedFile, setSelectedFile] = useState<NormalizedFileItem | null>(null)
  const [title, setTitle] = useState("")
  const [tags, setTags] = useState("")

  const handleSelectionChange = (files: NormalizedFileItem[]) => {
    if (files.length > 0) {
      const file = files[0]
      setSelectedFile(file)
      if (!title) {
        setTitle(file.filename)
      }
    } else {
      setSelectedFile(null)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedFile) return

    // Convert comma-separated string to JSON array string
    const tagsArray = tags.split(',').map(t => t.trim()).filter(Boolean)
    const jsonTags = JSON.stringify(tagsArray)

    try {
      await createItem.mutateAsync({
        folderId,
        uploadId: selectedFile.id,
        title: title || selectedFile.filename,
        tags: jsonTags
      })
      
      onSuccess?.()
      onOpenChange(false)
      setSelectedFile(null)
      setTitle("")
      setTags("")
    } catch (error) {
      console.error("Failed to link file:", error)
    }
  }

  const isSubmitting = createItem.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[800px] flex flex-col max-h-[90vh]">
        <DialogHeader>
          <DialogTitle>Link File</DialogTitle>
          <DialogDescription>
            Select a file from your storage to add to this folder.
          </DialogDescription>
        </DialogHeader>
        
        <div className="flex-1 min-h-0 overflow-y-auto py-4 space-y-4">
          <div className="border rounded-md p-4 min-h-[300px]">
             <FileSelector 
               onSelectionChange={handleSelectionChange} 
               multiSelect={false}
               className="w-full"
             />
          </div>
          
          {selectedFile && (
            <div className="space-y-4 border-t pt-4">
              <div className="space-y-2">
                <Label htmlFor="file-title">Title</Label>
                <Input 
                  id="file-title" 
                  value={title} 
                  onChange={(e) => setTitle(e.target.value)} 
                  placeholder="Item title"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="file-tags">Tags</Label>
                <Input 
                  id="file-tags" 
                  value={tags} 
                  onChange={(e) => setTags(e.target.value)} 
                  placeholder="comma, separated, tags"
                />
              </div>
            </div>
          )}
        </div>

        <DialogFooter className="mt-auto pt-2">
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" onClick={handleSubmit} disabled={isSubmitting || !selectedFile}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isSubmitting ? "Linking..." : "Link File"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
