import { useState } from 'react'
import { Input } from "@/components/ui/input"
import { useItems, useDeleteItem } from "../hooks/useItems"
import { ItemCard } from "./ItemCard"
import { ItemTable } from "./ItemTable"
import { Skeleton } from "@/components/ui/skeleton"
import { Search, LayoutGrid, List } from "lucide-react"
import { KnowledgeItem } from "../types"
import { Button } from "@/components/ui/button"

interface ItemListProps {
  folderId?: string | null
  onEditItem?: (item: KnowledgeItem) => void
  onViewSource?: (item: KnowledgeItem) => void
  onViewItem?: (item: KnowledgeItem) => void
}

export function ItemList({ folderId, onEditItem, onViewSource, onViewItem }: ItemListProps) {
  const [search, setSearch] = useState('')
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid')
  const { data, isLoading, isError } = useItems({
    folderId: folderId || undefined,
    includeSubFolders: !!folderId,
    search: search || undefined,
    pageSize: 20
  })
  const deleteItem = useDeleteItem()

  const handleDelete = async (item: KnowledgeItem) => {
    if (confirm(`Are you sure you want to delete "${item.title}"?`)) {
      try {
        await deleteItem.mutateAsync(item.id)
      } catch (error) {
        console.error("Failed to delete item:", error)
      }
    }
  }

  return (
    <div className="space-y-4 h-full flex flex-col">
      <div className="flex items-center justify-between shrink-0 gap-2">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search items..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>

        <div className="flex items-center border rounded-md bg-background shadow-sm">
          <Button
            variant="ghost"
            size="icon"
            className={`h-9 w-9 rounded-none rounded-l-md ${viewMode === 'grid' ? 'bg-accent text-accent-foreground' : 'hover:bg-muted'}`}
            onClick={() => setViewMode('grid')}
            title="Grid View"
          >
            <LayoutGrid className="h-4 w-4" />
          </Button>
          <div className="w-[1px] h-4 bg-border" />
          <Button
            variant="ghost"
            size="icon"
            className={`h-9 w-9 rounded-none rounded-r-md ${viewMode === 'list' ? 'bg-accent text-accent-foreground' : 'hover:bg-muted'}`}
            onClick={() => setViewMode('list')}
            title="List View"
          >
            <List className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <div className="flex-1 min-h-0">
        {isLoading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {[...Array(8)].map((_, i) => (
              <div key={i} className="space-y-3">
                <Skeleton className="h-[125px] w-full rounded-xl" />
                <div className="space-y-2">
                  <Skeleton className="h-4 w-full" />
                  <Skeleton className="h-4 w-2/3" />
                </div>
              </div>
            ))}
          </div>
        ) : isError ? (
          <div className="text-center py-10 text-destructive">
            Failed to load items.
          </div>
        ) : data?.items.length === 0 ? (
          <div className="text-center py-10 text-muted-foreground border-2 border-dashed rounded-xl">
            No items found in this folder.
          </div>
        ) : (
          viewMode === 'grid' ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 pb-4">
              {data?.items.map(item => (
                <ItemCard
                  key={item.id}
                  item={item}
                  onEdit={() => onEditItem?.(item)}
                  onDelete={() => handleDelete(item)}
                  onViewSource={item.sourceUploadId ? () => onViewSource?.(item) : undefined}
                  onViewItem={() => onViewItem?.(item)}
                />
              ))}
            </div>
          ) : (
            <div className="pb-4">
              <ItemTable
                items={data?.items || []}
                onEdit={onEditItem}
                onDelete={handleDelete}
                onViewSource={onViewSource}
                onViewItem={onViewItem}
              />
            </div>
          )
        )}
      </div>
    </div>
  )
}
