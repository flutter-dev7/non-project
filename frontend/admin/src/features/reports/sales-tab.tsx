import { useQuery } from '@tanstack/react-query'
import {
  Banknote,
  CalendarDays,
  Loader2,
  Package,
  ShoppingBasket,
  Wallet,
  type LucideIcon,
} from 'lucide-react'
import { useState, type ReactNode } from 'react'
import {
  Area,
  AreaChart,
  CartesianGrid,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { dushanbeDate, formatDate, money } from '@/lib/format'
import { cn } from '@/lib/utils'

import { fetchSales, type SalesDay } from './api'

type Metric = 'amount' | 'orders'

type DayPoint = {
  date: string
  label: string
  amount: number
  orders: number
}

const PALETTE = ['#C8801A', '#7A4E16', '#E3A44A', '#3B2A1A', '#F2C879', '#A8650F']

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

function fillDays(from: string, to: string, byDay: SalesDay[]): DayPoint[] {
  const map = new Map(byDay.map((d) => [d.date.slice(0, 10), d]))
  const result: DayPoint[] = []

  const cursor = new Date(`${from}T00:00:00Z`)
  const end = new Date(`${to}T00:00:00Z`)

  while (cursor <= end) {
    const key = cursor.toISOString().slice(0, 10)
    const day = map.get(key)

    result.push({
      date: key,
      label: `${key.slice(8, 10)}.${key.slice(5, 7)}`,
      amount: day?.amount ?? 0,
      orders: day?.ordersCount ?? 0,
    })

    cursor.setUTCDate(cursor.getUTCDate() + 1)
  }

  return result
}

function SparkCard({
  id,
  icon: Icon,
  label,
  value,
  tone,
  color,
  data,
  footer,
}: {
  id: string
  icon: LucideIcon
  label: string
  value: string
  tone: string
  color: string
  data?: { v: number }[]
  footer?: ReactNode
}) {
  return (
    <div className="rounded-2xl border bg-white p-5 shadow-sm">
      <div className="flex items-center gap-3">
        <div className={cn('flex size-10 items-center justify-center rounded-xl', tone)}>
          <Icon className="size-5" />
        </div>
        <p className="text-sm font-semibold text-stone-500">{label}</p>
      </div>
      <p className="mt-4 text-3xl font-extrabold text-stone-900">{value}</p>
      <div className="mt-2 h-12">
        {data && data.length > 1 ? (
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={data} margin={{ top: 4, right: 0, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={color} stopOpacity={0.35} />
                  <stop offset="100%" stopColor={color} stopOpacity={0} />
                </linearGradient>
              </defs>
              <Area
                type="monotone"
                dataKey="v"
                stroke={color}
                strokeWidth={2.5}
                fill={`url(#${id})`}
                dot={false}
              />
            </AreaChart>
          </ResponsiveContainer>
        ) : (
          footer
        )}
      </div>
    </div>
  )
}

function ChartTooltip({
  active,
  payload,
  metric,
}: {
  active?: boolean
  payload?: { payload: DayPoint }[]
  metric: Metric
}) {
  if (!active || !payload?.length) return null

  const point = payload[0].payload

  return (
    <div className="rounded-xl border bg-white px-4 py-3 shadow-lg">
      <p className="text-xs font-semibold text-stone-500">{formatDate(point.date)}</p>
      <p className="mt-1 text-lg font-extrabold text-stone-900">
        {metric === 'amount' ? money(point.amount) : `${point.orders} зак.`}
      </p>
    </div>
  )
}

export function SalesTab() {
  const [initialFrom, initialTo] = monthRange(0)
  const [from, setFrom] = useState(initialFrom)
  const [to, setTo] = useState(initialTo)
  const [metric, setMetric] = useState<Metric>('amount')

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

  const days = data ? fillDays(data.from, data.to, data.byDay) : []
  const products = (data?.byProduct ?? []).map((p, i) => ({
    ...p,
    fill: PALETTE[i % PALETTE.length],
  }))
  const productsTotal = products.reduce((sum, p) => sum + p.amount, 0)
  const receivedShare =
    data && data.totalAmount > 0 ? Math.min(100, (data.receivedPayments / data.totalAmount) * 100) : 0

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
      ) : error || !data ? (
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
            <SparkCard
              id="spark-orders"
              icon={ShoppingBasket}
              label="Заказов"
              value={String(data.ordersCount)}
              tone="bg-sky-100 text-sky-700"
              color="#0284c7"
              data={days.map((d) => ({ v: d.orders }))}
            />
            <SparkCard
              id="spark-amount"
              icon={Banknote}
              label="Сумма заказов"
              value={money(data.totalAmount)}
              tone="bg-emerald-100 text-emerald-700"
              color="#059669"
              data={days.map((d) => ({ v: d.amount }))}
            />
            <SparkCard
              id="spark-qty"
              icon={Package}
              label="Продано, шт."
              value={String(data.totalQuantity)}
              tone="bg-amber-100 text-amber-700"
              color="#C8801A"
              footer={
                <p className="pt-3 text-xs text-stone-500">
                  {data.ordersCount > 0
                    ? `В среднем ${(data.totalQuantity / data.ordersCount).toFixed(1)} шт. на заказ`
                    : 'Нет заказов'}
                </p>
              }
            />
            <SparkCard
              id="spark-paid"
              icon={Wallet}
              label="Получено оплат"
              value={money(data.receivedPayments)}
              tone="bg-violet-100 text-violet-700"
              color="#7c3aed"
              footer={
                <div className="pt-2">
                  <div className="h-2 overflow-hidden rounded-full bg-stone-100">
                    <div
                      className="h-full rounded-full bg-violet-500 transition-all"
                      style={{ width: `${receivedShare}%` }}
                    />
                  </div>
                  <p className="mt-1.5 text-xs text-stone-500">
                    {receivedShare.toFixed(0)}% от суммы заказов
                  </p>
                </div>
              }
            />
          </div>

          {data.ordersCount === 0 ? (
            <div className="rounded-2xl border bg-white py-20 text-center">
              <p className="font-bold text-stone-800">За этот период заказов нет</p>
              <p className="mt-1 text-sm text-stone-500">Выберите другой период</p>
            </div>
          ) : (
            <>
              <div className="rounded-2xl border bg-white p-6 shadow-sm">
                <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h2 className="text-lg font-extrabold text-stone-900">Динамика по дням</h2>
                    <p className="text-sm text-stone-500">
                      {formatDate(data.from)} — {formatDate(data.to)}
                    </p>
                  </div>
                  <div className="inline-flex gap-1 rounded-xl bg-stone-100 p-1">
                    {(
                      [
                        ['amount', 'Сумма'],
                        ['orders', 'Заказы'],
                      ] as const
                    ).map(([id, label]) => (
                      <button
                        key={id}
                        type="button"
                        onClick={() => setMetric(id)}
                        className={cn(
                          'rounded-lg px-4 py-1.5 text-sm font-bold transition-all',
                          metric === id
                            ? 'bg-white text-stone-900 shadow-sm'
                            : 'text-stone-500 hover:text-stone-800',
                        )}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="h-80">
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={days} margin={{ top: 10, right: 12, left: 0, bottom: 0 }}>
                      <defs>
                        <linearGradient id="main-fill" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="0%" stopColor="#C8801A" stopOpacity={0.35} />
                          <stop offset="100%" stopColor="#C8801A" stopOpacity={0.02} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="4 4" vertical={false} stroke="#efe7dc" />
                      <XAxis
                        dataKey="label"
                        tickLine={false}
                        axisLine={false}
                        tick={{ fill: '#78716c', fontSize: 12 }}
                        interval="preserveStartEnd"
                        minTickGap={24}
                      />
                      <YAxis
                        tickLine={false}
                        axisLine={false}
                        width={44}
                        allowDecimals={false}
                        tick={{ fill: '#78716c', fontSize: 12 }}
                        tickFormatter={compact}
                      />
                      <Tooltip
                        content={<ChartTooltip metric={metric} />}
                        cursor={{ stroke: '#C8801A', strokeDasharray: '4 4' }}
                      />
                      <Area
                        type="monotone"
                        dataKey={metric === 'amount' ? 'amount' : 'orders'}
                        stroke="#C8801A"
                        strokeWidth={3}
                        fill="url(#main-fill)"
                        dot={
                          days.length <= 45
                            ? { r: 3.5, fill: '#fff', stroke: '#C8801A', strokeWidth: 2 }
                            : false
                        }
                        activeDot={{ r: 6, fill: '#C8801A', stroke: '#fff', strokeWidth: 3 }}
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </div>

              <div className="grid gap-6 xl:grid-cols-[2fr_3fr]">
                <div className="rounded-2xl border bg-white p-6 shadow-sm">
                  <h2 className="mb-2 text-lg font-extrabold text-stone-900">Доли товаров</h2>
                  <p className="text-sm text-stone-500">По сумме продаж</p>

                  <div className="relative mx-auto mt-4 h-64 w-64">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={products}
                          dataKey="amount"
                          nameKey="productName"
                          innerRadius={78}
                          outerRadius={116}
                          paddingAngle={3}
                          cornerRadius={8}
                          stroke="none"
                        />
                        <Tooltip
                          formatter={(value) => money(Number(value))}
                          contentStyle={{ borderRadius: 12, border: '1px solid #e7e5e4' }}
                        />
                      </PieChart>
                    </ResponsiveContainer>
                    <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
                      <p className="text-xs font-semibold text-stone-500">Всего</p>
                      <p className="text-xl font-extrabold text-stone-900">
                        {money(productsTotal)}
                      </p>
                    </div>
                  </div>
                </div>

                <div className="rounded-2xl border bg-white p-6 shadow-sm">
                  <h2 className="mb-5 text-lg font-extrabold text-stone-900">По товарам</h2>
                  <ul className="divide-y divide-stone-100">
                    {products.map((p) => (
                      <li key={p.productId} className="flex items-center gap-4 py-3">
                        <span
                          className="size-3.5 shrink-0 rounded-full"
                          style={{ backgroundColor: p.fill }}
                        />
                        <div className="min-w-0 flex-1">
                          <p className="truncate font-bold text-stone-900">{p.productName}</p>
                          <p className="text-xs text-stone-500">{p.quantity} шт.</p>
                        </div>
                        <p className="text-sm font-semibold text-stone-500">
                          {productsTotal > 0 ? ((p.amount / productsTotal) * 100).toFixed(0) : 0}%
                        </p>
                        <p className="w-28 text-right font-extrabold text-stone-900">
                          {money(p.amount)}
                        </p>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </>
          )}
        </>
      )}
    </div>
  )
}