import type { LucideIcon } from 'lucide-react'

import { cn } from '@/lib/utils'

export function StatCard({
  icon: Icon,
  label,
  value,
  tone,
}: {
  icon: LucideIcon
  label: string
  value: string | number | undefined
  tone: string
}) {
  return (
    <div className="flex items-center gap-4 rounded-2xl border bg-white p-5 shadow-sm">
      <div className={cn('flex size-12 shrink-0 items-center justify-center rounded-xl', tone)}>
        <Icon className="size-6" />
      </div>
      <div className="min-w-0">
        <p className="truncate text-2xl font-extrabold leading-none text-stone-900">
          {value ?? '–'}
        </p>
        <p className="mt-1.5 text-sm font-medium text-stone-500">{label}</p>
      </div>
    </div>
  )
}