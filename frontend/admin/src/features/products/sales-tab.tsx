import { useQuery } from '@tanstack/react-query'
import { Banknote, CalendarDays, Loader2, Package, ShoppingBasket, Wallet } from 'lucide-react'
import { useState, type JSXElementConstructor, type Key, type ReactElement, type ReactNode, type ReactPortal } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { StatCard } from '@/components/stat-card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { dushanbeDate, formatDate, money } from '@/lib/format'
import { fetchSales } from '../reports/api'


function monthRange(offset: number): [string, string] {
  const today = dushanbeDate(0)
  const year = Number(today.slice(0, 4))
  const month = Number(today.slice(5, 7)) - 1 + offset

  const first = new Date(Date.UTC(year, month, 1))
  const last = new Date(Date.UTC(year, month + 1, 0))

  const fmt = (d: Date) => d.toISOString().slice(0, 10)

  return offset === 0 ? [fmt(first), today] : [fmt(first), fmt(last)]
}

const compact = (value: number) =>
  new Intl.NumberFormat('ru-RU', { notation: 'compact' }).format(value)

export function SalesTab() {
  const [initialFrom, initialTo] = monthRange(0)
  const [from, setFrom] = useState(initialFrom)
  const [to, setTo] = useState(initialTo)

  const valid = !!from && !!to && from <= to

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['reports', 'sales', from, to],
    queryFn: () => fetchSales(from, to),
    enabled: valid,
  })

  const setRange = (a: string, b: string) => {
    setFrom(a)
    setTo(b)
  }

  const days = (data?.byDay ?? []).map((d: { date: string }) => ({
    ...d,
    label: d.date.slice(8, 10) + '.' + d.date.slice(5, 7),
  }))
  const products = data?.byProduct ?? []
  const maxAmount = Math.max(1, ...products.map((p: { amount: any }) => p.amount))

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3 rounded-2xl border bg-white p-4 shadow-sm">
        <div className="flex items-center gap-2 text-sm font-semibold text-stone-600">
          <CalendarDays className="size-5 text-stone-400" />
          Период
        </div>
        <Input
          type="date"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
          className="h-10 w-40 rounded-xl"
        />
        <span className="text-stone-400">—</span>
        <Input
          type="date"
          value={to}
          onChange={(e) => setTo(e.target.value)}
          className="h-10 w-40 rounded-xl"
        />

        <div className="flex flex-wrap gap-2 sm:ml-2">
          <Button
            variant="outline"
            className="h-10 rounded-xl"
            onClick={() => setRange(dushanbeDate(-6), dushanbeDate(0))}
          >
            7 дней
          </Button>
          <Button
            variant="outline"
            className="h-10 rounded-xl"
            onClick={() => setRange(...monthRange(0))}
          >
            Этот месяц
          </Button>
          <Button
            variant="outline"
            className="h-10 rounded-xl"
            onClick={() => setRange(...monthRange(-1))}
          >
            Прошлый месяц
          </Button>
        </div>
      </div>

      {!valid ? (
        <p className="py-10 text-center text-sm text-amber-700">
          Выберите корректный период: дата «с» не позже даты «по».
        </p>
      ) : isLoading ? (
        <div className="flex justify-center py-24">
          <Loader2 className="size-8 animate-spin text-primary" />
        </div>
      ) : error ? (
        <div className="rounded-2xl border bg-white py-20 text-center">
          <p className="font-semibold text-red-600">
            {error instanceof Error ? error.message : 'Не удалось загрузить отчёт'}
          </p>
          <Button variant="outline" className="mt-4" onClick={() => refetch()}>
            Повторить
          </Button>
        </div>
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <StatCard
              icon={ShoppingBasket}
              label="Заказов"
              value={data?.ordersCount}
              tone="bg-sky-100 text-sky-700"
            />
            <StatCard
              icon={Package}
              label="Продано, шт."
              value={data?.totalQuantity}
              tone="bg-amber-100 text-amber-700"
            />
            <StatCard
              icon={Banknote}
              label="Сумма заказов"
              value={data ? money(data.totalAmount) : undefined}
              tone="bg-emerald-100 text-emerald-700"
            />
            <StatCard
              icon={Wallet}
              label="Получено оплат"
              value={data ? money(data.receivedPayments) : undefined}
              tone="bg-violet-100 text-violet-700"
            />
          </div>

          {data && data.ordersCount === 0 ? (
            <div className="rounded-2xl border bg-white py-20 text-center">
              <p className="font-bold text-stone-800">За этот период заказов нет</p>
              <p className="mt-1 text-sm text-stone-500">Выберите другой период</p>
            </div>
          ) : (
            <div className="grid gap-6 xl:grid-cols-[3fr_2fr]">
              <div className="rounded-2xl border bg-white p-6 shadow-sm">
                <h2 className="mb-6 text-lg font-extrabold text-stone-900">Сумма по дням</h2>
                <div className="h-72">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={days} margin={{ top: 4, right: 4, left: 0, bottom: 0 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e7e5e4" />
                      <XAxis
                        dataKey="label"
                        tickLine={false}
                        axisLine={false}
                        tick={{ fill: '#78716c', fontSize: 12 }}
                      />
                      <YAxis
                        tickLine={false}
                        axisLine={false}
                        width={48}
                        tick={{ fill: '#78716c', fontSize: 12 }}
                        tickFormatter={compact}
                      />
                      <Tooltip
                        cursor={{ fill: 'rgba(200,128,26,0.08)' }}
                        formatter={(value) => [money(Number(value)), 'Сумма']}
                        labelFormatter={(_, payload) => {
                          const date = payload?.[0]?.payload?.date as string | undefined

                          return date ? formatDate(date) : ''
                        }}
                        contentStyle={{ borderRadius: 12, border: '1px solid #e7e5e4' }}
                      />
                      <Bar dataKey="amount" fill="#C8801A" radius={[6, 6, 0, 0]} maxBarSize={40} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </div>

              <div className="rounded-2xl border bg-white p-6 shadow-sm">
                <h2 className="mb-5 text-lg font-extrabold text-stone-900">По товарам</h2>
                <ul className="space-y-5">
                  {products.map((p: { productId: Key | null | undefined; productName: string | number | bigint | boolean | ReactElement<unknown, string | JSXElementConstructor<any>> | Iterable<ReactNode> | ReactPortal | Promise<string | number | bigint | boolean | ReactPortal | ReactElement<unknown, string | JSXElementConstructor<any>> | Iterable<ReactNode> | null | undefined> | null | undefined; amount: number; quantity: string | number | bigint | boolean | ReactElement<unknown, string | JSXElementConstructor<any>> | Iterable<ReactNode> | ReactPortal | Promise<string | number | bigint | boolean | ReactPortal | ReactElement<unknown, string | JSXElementConstructor<any>> | Iterable<ReactNode> | null | undefined> | null | undefined }) => (
                    <li key={p.productId}>
                      <div className="flex items-baseline justify-between gap-3">
                        <p className="truncate font-bold text-stone-900">{p.productName}</p>
                        <p className="shrink-0 font-extrabold text-stone-900">{money(p.amount)}</p>
                      </div>
                      <p className="text-xs text-stone-500">{p.quantity} шт.</p>
                      <div className="mt-2 h-2 overflow-hidden rounded-full bg-stone-100">
                        <div
                          className="h-full rounded-full bg-gradient-to-r from-[#C8801A] to-[#E3A44A]"
                          style={{ width: `${(p.amount / maxAmount) * 100}%` }}
                        />
                      </div>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}