import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { KnowledgeItem } from "../types"
import { FileText, File, Pencil, Trash2, Eye } from "lucide-react"


interface ItemTableProps {
  items: KnowledgeItem[]
  onEdit?: (item: KnowledgeItem) => void
  onDelete?: (item: KnowledgeItem) => void
  onViewSource?: (item: KnowledgeItem) => void
  onViewItem?: (item: KnowledgeItem) => void
}

export function ItemTable({ items, onEdit, onDelete, onViewSource, onViewItem }: ItemTableProps) {
  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-[50px]">Type</TableHead>
            <TableHead className="w-[200px]">Name</TableHead>
            <TableHead className="hidden md:table-cell">Content Preview</TableHead>
            <TableHead className="hidden sm:table-cell w-[220px]">Metadata</TableHead>
            <TableHead className="w-[120px]">Updated</TableHead>
            <TableHead className="w-[100px] text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => {
            const Icon = item.itemType === 'Note' ? FileText : File

            const metadataEntries = (() => {
              if (!item.metadata) return [] as string[]
              try {
                const parsed = JSON.parse(item.metadata) as unknown
                if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
                  return Object.entries(parsed as Record<string, unknown>).map(
                    ([key, value]) => `${key}: ${typeof value === 'string' ? value : JSON.stringify(value)}`
                  )
                }
                return [JSON.stringify(parsed)]
              } catch {
                return [item.metadata]
              }
            })()

            return (
              <TableRow key={item.id} className="group cursor-pointer hover:bg-muted/50 transition-colors" onClick={() => onViewItem?.(item)}>
                <TableCell>
                  <Icon className="h-4 w-4 text-muted-foreground" />
                </TableCell>
                <TableCell className="font-medium">
                  <div className="flex flex-col">
                    <span className="truncate max-w-[180px]" title={item.title}>{item.title}</span>
                    <span className="text-[10px] text-muted-foreground inline-block truncate max-w-[180px]">
                      {item.folderName}
                    </span>
                  </div>
                </TableCell>
                <TableCell className="hidden md:table-cell">
                  <p className="text-sm text-muted-foreground truncate max-w-[300px]">
                    {item.content || (item.itemType === 'File' ? "Binary file content" : "No content")}
                  </p>
                </TableCell>
                <TableCell className="hidden sm:table-cell">
                  <div className="flex flex-wrap gap-1">
                    {metadataEntries.slice(0, 2).map((entry: string) => (
                      <span key={entry} className="text-[10px] bg-secondary text-secondary-foreground px-1 rounded truncate max-w-[160px]" title={entry}>
                        {entry}
                      </span>
                    ))}
                    {metadataEntries.length > 2 && (
                      <span className="text-[10px] text-muted-foreground">+{metadataEntries.length - 2}</span>
                    )}
                  </div>
                </TableCell>
                <TableCell className="text-xs text-muted-foreground">
                  {new Date(item.updatedAt).toLocaleDateString()}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1 opacity-100 sm:opacity-0 group-hover:opacity-100 transition-opacity">
                    {item.sourceUploadId && onViewSource && (
                      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={(e) => { e.stopPropagation(); onViewSource(item); }}>
                        <Eye className="h-4 w-4" />
                        <span className="sr-only">Source View</span>
                      </Button>
                    )}
                    <Button variant="ghost" size="icon" className="h-8 w-8" onClick={(e) => { e.stopPropagation(); onEdit?.(item); }}>
                      <Pencil className="h-4 w-4" />
                      <span className="sr-only">Edit</span>
                    </Button>
                    <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10" onClick={(e) => { e.stopPropagation(); onDelete?.(item); }}>
                      <Trash2 className="h-4 w-4" />
                      <span className="sr-only">Delete</span>
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}
