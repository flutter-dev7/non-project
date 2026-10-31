import { useState } from 'react'

import { cn } from '@/lib/utils'

import { DebtsTab } from './debts-tab'
import { ShopsTab } from './shops-tab'

type Tab = 'shops' | 'debts'

const tabs: { id: Tab; label: string }[] = [
  { id: 'shops', label: 'Магазины' },
  { id: 'debts', label: 'Долги' },
]

export function ShopsPage() {
  const [tab, setTab] = useState<Tab>('shops')

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div>
        <h1 className="text-3xl font-extrabold text-stone-900">Магазины и долги</h1>
        <p className="mt-1 text-stone-500">Клиенты, лимиты, оплаты и задолженность</p>
      </div>

      <div className="inline-flex gap-1 rounded-xl bg-stone-200/70 p-1">
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

      {tab === 'shops' ? <ShopsTab /> : <DebtsTab />}
    </div>
  )
}