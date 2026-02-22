import { useState, useEffect, useCallback } from 'react'
import { Download, FileWarning, Loader2 } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { ScrollArea } from '@/components/ui/scroll-area'
import { downloadFile } from '@/features/storage/api/storage-api'

interface FilePreviewSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  fileId: string | null
  fileName: string | null
  contentType?: string | null
}

type PreviewType = 'pdf' | 'image' | 'text' | 'unsupported'

const IMAGE_EXTENSIONS = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg', '.bmp', '.ico']
const TEXT_EXTENSIONS = ['.txt', '.md', '.csv', '.json', '.xml', '.html', '.css', '.js', '.ts', '.tsx', '.jsx', '.log', '.yml', '.yaml']
const PDF_EXTENSIONS = ['.pdf']

function getExtension(fileName: string): string {
  const dotIndex = fileName.lastIndexOf('.')
  if (dotIndex === -1 || dotIndex === 0) return ''
  return fileName.slice(dotIndex).toLowerCase()
}

function resolvePreviewType(fileName: string, contentType?: string | null): PreviewType {
  // Try content type first
  if (contentType) {
    if (contentType === 'application/pdf') return 'pdf'
    if (contentType.startsWith('image/')) return 'image'
    if (contentType.startsWith('text/')) return 'text'
    if (contentType === 'application/json') return 'text'
  }

  // Fall back to extension
  const ext = getExtension(fileName)
  if (PDF_EXTENSIONS.includes(ext)) return 'pdf'
  if (IMAGE_EXTENSIONS.includes(ext)) return 'image'
  if (TEXT_EXTENSIONS.includes(ext)) return 'text'

  return 'unsupported'
}

export function FilePreviewSheet({
  open,
  onOpenChange,
  fileId,
  fileName,
  contentType,
}: FilePreviewSheetProps) {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [blobUrl, setBlobUrl] = useState<string | null>(null)
  const [textContent, setTextContent] = useState<string | null>(null)

  const previewType = fileName ? resolvePreviewType(fileName, contentType) : 'unsupported'

  const handleDownload = useCallback(() => {
    if (!blobUrl || !fileName) return
    const a = document.createElement('a')
    a.href = blobUrl
    a.download = fileName
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
  }, [blobUrl, fileName])

  // Fetch blob when sheet opens with a new file
  useEffect(() => {
    if (!open || !fileId || !fileName) {
      return
    }

    let revoked = false
    let objectUrl: string | null = null
    const controller = new AbortController()

    const fetchFile = async () => {
      setIsLoading(true)
      setError(null)
      setBlobUrl(null)
      setTextContent(null)

      try {
        const blob = await downloadFile(fileId)

        if (controller.signal.aborted) return

        const type = resolvePreviewType(fileName, contentType)

        if (type === 'text') {
          const text = await blob.text()
          if (!controller.signal.aborted) {
            setTextContent(text)
          }
        } else {
          objectUrl = URL.createObjectURL(blob)
          if (!controller.signal.aborted) {
            setBlobUrl(objectUrl)
          } else {
            URL.revokeObjectURL(objectUrl)
          }
        }
      } catch (err) {
        if (!controller.signal.aborted) {
          setError(err instanceof Error ? err.message : 'Failed to load file')
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    fetchFile()

    return () => {
      controller.abort()
      if (objectUrl && !revoked) {
        URL.revokeObjectURL(objectUrl)
        revoked = true
      }
    }
  }, [open, fileId, fileName, contentType])

  // Cleanup blob URL when sheet closes
  useEffect(() => {
    if (!open && blobUrl) {
      URL.revokeObjectURL(blobUrl)
      setBlobUrl(null)
      setTextContent(null)
      setError(null)
    }
  }, [open, blobUrl])

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-2xl flex flex-col p-0">
        <SheetHeader className="px-6 pt-6 pb-4 shrink-0">
          <SheetTitle className="truncate pr-8" title={fileName ?? undefined}>
            {fileName ?? 'File Preview'}
          </SheetTitle>
          <SheetDescription>
            {previewType === 'unsupported'
              ? 'This file format cannot be previewed'
              : 'File preview'}
          </SheetDescription>
        </SheetHeader>

        <div className="flex-1 min-h-0 px-6 pb-6">
          {isLoading && (
            <div className="flex items-center justify-center h-full">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          )}

          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {!isLoading && !error && previewType === 'pdf' && blobUrl && (
            <embed
              src={`${blobUrl}#toolbar=1`}
              type="application/pdf"
              className="w-full h-full rounded-md border"
            />
          )}

          {!isLoading && !error && previewType === 'image' && blobUrl && (
            <div className="flex items-center justify-center h-full">
              <img
                src={blobUrl}
                alt={fileName ?? 'Preview'}
                className="max-w-full max-h-full object-contain rounded-md"
              />
            </div>
          )}

          {!isLoading && !error && previewType === 'text' && textContent !== null && (
            <ScrollArea className="h-full rounded-md border">
              <pre className="p-4 text-sm whitespace-pre-wrap break-words font-mono">
                {textContent}
              </pre>
            </ScrollArea>
          )}

          {!isLoading && !error && previewType === 'unsupported' && (
            <div className="flex flex-col items-center justify-center h-full gap-4 text-muted-foreground">
              <FileWarning className="h-16 w-16" />
              <div className="text-center">
                <p className="font-medium text-foreground">Cannot preview this file</p>
                <p className="text-sm mt-1">
                  {fileName ? `"${getExtension(fileName)}" files are not supported for preview` : 'Unsupported format'}
                </p>
              </div>
              {fileId && (
                <Button onClick={handleDownload} disabled={!blobUrl}>
                  <Download className="h-4 w-4 mr-2" />
                  Download File
                </Button>
              )}
            </div>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}