import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog'
import { cn } from '@/lib/utils'

export function AppDialog({
  open,
  onClose,
  icon: Icon,
  title,
  subtitle,
  wide,
  children,
}: {
  open: boolean
  onClose: () => void
  icon: LucideIcon
  title: string
  subtitle?: string
  wide?: boolean
  children: ReactNode
}) {
  return (
    <Dialog open={open} onOpenChange={(value) => !value && onClose()}>
      <DialogContent
        className={cn(
          'max-h-[92vh] gap-0 overflow-y-auto rounded-3xl p-0 sm:max-w-md',
          wide && 'sm:max-w-3xl',
        )}
      >
        <div className="relative overflow-hidden border-b bg-gradient-to-br from-amber-50 to-orange-50 px-7 pb-5 pt-7">
          <div className="pointer-events-none absolute -right-8 -top-8 size-32 rounded-full bg-[#C8801A]/15 blur-2xl" />
          <div className="relative flex items-center gap-4 pr-8">
            <div className="flex size-12 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-[#7A4E16] to-[#C8801A] text-white shadow-lg shadow-amber-900/20">
              <Icon className="size-6" />
            </div>
            <div className="min-w-0">
              <DialogTitle className="truncate text-xl font-extrabold text-[#3B2A1A]">
                {title}
              </DialogTitle>
              <DialogDescription className="mt-0.5 text-sm text-stone-500">
                {subtitle ?? ''}
              </DialogDescription>
            </div>
          </div>
        </div>
        <div className="p-7">{children}</div>
      </DialogContent>
    </Dialog>
  )
}