import { KnowledgeItem } from "../types"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ScrollArea } from "@/components/ui/scroll-area"
import { FileText, File, Pencil, Eye, ArrowLeft } from "lucide-react"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import remarkBreaks from "remark-breaks"
import type { Components } from "react-markdown"

interface ItemDetailViewProps {
  item: KnowledgeItem
  onBack: () => void
  onEdit: (item: KnowledgeItem) => void
  onViewSource: (item: KnowledgeItem) => void
}

const markdownComponents: Components = {
  h1: ({ children }) => <h1 className="text-2xl font-semibold tracking-tight mt-2 mb-3">{children}</h1>,
  h2: ({ children }) => <h2 className="text-xl font-semibold tracking-tight mt-6 mb-3">{children}</h2>,
  h3: ({ children }) => <h3 className="text-lg font-semibold mt-5 mb-2">{children}</h3>,
  p: ({ children }) => <p className="leading-7 mb-4">{children}</p>,
  ul: ({ children }) => <ul className="list-disc pl-6 mb-4 space-y-1">{children}</ul>,
  ol: ({ children }) => <ol className="list-decimal pl-6 mb-4 space-y-1">{children}</ol>,
  li: ({ children }) => <li className="leading-7">{children}</li>,
  blockquote: ({ children }) => (
    <blockquote className="border-l-4 border-border pl-4 py-1 my-4 text-muted-foreground italic">
      {children}
    </blockquote>
  ),
  code: ({ children, className }) => (
    <code className={`rounded bg-muted px-1.5 py-0.5 text-[0.9em] ${className ?? ""}`}>{children}</code>
  ),
  pre: ({ children }) => (
    <pre className="mb-4 overflow-x-auto rounded-lg border bg-muted/40 p-4 text-sm">{children}</pre>
  ),
  a: ({ children, href }) => {
    const isExternal = !!href && /^(https?:)?\/\//.test(href)

    return (
      <a
        href={href}
        className="font-medium underline underline-offset-4 text-primary hover:text-primary/80"
        target={isExternal ? "_blank" : undefined}
        rel={isExternal ? "noreferrer noopener" : undefined}
      >
        {children}
      </a>
    )
  },
  table: ({ children }) => <table className="mb-4 w-full border-collapse text-sm">{children}</table>,
  thead: ({ children }) => <thead className="bg-muted/40">{children}</thead>,
  th: ({ children }) => <th className="border px-3 py-2 text-left font-medium">{children}</th>,
  td: ({ children }) => <td className="border px-3 py-2 align-top">{children}</td>
}

