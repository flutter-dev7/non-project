import { useState } from 'react'

import { cn } from '@/lib/utils'

import { ProductionTab } from './production-tab'
import { SalesTab } from '../reports/sales-tab'

type Tab = 'production' | 'sales'

const tabs: { id: Tab; label: string }[] = [
  { id: 'production', label: 'План на день' },
  { id: 'sales', label: 'Продажи' },
]

export function PlanPage() {
  const [tab, setTab] = useState<Tab>('production')

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div className="print:hidden">
        <h1 className="text-3xl font-extrabold text-stone-900">План и отчёты</h1>
        <p className="mt-1 text-stone-500">Производство на день и продажи за период</p>
      </div>

      <div className="inline-flex gap-1 rounded-xl bg-stone-200/70 p-1 print:hidden">
        {tabs.map((t) => (
          <button
            key={t.id}
            type="button"
            onClick={() => setTab(t.id)}
            className={cn(
              'rounded-lg px-5 py-2 text-sm font-bold transition-all',
              tab === t.id
                ? 'bg-white text-stone-900 shadow-sm'
                : 'text-stone-600 hover:text-stone-900',
            )}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'production' ? <ProductionTab /> : <SalesTab />}
    </div>
  )
}