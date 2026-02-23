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
import { Alert, AlertDescription } from "@/components/ui/alert"
import { useCreateNote, useUpdateItem } from "../hooks/useItems"
import { KnowledgeItem } from "../types"
import { Loader2, Plus, Trash2 } from "lucide-react"

interface MetadataField {
  id: string
  key: string
  value: string
}

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
  const [metadataFields, setMetadataFields] = useState<MetadataField[]>([{ id: createFieldId(), key: "", value: "" }])
  const [metadataError, setMetadataError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    if (open) {
      setTitle(item?.title || "")
      setContent(item?.content || "")
      setMetadataFields(parseMetadataFields(item?.metadata))
      setMetadataError(null)
      setSubmitError(null)
    } else {
      // Reset form when closed if needed, but useEffect above handles it on open
    }
  }, [open, item])

  const validateMetadata = (fields: MetadataField[]): { ok: true; normalized: string } | { ok: false; error: string } => {
    const normalizedEntries = fields
      .map((field) => ({
        key: field.key.trim(),
        value: field.value.trim(),
      }))
      .filter((field) => field.key.length > 0 || field.value.length > 0)

    const metadataObject: Record<string, string> = {}

    for (const entry of normalizedEntries) {
      if (!entry.key) {
        return { ok: false, error: "Each metadata row needs a key." }
      }

      if (metadataObject[entry.key] !== undefined) {
        return { ok: false, error: `Duplicate metadata key: ${entry.key}` }
      }

      metadataObject[entry.key] = entry.value
    }

    return { ok: true, normalized: JSON.stringify(metadataObject) }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    const metadataValidation = validateMetadata(metadataFields)
    if (!metadataValidation.ok) {
      setMetadataError(metadataValidation.error)
      return
    }

    setMetadataError(null)
    setSubmitError(null)

    try {
      if (item) {
        await updateItem.mutateAsync({
          id: item.id,
          request: {
            title,
            content,
            metadata: metadataValidation.normalized,
            folderId: item.folderId 
          }
        })
      } else {
        await createNote.mutateAsync({
          folderId,
          title,
          content,
          metadata: metadataValidation.normalized,
        })
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (error) {
      const message = error instanceof Error ? error.message : "Failed to save note."
      setSubmitError(message)
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
            <div className="flex items-center justify-between">
              <Label>Metadata (optional)</Label>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  setMetadataFields((prev) => [...prev, { id: createFieldId(), key: "", value: "" }])
                  setMetadataError(null)
                }}
              >
                <Plus className="h-4 w-4 mr-2" />
                Add field
              </Button>
            </div>
            <div className="space-y-2">
              {metadataFields.map((field) => (
                <div key={field.id} className="grid grid-cols-[1fr_1fr_auto] gap-2">
                  <Input
                    value={field.key}
                    onChange={(e) => {
                      const nextValue = e.target.value
                      setMetadataFields((prev) => prev.map((entry) => entry.id === field.id ? { ...entry, key: nextValue } : entry))
                      if (metadataError) {
                        setMetadataError(null)
                      }
                    }}
                    placeholder="key"
                  />
                  <Input
                    value={field.value}
                    onChange={(e) => {
                      const nextValue = e.target.value
                      setMetadataFields((prev) => prev.map((entry) => entry.id === field.id ? { ...entry, value: nextValue } : entry))
                      if (metadataError) {
                        setMetadataError(null)
                      }
                    }}
                    placeholder="value"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => {
                      setMetadataFields((prev) => {
                        if (prev.length <= 1) {
                          return [{ id: createFieldId(), key: "", value: "" }]
                        }

                        return prev.filter((entry) => entry.id !== field.id)
                      })
                      if (metadataError) {
                        setMetadataError(null)
                      }
                    }}
                    aria-label="Remove metadata field"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>
            <p className="text-xs text-muted-foreground">
              Add simple key-value metadata, for example: category = notes, source = meeting.
            </p>
            {metadataError && (
              <Alert variant="destructive">
                <AlertDescription>{metadataError}</AlertDescription>
              </Alert>
            )}
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
          {submitError && (
            <Alert variant="destructive">
              <AlertDescription>{submitError}</AlertDescription>
            </Alert>
          )}
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

function createFieldId(): string {
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`
}

function parseMetadataFields(rawMetadata?: string | null): MetadataField[] {
  if (!rawMetadata) {
    return [{ id: createFieldId(), key: "", value: "" }]
  }

  try {
    const parsed = JSON.parse(rawMetadata)
    if (parsed && typeof parsed === "object" && !Array.isArray(parsed)) {
      const entries = Object.entries(parsed)
      if (entries.length === 0) {
        return [{ id: createFieldId(), key: "", value: "" }]
      }

      return entries.map(([key, value]) => ({
        id: createFieldId(),
        key,
        value: value === null ? "" : typeof value === "string" ? value : JSON.stringify(value),
      }))
    }
  } catch {
    // Fallback below for legacy/non-json metadata values.
  }

  return [{ id: createFieldId(), key: "notes", value: rawMetadata }]
}
