import { useState, useCallback, useEffect } from 'react'
import { Search } from 'lucide-react'
import { useFiles } from '../hooks/useFiles'
import { NormalizedFileItem } from '../api/storage-api'
import { FilesTable } from './FilesTable'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'

interface FileSelectorProps {
    onSelectionChange: (files: NormalizedFileItem[]) => void
    multiSelect?: boolean
    className?: string
    initialSelectedIds?: string[]
}

export function FileSelector({
    onSelectionChange,
    multiSelect = true,
    className,
    initialSelectedIds = []
}: FileSelectorProps) {
    const [selectedIds, setSelectedIds] = useState<string[]>(initialSelectedIds)

    const {
        filteredFiles,
        total,
        isLoading,
        page,
        pageSize,
        setPage,
        searchQuery,
        setSearchQuery
    } = useFiles({ pageSize: 10 })

    // Update parent when selection changes
    useEffect(() => {
        // We need to find the full file objects for the selected IDs
        // Note: This only finds files currently loaded/visible in the table/current page or cached
        // If we need to persist selection across pages, we might need a different approach 
        // or rely on the fact that selectedIds are preserved.
        // For now, we will return what we can find.
        const selectedFiles = filteredFiles.filter(f => selectedIds.includes(f.id))
        onSelectionChange(selectedFiles)
    }, [selectedIds, filteredFiles, onSelectionChange])

    const handleSelectionChange = useCallback((ids: string[]) => {
        setSelectedIds(ids)
    }, [])

    const totalPages = Math.ceil(total / pageSize)

    return (
        <div className={className}>
            <div className="flex items-center gap-4 mb-4">
                <div className="relative flex-1">
                    <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                        placeholder="Search files by name or metadata..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="pl-9"
                    />
                </div>
            </div>

            <div className="border rounded-md">
                <FilesTable
                    files={filteredFiles}
                    isLoading={isLoading}
                    selectedIds={selectedIds}
                    onSelect={handleSelectionChange}
                    multiSelect={multiSelect}
                // We don't render actions in the selector view usually
                />
            </div>

            {totalPages > 1 && (
                <div className="flex items-center justify-end gap-2 mt-4">
                    <Button
                        variant="outline"
                        size="sm"
                        disabled={page === 1}
                        onClick={() => setPage(page - 1)}
                    >
                        Previous
                    </Button>
                    <span className="text-sm text-muted-foreground">
                        Page {page} of {totalPages}
                    </span>
                    <Button
                        variant="outline"
                        size="sm"
                        disabled={page >= totalPages}
                        onClick={() => setPage(page + 1)}
                    >
                        Next
                    </Button>
                </div>
            )}

            <div className="mt-2 text-sm text-muted-foreground">
                {selectedIds.length} file{selectedIds.length !== 1 ? 's' : ''} selected
            </div>
        </div>
    )
}
