import { useState } from 'react'
import { FileSelector } from '@/features/storage/components/FileSelector'
import { FileUploadButton } from '@/features/storage/components/FileUploadButton'
import { NormalizedFileItem } from '@/features/storage/api/storage-api'
import { Separator } from '@/components/ui/separator'

export function StorageComponentsDemoPage() {
    const [selectedFiles, setSelectedFiles] = useState<NormalizedFileItem[]>([])

    return (
        <div className="space-y-8 p-8">
            <div>
                <h1 className="text-2xl font-bold">Storage Components Demo</h1>
                <p className="text-muted-foreground">Demonstrating reusable storage components.</p>
            </div>

            <Separator />

            <div className="space-y-4">
                <h2 className="text-xl font-semibold">File Upload Button</h2>
                <div className="p-4 border rounded-lg bg-muted/20">
                    <p className="mb-4 text-sm text-muted-foreground">
                        This button can be placed anywhere. It manages its own upload state and shows progress in a popover.
                    </p>
                    <FileUploadButton />
                </div>
            </div>

            <Separator />

            <div className="space-y-4">
                <h2 className="text-xl font-semibold">File Selector</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    <div className="p-4 border rounded-lg">
                        <h3 className="mb-4 font-medium">Selector Component</h3>
                        <FileSelector
                            onSelectionChange={setSelectedFiles}
                            multiSelect={true}
                            className="bg-card"
                        />
                    </div>

                    <div className="p-4 border rounded-lg bg-muted/20">
                        <h3 className="mb-4 font-medium">Selection State</h3>
                        <div className="space-y-2">
                            <p className="text-sm font-medium">Selected Files Count: {selectedFiles.length}</p>
                            <ul className="text-sm space-y-1 list-disc list-inside">
                                {selectedFiles.map(file => (
                                    <li key={file.id} className="truncate">{file.filename}</li>
                                ))}
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}
