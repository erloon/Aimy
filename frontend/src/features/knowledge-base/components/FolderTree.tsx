import * as React from "react"
import { ChevronRight, Folder } from "lucide-react"
import { cn } from "@/lib/utils"
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible"
import { Button } from "@/components/ui/button"
import { FolderTreeNode } from "../types"
import { useFolderTree } from "../hooks/useFolders"
import { Skeleton } from "@/components/ui/skeleton"

interface FolderTreeProps {
  onSelectFolder?: (folderId: string | null) => void
  selectedFolderId?: string | null
  className?: string
}

export function FolderTree({ onSelectFolder, selectedFolderId, className }: FolderTreeProps) {
  const { data, isLoading } = useFolderTree()

  if (isLoading) {
    return (
      <div className="p-2 space-y-2">
        <Skeleton className="h-8 w-full" />
        <Skeleton className="h-8 w-full" />
        <Skeleton className="h-8 w-full" />
      </div>
    )
  }

  return (
    <div className={cn("w-full space-y-1", className)}>
      <Button
        variant="ghost"
        className={cn(
          "w-full justify-start gap-2 px-2 h-9",
          selectedFolderId === null && "bg-accent text-accent-foreground"
        )}
        onClick={() => onSelectFolder?.(null)}
      >
        <Folder className="h-4 w-4" />
        <span className="truncate">All Items</span>
      </Button>

      {data?.rootFolders.map((folder) => (
        <FolderNode
          key={folder.id}
          node={folder}
          level={0}
          selectedFolderId={selectedFolderId}
          onSelectFolder={onSelectFolder}
        />
      ))}
    </div>
  )
}

interface FolderNodeProps {
  node: FolderTreeNode
  level: number
  selectedFolderId?: string | null
  onSelectFolder?: (folderId: string | null) => void
}

function FolderNode({ node, level, selectedFolderId, onSelectFolder }: FolderNodeProps) {
  const [isOpen, setIsOpen] = React.useState(false)
  const hasChildren = node.children && node.children.length > 0
  const isSelected = selectedFolderId === node.id

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div className="flex items-center gap-1 w-full group py-0.5">
        {hasChildren ? (
          <CollapsibleTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 p-0 shrink-0 hover:bg-accent hover:text-accent-foreground"
            >
              <ChevronRight
                className={cn(
                  "h-4 w-4 transition-transform duration-200",
                  isOpen && "rotate-90"
                )}
              />
              <span className="sr-only">Toggle</span>
            </Button>
          </CollapsibleTrigger>
        ) : (
          <div className="w-6 shrink-0" />
        )}

        <Button
          variant="ghost"
          className={cn(
            "flex-1 justify-start gap-2 px-2 h-8 font-normal min-w-0",
            isSelected && "bg-accent text-accent-foreground"
          )}
          onClick={() => onSelectFolder?.(node.id)}
        >
          <Folder className="h-4 w-4 shrink-0 text-muted-foreground group-hover:text-foreground transition-colors" />
          <span className="truncate">{node.name}</span>
        </Button>
      </div>

      {hasChildren && (
        <CollapsibleContent className="pl-4 border-l-2 border-transparent hover:border-border ml-2.5 transition-colors">
          {node.children.map((child) => (
            <FolderNode
              key={child.id}
              node={child}
              level={level + 1}
              selectedFolderId={selectedFolderId}
              onSelectFolder={onSelectFolder}
            />
          ))}
        </CollapsibleContent>
      )}
    </Collapsible>
  )
}
