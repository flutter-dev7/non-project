import { useQuery } from '@tanstack/react-query'
import { Banknote, CircleAlert, Loader2, Store, TriangleAlert } from 'lucide-react'
import { useState } from 'react'

import { StatCard } from '@/components/stat-card'
import { Button } from '@/components/ui/button'
import { fetchDebts } from '@/features/debts/api'
import { money } from '@/lib/format'
import { cn } from '@/lib/utils'

import { ShopDialog } from './shop-dialog'

export function DebtsTab() {
  const [openShop, setOpenShop] = useState<{ id: string; name: string } | null>(null)

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['debts', 'list'],
    queryFn: fetchDebts,
  })

  const items = data?.items ?? []
  const total = items.reduce((sum, d) => sum + d.debt, 0)
  const overLimit = items.filter((d) => d.isOverLimit).length

  if (isLoading) {
    return (
      <div className="flex justify-center py-24">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="rounded-2xl border bg-white py-20 text-center">
        <p className="font-semibold text-red-600">Не удалось загрузить долги</p>
        <Button variant="outline" className="mt-4" onClick={() => refetch()}>
          Повторить
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard
          icon={Banknote}
          label="Общий долг"
          value={money(total)}
          tone="bg-red-100 text-red-700"
        />
        <StatCard
          icon={Store}
          label="Должников"
          value={data?.totalCount ?? items.length}
          tone="bg-amber-100 text-amber-700"
        />
        <StatCard
          icon={TriangleAlert}
          label="Превысили лимит"
          value={overLimit}
          tone="bg-orange-100 text-orange-700"
        />
      </div>

      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        {items.length === 0 ? (
          <div className="py-20 text-center">
            <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
              <CircleAlert className="size-7" />
            </div>
            <p className="mt-4 font-bold text-stone-800">Долгов нет</p>
            <p className="mt-1 text-sm text-stone-500">Все магазины рассчитались</p>
          </div>
        ) : (
          <ul className="divide-y divide-stone-100">
            {items.map((d) => {
              const pct =
                d.debtLimit && d.debtLimit > 0 ? Math.min(100, (d.debt / d.debtLimit) * 100) : null

              return (
                <li
                  key={d.shopId}
                  onClick={() => setOpenShop({ id: d.shopId, name: d.shopName })}
                  className="cursor-pointer px-6 py-4 transition-colors hover:bg-amber-50/50"
                >
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div className="flex items-center gap-3">
                      <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 font-extrabold text-amber-800">
                        {d.shopName.charAt(0).toUpperCase()}
                      </div>
                      <div>
                        <p className="font-bold text-stone-900">{d.shopName}</p>
                        <p className="text-xs text-stone-500">{d.shopPhone}</p>
                      </div>
                    </div>

                    <div className="flex items-center gap-3">
                      {d.isOverLimit && (
                        <span className="rounded-md bg-red-50 px-2 py-1 text-xs font-bold text-red-700">
                          Превышен лимит
                        </span>
                      )}
                      <p className="text-xl font-extrabold text-red-600">{money(d.debt)}</p>
                    </div>
                  </div>

                  {pct !== null && (
                    <div className="mt-3">
                      <div className="h-2 overflow-hidden rounded-full bg-stone-100">
                        <div
                          className={cn(
                            'h-full rounded-full',
                            d.isOverLimit ? 'bg-red-500' : 'bg-gradient-to-r from-[#C8801A] to-[#E3A44A]',
                          )}
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                      <p className="mt-1 text-xs text-stone-500">
                        Лимит {money(d.debtLimit ?? 0)}
                      </p>
                    </div>
                  )}
                </li>
              )
            })}
          </ul>
        )}
      </div>

      <ShopDialog
        shopId={openShop?.id ?? null}
        shopName={openShop?.name ?? ''}
        onClose={() => setOpenShop(null)}
      />
    </div>
  )
}