export function ItemDetailView({
  item,
  onBack,
  onEdit,
  onViewSource
}: ItemDetailViewProps) {
  const Icon = item.itemType === 'Note' ? FileText : File
  const hasSourceFile = !!item.sourceUploadId

  const metadataEntries = (() => {
    if (!item.metadata) return [] as [string, unknown][]
    try {
      const parsed = JSON.parse(item.metadata)
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        return Object.entries(parsed)
      }
      return [['Raw', parsed]] as [string, unknown][]
    } catch {
      return [['Raw', item.metadata]] as [string, unknown][]
    }
  })()

  const uploadMetadataEntries = (() => {
    if (!item.sourceUploadMetadata) return [] as [string, unknown][]
    try {
      const parsed = JSON.parse(item.sourceUploadMetadata)
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        return Object.entries(parsed)
      }
      return [['Raw', parsed]] as [string, unknown][]
    } catch {
      return [['Raw', item.sourceUploadMetadata]] as [string, unknown][]
    }
  })()

  return (
    <div className="flex flex-col h-full bg-background">
      {/* Header */}
      <div className="shrink-0 border-b px-6 py-4">
        <div className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-3 min-w-0">
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={onBack}
              className="shrink-0"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to list
            </Button>
            <div className="p-2 rounded-md bg-muted shrink-0">
              <Icon className="h-5 w-5 text-foreground" />
            </div>
            <div className="min-w-0">
              <h1 className="truncate text-xl font-semibold" title={item.title}>
                {item.title}
              </h1>
              <div className="flex items-center gap-2 mt-1">
                <span className="bg-secondary text-secondary-foreground text-[10px] px-2 py-0.5 rounded-full font-medium">
                  {item.itemType}
                </span>
                {item.folderName && (
                  <span className="text-muted-foreground text-xs truncate">
                    in {item.folderName}
                  </span>
                )}
                <span className="text-muted-foreground text-xs">
                  • Updated {new Date(item.updatedAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          </div>
          {item.itemType === 'Note' && (
            <Button onClick={() => onEdit(item)} size="sm" variant="outline" className="shrink-0">
              <Pencil className="h-4 w-4 mr-2" />
              Edit Note
            </Button>
          )}
        </div>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="markdown" className="flex-1 flex flex-col min-h-0">
        <div className="px-6 border-b shrink-0 pt-2">
          <TabsList className="w-full justify-start rounded-none border-b bg-transparent p-0">
            <TabsTrigger 
              value="markdown" 
              className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:shadow-none pb-3"
            >
              Markdown
            </TabsTrigger>
            {hasSourceFile && (
              <TabsTrigger 
                value="source"
                className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:shadow-none pb-3"
              >
                Source
              </TabsTrigger>
            )}
            {item.chunks && item.chunks.length > 0 && (
              <TabsTrigger 
                value="chunks"
                className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:shadow-none pb-3"
              >
                Chunks ({item.chunkCount || item.chunks.length})
              </TabsTrigger>
            )}
            <TabsTrigger 
              value="metadata"
              className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:shadow-none pb-3"
            >
              Metadata
            </TabsTrigger>
          </TabsList>
        </div>

        <div className="flex-1 min-h-0 relative">
          <ScrollArea className="h-full">
            <div className="p-6">
              <TabsContent value="markdown" className="mt-0 outline-none">
                {item.summary && (
                  <div className="mb-6 p-4 rounded-lg bg-muted/50 border border-border">
                    <h4 className="text-sm font-semibold mb-2">Summary</h4>
                    <p className="text-sm text-muted-foreground">{item.summary}</p>
                  </div>
                )}
                <div className="max-w-none text-sm text-foreground">
                  <ReactMarkdown
                    remarkPlugins={[remarkGfm, remarkBreaks]}
                    components={markdownComponents}
                  >
                    {item.sourceMarkdown || item.content || "No content available."}
                  </ReactMarkdown>
                </div>
              </TabsContent>

              {hasSourceFile && (
                <TabsContent value="source" className="mt-0 outline-none">
                  <div className="flex flex-col items-center justify-center py-12 text-center border-2 border-dashed rounded-xl border-border bg-muted/30">
                    <File className="h-12 w-12 text-muted-foreground mb-4" />
                    <h3 className="text-lg font-medium mb-1">Source File</h3>
                    <p className="text-sm text-muted-foreground mb-6">
                      {item.sourceUploadFileName || 'Unknown file'}
                    </p>
                    <Button onClick={() => onViewSource(item)}>
                      <Eye className="h-4 w-4 mr-2" />
                      View Source File
                    </Button>
                  </div>
                </TabsContent>
              )}

              {item.chunks && item.chunks.length > 0 && (
                <TabsContent value="chunks" className="mt-0 outline-none">
                  <div className="rounded-md border">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead className="w-[50px]">#</TableHead>
                          <TableHead>Content / Summary</TableHead>
                          <TableHead className="w-[100px] text-right">Tokens</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {item.chunks.map((chunk) => (
                          <TableRow key={chunk.id}>
                            <TableCell className="font-medium">{chunk.chunkIndex}</TableCell>
                            <TableCell>
                              <div className="space-y-2 py-2">
                                {chunk.summary && (
                                  <div className="text-xs font-semibold text-primary/80">
                                    {chunk.summary}
                                  </div>
                                )}
                                <div className="text-sm line-clamp-4 hover:line-clamp-none transition-all duration-200">
                                  {chunk.content}
                                </div>
                                {chunk.context && (
                                  <div className="text-xs text-muted-foreground border-l-2 pl-2 italic mt-2">
                                    {chunk.context}
                                  </div>
                                )}
                              </div>
                            </TableCell>
                            <TableCell className="text-right text-xs text-muted-foreground">
                              {chunk.tokenCount ?? '-'}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                </TabsContent>
              )}

              <TabsContent value="metadata" className="mt-0 outline-none space-y-8">
                <div>
                  <h3 className="text-sm font-medium mb-3">Item Metadata</h3>
                  {metadataEntries.length > 0 ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
                      {metadataEntries.map(([key, value]) => (
                        <div key={key} className="flex flex-col p-3 rounded-lg border bg-card">
                          <span className="text-xs text-muted-foreground mb-1">{key}</span>
                          <span className="text-sm font-medium truncate" title={typeof value === 'object' ? JSON.stringify(value) : String(value)}>
                            {typeof value === 'object' ? JSON.stringify(value) : String(value)}
                          </span>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground italic">No metadata available</p>
                  )}
                </div>

                {hasSourceFile && (
                  <div>
                    <h3 className="text-sm font-medium mb-3">Upload Metadata</h3>
                    {uploadMetadataEntries.length > 0 ? (
                      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
                        {uploadMetadataEntries.map(([key, value]) => (
                          <div key={key} className="flex flex-col p-3 rounded-lg border bg-card">
                            <span className="text-xs text-muted-foreground mb-1">{key}</span>
                            <span className="text-sm font-medium truncate" title={typeof value === 'object' ? JSON.stringify(value) : String(value)}>
                              {typeof value === 'object' ? JSON.stringify(value) : String(value)}
                            </span>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p className="text-sm text-muted-foreground italic">No upload metadata available</p>
                    )}
                  </div>
                )}
              </TabsContent>
            </div>
          </ScrollArea>
        </div>
      </Tabs>
    </div>
  )
}
