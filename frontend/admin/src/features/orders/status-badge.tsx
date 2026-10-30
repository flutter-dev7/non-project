import { cn } from '@/lib/utils'

import { statusLabels, type OrderStatus } from './types'

const styles: Record<OrderStatus, { badge: string; dot: string }> = {
  New: { badge: 'bg-sky-50 text-sky-700', dot: 'bg-sky-500' },
  Accepted: { badge: 'bg-indigo-50 text-indigo-700', dot: 'bg-indigo-500' },
  Baking: { badge: 'bg-amber-50 text-amber-700', dot: 'bg-amber-500' },
  OnTheWay: { badge: 'bg-violet-50 text-violet-700', dot: 'bg-violet-500' },
  Delivered: { badge: 'bg-emerald-50 text-emerald-700', dot: 'bg-emerald-500' },
  Cancelled: { badge: 'bg-stone-100 text-stone-500', dot: 'bg-stone-400' },
}

export function StatusBadge({ status }: { status: OrderStatus }) {
  const style = styles[status]

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-bold',
        style.badge,
      )}
    >
      <span className={cn('size-1.5 rounded-full', style.dot)} />
      {statusLabels[status]}
    </span>
  )
}