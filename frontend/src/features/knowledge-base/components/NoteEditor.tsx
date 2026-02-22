import { useState, useEffect } from "react"
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
import { useCreateNote, useUpdateItem } from "../hooks/useItems"
import { KnowledgeItem } from "../types"
import { Loader2 } from "lucide-react"

interface NoteEditorProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  folderId: string
  item?: KnowledgeItem
  onSuccess?: () => void
}

export function NoteEditor({ open, onOpenChange, folderId, item, onSuccess }: NoteEditorProps) {
  const createNote = useCreateNote()
  const updateItem = useUpdateItem()
  
  const [title, setTitle] = useState("")
  const [content, setContent] = useState("")
  const [metadataText, setMetadataText] = useState("{}")

  useEffect(() => {
    if (open) {
      setTitle(item?.title || "")
      setContent(item?.content || "")
      let initialMetadata = "{}"
      if (item?.metadata) {
        try {
          const parsed = JSON.parse(item.metadata)
          initialMetadata = JSON.stringify(parsed, null, 2)
        } catch {
          initialMetadata = item.metadata
        }
      }
      setMetadataText(initialMetadata)
    } else {
      // Reset form when closed if needed, but useEffect above handles it on open
    }
  }, [open, item])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    let normalizedMetadata = "{}"
    if (metadataText.trim()) {
      try {
        const parsed = JSON.parse(metadataText)
        normalizedMetadata = JSON.stringify(parsed)
      } catch {
        console.error("Metadata must be valid JSON")
        return
      }
    }

    try {
      if (item) {
        await updateItem.mutateAsync({
          id: item.id,
          request: {
            title,
            content,
            metadata: normalizedMetadata,
            folderId: item.folderId 
          }
        })
      } else {
        await createNote.mutateAsync({
          folderId,
          title,
          content,
          metadata: normalizedMetadata,
        })
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (error) {
      console.error("Failed to save note:", error)
    }
  }

  const isSubmitting = createNote.isPending || updateItem.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[625px]">
        <DialogHeader>
          <DialogTitle>{item ? "Edit Note" : "Create Note"}</DialogTitle>
          <DialogDescription>
            {item ? "Make changes to your note here." : "Add a new note to your knowledge base."}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="title">Title</Label>
            <Input 
              id="title" 
              value={title} 
              onChange={(e) => setTitle(e.target.value)} 
              placeholder="Note title"
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="metadata">Metadata (JSON)</Label>
            <Textarea 
              id="metadata" 
              value={metadataText} 
              onChange={(e) => setMetadataText(e.target.value)} 
              placeholder='{"field-1":"value","field-2":["value1","value2"]}'
              className="min-h-[120px] font-mono resize-y"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="content">Content (Markdown)</Label>
            <Textarea 
              id="content" 
              value={content} 
              onChange={(e) => setContent(e.target.value)} 
              placeholder="# Markdown content" 
              className="min-h-[200px] font-mono resize-y"
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isSubmitting ? "Saving..." : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
