import { useState } from 'react'
import { FolderTree } from '../components/FolderTree'
import { ItemList } from '../components/ItemList'
import { NoteEditor } from '../components/NoteEditor'
import { FileLinkDialog } from '../components/FileLinkDialog'
import { CreateFolderDialog } from '../components/CreateFolderDialog'
import { FilePreviewSheet } from '@/components/shared/FilePreviewSheet'
import { Button } from '@/components/ui/button'
import { Plus, Link2 } from 'lucide-react'
import { KnowledgeItem } from '../types'
import { Separator } from "@/components/ui/separator"

export function KnowledgeBasePage() {
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null)
  const [noteEditorOpen, setNoteEditorOpen] = useState(false)
  const [fileLinkOpen, setFileLinkOpen] = useState(false)
  const [createFolderOpen, setCreateFolderOpen] = useState(false)
  const [editingItem, setEditingItem] = useState<KnowledgeItem | undefined>(undefined)
  const [previewSource, setPreviewSource] = useState<{ id: string; name: string } | null>(null)

  const handleViewSource = (item: KnowledgeItem) => {
    if (item.sourceUploadId) {
      setPreviewSource({
        id: item.sourceUploadId,
        name: item.sourceUploadFileName || item.title,
      })
    }
  }

  const handleEditItem = (item: KnowledgeItem) => {
    setEditingItem(item)
    setNoteEditorOpen(true)
  }

  const handleCreateNote = () => {
    setEditingItem(undefined)
    setNoteEditorOpen(true)
  }

  return (
    <div className="flex h-[calc(100vh-4rem)]">
      {/* Left Sidebar - Folder Tree */}
      <div className="w-64 border-r p-4 flex flex-col gap-4">
        <div className="flex items-center justify-between px-2">
          <div className="font-semibold text-lg">Knowledge Base</div>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => setCreateFolderOpen(true)}
            title="New Folder"
          >
            <Plus className="h-4 w-4" />
          </Button>
        </div>
        <Separator />
        <FolderTree
          selectedFolderId={selectedFolderId}
          onSelectFolder={setSelectedFolderId}
          className="flex-1 overflow-y-auto"
        />
      </div>

      {/* Main Content - Item List */}
      <div className="flex-1 flex flex-col min-w-0">
        <div className="border-b p-4 flex justify-between items-center h-16 shrink-0">
          <h2 className="text-lg font-medium">
            {selectedFolderId ? "Folder Items" : "All Items"}
          </h2>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setFileLinkOpen(true)}
              disabled={!selectedFolderId}
              title={!selectedFolderId ? "Select a folder to link files" : "Link a file"}
            >
              <Link2 className="h-4 w-4 mr-2" />
              Link File
            </Button>
            <Button
              size="sm"
              onClick={handleCreateNote}
              disabled={!selectedFolderId}
              title={!selectedFolderId ? "Select a folder to create notes" : "Create a note"}
            >
              <Plus className="h-4 w-4 mr-2" />
              New Note
            </Button>
          </div>
        </div>

        <div className="flex-1 p-4 overflow-hidden">
          <ItemList
            folderId={selectedFolderId}
            onEditItem={handleEditItem}
            onViewSource={handleViewSource}
          />
        </div>
      </div>

      <CreateFolderDialog
        open={createFolderOpen}
        onOpenChange={setCreateFolderOpen}
        parentFolderId={selectedFolderId}
      />

      <NoteEditor
        open={noteEditorOpen}
        onOpenChange={setNoteEditorOpen}
        folderId={selectedFolderId || ""}
        item={editingItem}
        onSuccess={() => {
          // ItemList will auto-refresh via React Query
        }}
      />

      <FileLinkDialog
        open={fileLinkOpen}
        onOpenChange={setFileLinkOpen}
        folderId={selectedFolderId || ""}
        onSuccess={() => {
          // ItemList will auto-refresh via React Query
        }}
      />

      <FilePreviewSheet
        open={!!previewSource}
        onOpenChange={(open) => !open && setPreviewSource(null)}
        fileId={previewSource?.id ?? null}
        fileName={previewSource?.name ?? null}
      />
    </div>
  )
}
