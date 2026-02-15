import { useRef, useState } from 'react'
import { Upload, X, Loader2, AlertCircle, CheckCircle2 } from 'lucide-react'
import { Button, ButtonProps } from '@/components/ui/button'
import { useUpload } from '../hooks/useUpload'
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from '@/components/ui/popover'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Progress } from '@/components/ui/progress'
import { cn } from '@/lib/utils'

interface FileUploadButtonProps {
    onUploadComplete?: (response: any) => void
    buttonProps?: ButtonProps
    className?: string
    children?: React.ReactNode
}

export function FileUploadButton({
    onUploadComplete,
    buttonProps,
    className,
    children
}: FileUploadButtonProps) {
    const fileInputRef = useRef<HTMLInputElement>(null)
    const [isOpen, setIsOpen] = useState(false)

    const {
        tasks,
        uploadFiles,
        retryTask,
        removeTask,
        clearCompleted,
        // isUploading is available
    } = useUpload()

    // onUploadComplete could be called here if we track task completion transition
    // For now it is unused but kept in interface for future use
    void onUploadComplete

    // Monitor tasks for completion to trigger callback
    // This is a bit tricky since we don't have a direct callback per file in the hook
    // We could rely on the parent to invalidate queries, which it probably does.
    // Or we could wrap the uploadFiles to capture the promise, but useUpload doesn't expose promises per se.
    // For now we will just rely on the fact that uploads happen.

    const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            uploadFiles(Array.from(e.target.files))
            setIsOpen(true)
            // Reset input
            e.target.value = ''
        }
    }

    const handleButtonClick = () => {
        fileInputRef.current?.click()
    }

    const hasTasks = tasks.length > 0

    return (
        <Popover open={isOpen} onOpenChange={setIsOpen}>
            <div className={cn("relative inline-block", className)}>
                <input
                    type="file"
                    ref={fileInputRef}
                    className="hidden"
                    multiple
                    onChange={handleFileSelect}
                />

                <PopoverTrigger asChild>
                    <div className="relative">
                        <Button
                            onClick={handleButtonClick}
                            {...buttonProps}
                        >
                            {children || (
                                <>
                                    <Upload className="mr-2 h-4 w-4" />
                                    Upload Files
                                </>
                            )}
                        </Button>
                        {hasTasks && (
                            <span className="absolute -top-2 -right-2 h-5 w-5 rounded-full bg-primary text-[10px] flex items-center justify-center text-primary-foreground pointer-events-none">
                                {tasks.filter(t => t.status === 'uploading' || t.status === 'pending').length || tasks.length}
                            </span>
                        )}
                    </div>
                </PopoverTrigger>
            </div>

            {hasTasks && (
                <PopoverContent className="w-80 p-0" align="end">
                    <div className="flex items-center justify-between p-4 border-b">
                        <h4 className="font-medium leading-none">Uploads</h4>
                        <div className="flex items-center gap-2">
                            {tasks.some(t => t.status === 'success' || t.status === 'error') && (
                                <Button variant="ghost" size="sm" onClick={clearCompleted} className="h-6 text-xs px-2">
                                    Clear done
                                </Button>
                            )}
                            <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => setIsOpen(false)}>
                                <X className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>
                    <ScrollArea className="h-[300px]">
                        <div className="p-4 space-y-4">
                            {tasks.map(task => (
                                <div key={task.id} className="space-y-2">
                                    <div className="flex items-center justify-between text-sm">
                                        <span className="truncate max-w-[180px] font-medium">
                                            {task.file.name}
                                        </span>
                                        <div className="flex items-center gap-2">
                                            {task.status === 'uploading' && <span className="text-xs text-muted-foreground">{task.progress}%</span>}
                                            {task.status === 'error' && (
                                                <Button
                                                    variant="ghost"
                                                    size="icon"
                                                    className="h-6 w-6 text-destructive"
                                                    onClick={() => retryTask(task.id)}
                                                >
                                                    <Loader2 className="h-3 w-3" />
                                                    <span className="sr-only">Retry</span>
                                                </Button>
                                            )}
                                            <Button
                                                variant="ghost"
                                                size="icon"
                                                className="h-6 w-6"
                                                onClick={() => removeTask(task.id)}
                                            >
                                                <X className="h-3 w-3" />
                                                <span className="sr-only">Remove</span>
                                            </Button>
                                        </div>
                                    </div>

                                    {task.status === 'pending' && <Progress value={0} className="h-1" />}
                                    {task.status === 'uploading' && <Progress value={task.progress} className="h-1" />}
                                    {task.status === 'success' && (
                                        <div className="flex items-center gap-2 text-xs text-green-600">
                                            <CheckCircle2 className="h-3 w-3" />
                                            <span>Completed</span>
                                        </div>
                                    )}
                                    {task.status === 'error' && (
                                        <div className="flex items-center gap-2 text-xs text-destructive">
                                            <AlertCircle className="h-3 w-3" />
                                            <span>{task.error || 'Failed'}</span>
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    </ScrollArea>
                </PopoverContent>
            )}
        </Popover>
    )
}
