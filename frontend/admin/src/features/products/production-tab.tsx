import { useQuery } from '@tanstack/react-query'
import { Loader2, Printer, RefreshCw, ShoppingBasket, Wheat } from 'lucide-react'
import { useState } from 'react'

import { StatCard } from '@/components/stat-card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { dushanbeDate, formatDate } from '@/lib/format'
import { cn } from '@/lib/utils'
import { fetchProduction } from '../reports/api'


export function ProductionTab() {
  const [date, setDate] = useState(dushanbeDate(1))

  const { data, isLoading, isFetching, error, refetch } = useQuery({
    queryKey: ['reports', 'production', date],
    queryFn: () => fetchProduction(date),
    enabled: !!date,
  })

  const today = dushanbeDate(0)
  const tomorrow = dushanbeDate(1)
  const items = data?.items ?? []
  const max = Math.max(1, ...items.map((i: { quantity: any }) => i.quantity))

  return (
    <div className="space-y-6">
      <div className="hidden print:block">
        <h1 className="text-2xl font-extrabold">План производства на {formatDate(date)}</h1>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-2xl border bg-white p-4 shadow-sm print:hidden">
        <Input
          type="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          className="h-10 w-44 rounded-xl"
        />
        <Button
          variant={date === today ? 'default' : 'outline'}
          className="h-10 rounded-xl"
          onClick={() => setDate(today)}
        >
          Сегодня
        </Button>
        <Button
          variant={date === tomorrow ? 'default' : 'outline'}
          className="h-10 rounded-xl"
          onClick={() => setDate(tomorrow)}
        >
          Завтра
        </Button>

        <div className="ml-auto flex gap-2">
          <Button
            variant="outline"
            className="h-10 rounded-xl"
            onClick={() => refetch()}
            disabled={isFetching}
          >
            <RefreshCw className={cn('size-4', isFetching && 'animate-spin')} />
            Обновить
          </Button>
          <Button className="h-10 rounded-xl font-bold" onClick={() => window.print()}>
            <Printer className="size-4" />
            Печать
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-24">
          <Loader2 className="size-8 animate-spin text-primary" />
        </div>
      ) : error ? (
        <div className="rounded-2xl border bg-white py-20 text-center">
          <p className="font-semibold text-red-600">Не удалось загрузить план</p>
          <Button variant="outline" className="mt-4" onClick={() => refetch()}>
            Повторить
          </Button>
        </div>
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2">
            <StatCard
              icon={ShoppingBasket}
              label="Заказов на эту дату"
              value={data?.ordersCount}
              tone="bg-sky-100 text-sky-700"
            />
            <StatCard
              icon={Wheat}
              label="Всего испечь, шт."
              value={data?.totalQuantity}
              tone="bg-amber-100 text-amber-700"
            />
          </div>

          <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
            <div className="border-b px-6 py-4">
              <h2 className="text-lg font-extrabold text-stone-900">Что печь</h2>
            </div>

            {items.length === 0 ? (
              <div className="py-20 text-center">
                <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-stone-100 text-stone-400">
                  <Wheat className="size-7" />
                </div>
                <p className="mt-4 font-bold text-stone-800">На эту дату заказов нет</p>
                <p className="mt-1 text-sm text-stone-500">Выберите другой день</p>
              </div>
            ) : (
              <ul className="divide-y divide-stone-100">
                {items.map((item) => (
                  <li key={item.productId} className="px-6 py-4">
                    <div className="flex items-baseline justify-between gap-4">
                      <p className="text-lg font-bold text-stone-900">{item.productName}</p>
                      <p className="text-3xl font-extrabold text-stone-900">
                        {item.quantity}
                        <span className="ml-1 text-base font-semibold text-stone-400">шт.</span>
                      </p>
                    </div>
                    <div className="mt-3 h-2.5 overflow-hidden rounded-full bg-stone-100">
                      <div
                        className="h-full rounded-full bg-gradient-to-r from-[#C8801A] to-[#E3A44A]"
                        style={{ width: `${(item.quantity / max) * 100}%` }}
                      />
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </>
      )}
    </div>
  )
}