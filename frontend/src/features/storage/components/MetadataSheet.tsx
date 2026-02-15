import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import { filesize } from 'filesize'
import { Plus, X } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { NormalizedFileItem } from '../api/storage-api'

interface MetadataSheetProps {
  file: NormalizedFileItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (fileId: string, metadata: Record<string, string>) => void
  isSaving?: boolean
}

interface KeyValue {
  key: string
  value: string
}

function parseMetadata(metadata: string | null): KeyValue[] {
  if (!metadata) return []
  try {
    const parsed = JSON.parse(metadata)
    return Object.entries(parsed).map(([key, value]) => ({
      key,
      value: String(value)
    }))
  } catch {
    return []
  }
}

function serializeMetadata(entries: KeyValue[]): Record<string, string> {
  const result: Record<string, string> = {}
  entries.forEach(entry => {
    if (entry.key.trim()) {
      result[entry.key.trim()] = entry.value
    }
  })
  return result
}

export function MetadataSheet({
  file,
  open,
  onOpenChange,
  onSave,
  isSaving
}: MetadataSheetProps) {
  const [entries, setEntries] = useState<KeyValue[]>([])

  // Reset entries when file changes
  useEffect(() => {
    if (file) {
      setEntries(parseMetadata(file.metadata))
    }
  }, [file])

  const handleAddEntry = () => {
    setEntries(prev => [...prev, { key: '', value: '' }])
  }

  const handleRemoveEntry = (index: number) => {
    setEntries(prev => prev.filter((_, i) => i !== index))
  }

  const handleUpdateEntry = (index: number, field: 'key' | 'value', value: string) => {
    setEntries(prev => prev.map((entry, i) => 
      i === index ? { ...entry, [field]: value } : entry
    ))
  }

  const handleSave = () => {
    if (!file) return
    const metadata = serializeMetadata(entries)
    onSave(file.id, metadata)
  }

  if (!file) return null

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg overflow-y-auto">
        <SheetHeader>
          <SheetTitle>File Details</SheetTitle>
          <SheetDescription>
            View and edit file metadata
          </SheetDescription>
        </SheetHeader>

        <div className="space-y-6 py-6">
          {/* Read-only file info */}
          <div className="space-y-4">
            <div>
              <Label className="text-muted-foreground">Filename</Label>
              <p className="font-medium mt-1">{file.filename}</p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label className="text-muted-foreground">Size</Label>
                <p className="font-medium mt-1">{String(filesize(file.size))}</p>
              </div>
              <div>
                <Label className="text-muted-foreground">Type</Label>
                <p className="font-medium mt-1 truncate">{file.contentType}</p>
              </div>
            </div>
            <div>
              <Label className="text-muted-foreground">Uploaded</Label>
              <p className="font-medium mt-1">
                {format(new Date(file.uploadedAt), 'PPP')}
              </p>
            </div>
          </div>

          {/* Editable metadata */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <Label>Metadata</Label>
              <Button variant="outline" size="sm" onClick={handleAddEntry}>
                <Plus className="h-4 w-4 mr-1" />
                Add
              </Button>
            </div>

            {entries.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                No metadata. Click "Add" to create key-value pairs.
              </p>
            ) : (
              <div className="space-y-3">
                {entries.map((entry, index) => (
                  <div key={index} className="flex items-start gap-2">
                    <Input
                      placeholder="Key"
                      value={entry.key}
                      onChange={(e) => handleUpdateEntry(index, 'key', e.target.value)}
                      className="flex-1"
                    />
                    <Input
                      placeholder="Value"
                      value={entry.value}
                      onChange={(e) => handleUpdateEntry(index, 'value', e.target.value)}
                      className="flex-1"
                    />
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleRemoveEntry(index)}
                      className="shrink-0"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Save button */}
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSaving}>
              {isSaving ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
