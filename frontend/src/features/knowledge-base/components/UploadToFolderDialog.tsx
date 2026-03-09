import { useState, useRef } from "react"
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
import { Textarea } from "@/components/ui/textarea"
import { useUploadToFolder } from "../hooks/useItems"
import { Loader2 } from "lucide-react"

interface UploadToFolderDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  folderId: string
  onSuccess?: () => void
}

const MAX_FILE_SIZE = 50 * 1024 * 1024 // 50MB
const ALLOWED_EXTENSIONS = ['.txt', '.docx', '.md', '.pdf']

export function UploadToFolderDialog({ open, onOpenChange, folderId, onSuccess }: UploadToFolderDialogProps) {
  const uploadToFolder = useUploadToFolder()
  
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [title, setTitle] = useState("")
  const [metadataText, setMetadataText] = useState("{}")
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setError(null)
    const file = e.target.files?.[0]
    if (!file) {
      setSelectedFile(null)
      return
    }

    const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase()
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      setError(`Invalid file type. Allowed: ${ALLOWED_EXTENSIONS.join(', ')}`)
      setSelectedFile(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
      return
    }

    if (file.size > MAX_FILE_SIZE) {
      setError(`File size exceeds 50MB limit.`)
      setSelectedFile(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
      return
    }

    setSelectedFile(file)
    if (!title) {
      setTitle(file.name)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedFile) return
    setError(null)

    let normalizedMetadata = "{}"
    if (metadataText.trim()) {
      try {
        const parsed = JSON.parse(metadataText)
        normalizedMetadata = JSON.stringify(parsed)
      } catch {
        setError("Metadata must be valid JSON")
        return
      }
    }

    try {
      await uploadToFolder.mutateAsync({
        file: selectedFile,
        folderId,
        title: title || selectedFile.name,
        metadata: normalizedMetadata !== "{}" ? normalizedMetadata : undefined
      })
      
      onSuccess?.()
      onOpenChange(false)
      setSelectedFile(null)
      setTitle("")
      setMetadataText("{}")
      setError(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
    } catch (err) {
      console.error("Failed to upload file:", err)
      setError("Failed to upload file")
    }
  }

  const isSubmitting = uploadToFolder.isPending

  // Reset state when closing
  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      setSelectedFile(null)
      setTitle("")
      setMetadataText("{}")
      setError(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[800px] flex flex-col max-h-[90vh]">
        <DialogHeader>
          <DialogTitle>Upload File</DialogTitle>
          <DialogDescription>
            Upload a file from your computer to add to this folder.
          </DialogDescription>
        </DialogHeader>
        
        <div className="flex-1 min-h-0 py-4 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="file-upload">Select File</Label>
            <Input 
              id="file-upload" 
              type="file" 
              ref={fileInputRef}
              onChange={handleFileChange}
              accept=".txt,.docx,.md,.pdf"
              className="cursor-pointer"
            />
            {error && <p className="text-sm text-destructive">{error}</p>}
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
                <Label htmlFor="file-metadata">Metadata (JSON)</Label>
                <Textarea 
                  id="file-metadata" 
                  value={metadataText} 
                  onChange={(e) => setMetadataText(e.target.value)} 
                  placeholder='{"field-1":"value","field-2":["value1","value2"]}'
                  className="min-h-[120px] font-mono resize-y"
                />
              </div>
            </div>
          )}
        </div>

        <DialogFooter className="mt-auto pt-2">
          <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" onClick={handleSubmit} disabled={isSubmitting || !selectedFile}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isSubmitting ? "Uploading..." : "Upload File"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
