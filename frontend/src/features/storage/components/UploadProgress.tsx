import { CheckCircle2, XCircle, Loader2, Clock, X, RotateCcw } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Progress } from '@/components/ui/progress'
import { Button } from '@/components/ui/button'
import type { UploadTask, UploadStatus } from '../hooks/useUpload'

interface UploadProgressProps {
  tasks: UploadTask[]
  onRetry: (taskId: string) => void
  onRemove: (taskId: string) => void
  onClearCompleted: () => void
  className?: string
}

const statusConfig: Record<UploadStatus, { icon: typeof CheckCircle2; color: string; label: string }> = {
  pending: { icon: Clock, color: 'text-muted-foreground', label: 'Pending' },
  uploading: { icon: Loader2, color: 'text-primary', label: 'Uploading' },
  success: { icon: CheckCircle2, color: 'text-green-500', label: 'Complete' },
  error: { icon: XCircle, color: 'text-destructive', label: 'Failed' }
}

export function UploadProgress({
  tasks,
  onRetry,
  onRemove,
  onClearCompleted,
  className
}: UploadProgressProps) {
  if (tasks.length === 0) return null

  const completedCount = tasks.filter(t => t.status === 'success' || t.status === 'error').length
  const hasCompleted = completedCount > 0

  return (
    <div className={cn('space-y-4', className)}>
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">
          Uploading {tasks.filter(t => t.status === 'uploading' || t.status === 'pending').length} of {tasks.length} files
        </h3>
        {hasCompleted && (
          <Button variant="ghost" size="sm" onClick={onClearCompleted}>
            Clear completed
          </Button>
        )}
      </div>

      <div className="space-y-2">
        {tasks.map(task => {
          const config = statusConfig[task.status]
          const Icon = config.icon

          return (
            <div
              key={task.id}
              className={cn(
                'flex items-center gap-3 p-3 rounded-lg border',
                task.status === 'error' && 'border-destructive/50 bg-destructive/5'
              )}
            >
              <Icon className={cn(
                'h-4 w-4 shrink-0',
                config.color,
                task.status === 'uploading' && 'animate-spin'
              )} />

              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{task.file.name}</p>
                {task.status === 'uploading' && (
                  <Progress value={50} className="h-1 mt-1" />
                )}
                {task.status === 'error' && task.error && (
                  <p className="text-xs text-destructive mt-1">{task.error}</p>
                )}
              </div>

              <div className="flex items-center gap-1">
                {task.status === 'error' && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    onClick={() => onRetry(task.id)}
                  >
                    <RotateCcw className="h-3.5 w-3.5" />
                  </Button>
                )}
                {(task.status === 'success' || task.status === 'error') && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    onClick={() => onRemove(task.id)}
                  >
                    <X className="h-3.5 w-3.5" />
                  </Button>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
