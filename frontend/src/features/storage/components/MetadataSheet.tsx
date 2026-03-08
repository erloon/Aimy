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
import {
  getMetadataKeys,
  getMetadataValues,
  normalizeMetadata,
  type MetadataKeyDefinition,
  type MetadataValueSuggestionItem,
  type MetadataNormalizeWarning,
  type NormalizedFileItem
} from '../api/storage-api'

interface MetadataSheetProps {
  file: NormalizedFileItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (fileId: string, metadata: Record<string, unknown>) => void
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
      value: typeof value === 'string' ? value : JSON.stringify(value)
    }))
  } catch {
    return []
  }
}

function serializeMetadata(entries: KeyValue[]): Record<string, unknown> {
  const result: Record<string, unknown> = {}
  entries.forEach(entry => {
    if (entry.key.trim()) {
      const trimmedValue = entry.value.trim()
      if (!trimmedValue) {
        result[entry.key.trim()] = ''
        return
      }

      try {
        result[entry.key.trim()] = JSON.parse(trimmedValue)
      } catch {
        result[entry.key.trim()] = entry.value
      }
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
  const [keyDefinitions, setKeyDefinitions] = useState<MetadataKeyDefinition[]>([])
  const [valueSuggestions, setValueSuggestions] = useState<Record<number, MetadataValueSuggestionItem[]>>({})
  const [normalizationWarnings, setNormalizationWarnings] = useState<MetadataNormalizeWarning[]>([])
  const [validationError, setValidationError] = useState<string | null>(null)

  // Reset entries when file changes
  useEffect(() => {
    if (file) {
      setEntries(parseMetadata(file.metadata))
      setNormalizationWarnings([])
      setValidationError(null)
    }
  }, [file])

  useEffect(() => {
    if (!open) return

    void (async () => {
      try {
        const keys = await getMetadataKeys()
        setKeyDefinitions(keys)
      } catch {
        setKeyDefinitions([])
      }
    })()
  }, [open])

  const keyDefinitionMap = keyDefinitions.reduce<Record<string, MetadataKeyDefinition>>((acc, definition) => {
    acc[definition.key.toLowerCase()] = definition
    return acc
  }, {})

  const handleAddEntry = () => {
    setEntries(prev => [...prev, { key: '', value: '' }])
  }

  const handleRemoveEntry = (index: number) => {
    setEntries(prev => prev.filter((_, i) => i !== index))
    setValueSuggestions(prev => {
      const next: Record<number, MetadataValueSuggestionItem[]> = {}
      for (const [key, suggestions] of Object.entries(prev)) {
        const numeric = Number(key)
        if (numeric < index) {
          next[numeric] = suggestions
        } else if (numeric > index) {
          next[numeric - 1] = suggestions
        }
      }
      return next
    })
  }

  const handleUpdateEntry = (index: number, field: 'key' | 'value', value: string) => {
    setEntries(prev => prev.map((entry, i) => 
      i === index ? { ...entry, [field]: value } : entry
    ))

    if (field === 'value') {
      const key = entries[index]?.key?.trim()
      if (key) {
        void loadValueSuggestions(index, key, value)
      }
    }

    if (field === 'key') {
      void loadValueSuggestions(index, value, entries[index]?.value ?? '')
    }
  }

  const loadValueSuggestions = async (index: number, key: string, valuePrefix: string) => {
    const normalizedKey = key.trim()
    if (!normalizedKey) {
      setValueSuggestions(prev => ({ ...prev, [index]: [] }))
      return
    }

    try {
      const suggestions = await getMetadataValues(normalizedKey, valuePrefix || undefined)
      setValueSuggestions(prev => ({ ...prev, [index]: suggestions.items }))
    } catch {
      setValueSuggestions(prev => ({ ...prev, [index]: [] }))
    }
  }

  const validateStrictDefinitions = (): string | null => {
    for (let i = 0; i < entries.length; i += 1) {
      const entry = entries[i]
      const key = entry.key.trim()
      const value = entry.value.trim()
      if (!key || !value) {
        continue
      }

      const definition = keyDefinitionMap[key.toLowerCase()]
      if (!definition || definition.allowFreeText) {
        continue
      }

      const knownOptions = valueSuggestions[i] ?? []
      if (knownOptions.length === 0) {
        continue
      }

      const isKnown = knownOptions.some(option =>
        option.value.toLowerCase() === value.toLowerCase()
        || option.aliases.some(alias => alias.toLowerCase() === value.toLowerCase())
        || option.label.toLowerCase() === value.toLowerCase())

      if (!isKnown) {
        return `Value '${value}' is not allowed for key '${key}'. Choose one of the suggested values.`
      }
    }

    return null
  }

  const handleSave = async () => {
    if (!file) return

    setValidationError(null)
    const strictValidationError = validateStrictDefinitions()
    if (strictValidationError) {
      setValidationError(strictValidationError)
      return
    }

    const metadata = serializeMetadata(entries)

    try {
      const normalized = await normalizeMetadata(metadata)
      setNormalizationWarnings(normalized.warnings)

      const normalizedPayload = normalized.metadata
        ? (JSON.parse(normalized.metadata) as Record<string, unknown>)
        : {}

      onSave(file.id, normalizedPayload)
    } catch {
      onSave(file.id, metadata)
    }
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
                      list={`metadata-key-options-${index}`}
                    />
                    <datalist id={`metadata-key-options-${index}`}>
                      {keyDefinitions.map(definition => (
                        <option key={definition.key} value={definition.key}>
                          {definition.label}
                        </option>
                      ))}
                    </datalist>
                    <Input
                      placeholder="Value"
                      value={entry.value}
                      onChange={(e) => handleUpdateEntry(index, 'value', e.target.value)}
                      className="flex-1"
                      list={`metadata-value-options-${index}`}
                    />
                    <datalist id={`metadata-value-options-${index}`}>
                      {(valueSuggestions[index] ?? []).map(option => (
                        <option key={`${option.value}-${option.matchType}`} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </datalist>
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

            {validationError && (
              <p className="text-sm text-destructive">{validationError}</p>
            )}

            {normalizationWarnings.length > 0 && (
              <div className="rounded-md border bg-muted/40 p-3 space-y-1">
                <p className="text-xs font-medium text-muted-foreground">Canonicalization feedback</p>
                {normalizationWarnings.map((warning, index) => (
                  <p key={`${warning.key}-${index}`} className="text-xs text-muted-foreground">
                    {warning.key}: {warning.message}
                    {warning.resolvedValue ? ` -> ${warning.resolvedValue}` : ''}
                  </p>
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
