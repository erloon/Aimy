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
  const [tags, setTags] = useState("")

  useEffect(() => {
    if (open) {
      setTitle(item?.title || "")
      setContent(item?.content || "")
      // Parse JSON tags to comma-separated string for display
      let initialTags = ""
      if (item?.tags) {
        try {
          const parsed = JSON.parse(item.tags)
          if (Array.isArray(parsed)) {
            initialTags = parsed.join(", ")
          } else {
            initialTags = item.tags
          }
        } catch {
          initialTags = item.tags
        }
      }
      setTags(initialTags)
    } else {
      // Reset form when closed if needed, but useEffect above handles it on open
    }
  }, [open, item])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Convert comma-separated string to JSON array string
    const tagsArray = tags.split(',').map(t => t.trim()).filter(Boolean)
    const jsonTags = JSON.stringify(tagsArray)

    try {
      if (item) {
        await updateItem.mutateAsync({
          id: item.id,
          request: {
            title,
            content,
            tags: jsonTags,
            folderId: item.folderId 
          }
        })
      } else {
        await createNote.mutateAsync({
          folderId,
          title,
          content,
          tags: jsonTags,
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
            <Label htmlFor="tags">Tags</Label>
            <Input 
              id="tags" 
              value={tags} 
              onChange={(e) => setTags(e.target.value)} 
              placeholder="comma, separated, tags"
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
