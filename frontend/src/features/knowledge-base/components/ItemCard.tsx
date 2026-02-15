import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { KnowledgeItem } from "../types"
import { FileText, File, Pencil, Trash2 } from "lucide-react"
import { cn } from "@/lib/utils"

interface ItemCardProps {
  item: KnowledgeItem
  onEdit?: () => void
  onDelete?: () => void
  className?: string
}

export function ItemCard({ item, onEdit, onDelete, className }: ItemCardProps) {
  const Icon = item.itemType === 'Note' ? FileText : File

  const tagsList = (() => {
    if (!item.tags) return []
    try {
      const parsed = JSON.parse(item.tags)
      return Array.isArray(parsed) ? parsed : [String(parsed)]
    } catch {
      return item.tags.split(',').map(t => t.trim()).filter(Boolean)
    }
  })()

  return (
    <Card className={cn("hover:bg-accent/50 transition-colors flex flex-col justify-between h-full group relative overflow-hidden", className)}>
      <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2 min-w-0">
        <div className="flex items-center gap-2 min-w-0 flex-1 pr-2">
          <div className="p-1.5 rounded-md bg-muted shrink-0">
            <Icon className="h-4 w-4 text-foreground" />
          </div>
          <CardTitle className="text-sm font-medium truncate" title={item.title}>
            {item.title}
          </CardTitle>
        </div>

        <span className={cn(
          "text-[10px] px-1.5 py-0.5 rounded-full border shrink-0",
          item.itemType === 'Note'
            ? "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-800"
            : "bg-orange-50 text-orange-700 border-orange-200 dark:bg-orange-900/30 dark:text-orange-300 dark:border-orange-800"
        )}>
          {item.itemType}
        </span>
      </CardHeader>
      <CardContent className="py-2 flex-grow min-w-0">
        <p className="text-xs text-muted-foreground line-clamp-3 break-words">
          {item.content || (item.itemType === 'File' ? "Binary file content" : "No content")}
        </p>
        {item.folderName && (
          <div className="mt-2">
            <span className="text-[10px] bg-muted text-muted-foreground px-1.5 py-0.5 rounded-full border">
              {item.folderName}
            </span>
          </div>
        )}
        {tagsList.length > 0 && (
          <div className="flex flex-wrap gap-1 mt-2">
            {tagsList.map((tag: string) => (
              <span key={tag} className="text-[10px] bg-secondary text-secondary-foreground px-1 rounded">
                {tag}
              </span>
            ))}
          </div>
        )}
      </CardContent>
      <CardFooter className="flex justify-between py-2 border-t bg-muted/20">
        <div className="text-[10px] text-muted-foreground">
          {new Date(item.updatedAt).toLocaleDateString()}
        </div>
        <div className="flex gap-1">
          <Button variant="ghost" size="icon" className="h-6 w-6" onClick={onEdit}>
            <Pencil className="h-3 w-3" />
            <span className="sr-only">Edit</span>
          </Button>
          <Button variant="ghost" size="icon" className="h-6 w-6 text-destructive hover:text-destructive hover:bg-destructive/10" onClick={onDelete}>
            <Trash2 className="h-3 w-3" />
            <span className="sr-only">Delete</span>
          </Button>
        </div>
      </CardFooter>
    </Card>
  )
}
