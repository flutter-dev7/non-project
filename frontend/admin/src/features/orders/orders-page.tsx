import {
    CalendarDays,
    ChevronRight,
    CircleCheck,
    Flame,
    Inbox,
    Loader2,
    Plus,
    RefreshCw,
    Truck,
    User,
    type LucideIcon,
} from 'lucide-react'
import { useState, type ReactNode } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { dushanbeDate, formatDate, formatDateTime, money } from '@/lib/format'
import { cn } from '@/lib/utils'

import { useOrders, useStatusCounts } from './hooks'
import { StatusBadge } from './status-badge'
import {
    ORDER_STATUSES,
    orderNumber,
    paymentLabels,
    statusLabels,
    type Order,
    type OrderStatus,
} from './types'
import { useNavigate } from 'react-router-dom'

const PAGE_SIZE = 20

type StatCardProps = {
    icon: LucideIcon
    label: string
    value: number | undefined
    tone: string
}

function StatCard({ icon: Icon, label, value, tone }: StatCardProps) {
    return (
        <div className="flex items-center gap-4 rounded-2xl border bg-white p-5 shadow-sm">
            <div className={cn('flex size-12 shrink-0 items-center justify-center rounded-xl', tone)}>
                <Icon className="size-6" />
            </div>
            <div>
                <p className="text-3xl font-extrabold leading-none text-stone-900">{value ?? '–'}</p>
                <p className="mt-1.5 text-sm font-medium text-stone-500">{label}</p>
            </div>
        </div>
    )
}

function Chip({
    active,
    count,
    onClick,
    children,
}: {
    active: boolean
    count?: number
    onClick: () => void
    children: ReactNode
}) {
    return (
        <button
            type="button"
            onClick={onClick}
            className={cn(
                'flex items-center gap-2 rounded-full border px-4 py-2 text-sm font-semibold transition-all',
                active
                    ? 'border-primary bg-primary text-primary-foreground shadow-sm'
                    : 'border-stone-200 bg-white text-stone-700 hover:border-stone-300 hover:bg-stone-50',
            )}
        >
            {children}
            {count !== undefined && (
                <span
                    className={cn(
                        'rounded-full px-2 py-0.5 text-xs font-bold',
                        active ? 'bg-white/25' : 'bg-stone-100 text-stone-600',
                    )}
                >
                    {count}
                </span>
            )}
        </button>
    )
}

function DeliveryCell({ order }: { order: Order }) {
    const today = dushanbeDate(0)
    const tomorrow = dushanbeDate(1)
    const date = order.deliveryDate.slice(0, 10)
    const isFinal = order.status === 'Delivered' || order.status === 'Cancelled'

    let tag: { text: string; cls: string } | null = null

    if (!isFinal) {
        if (date === today) tag = { text: 'Сегодня', cls: 'bg-amber-100 text-amber-800' }
        else if (date === tomorrow) tag = { text: 'Завтра', cls: 'bg-sky-100 text-sky-800' }
        else if (date < today) tag = { text: 'Просрочен', cls: 'bg-red-100 text-red-700' }
    }

    return (
        <div>
            <p className="font-semibold text-stone-800">{formatDate(order.deliveryDate)}</p>
            {tag && (
                <span
                    className={cn(
                        'mt-1 inline-block rounded-md px-1.5 py-0.5 text-[11px] font-bold',
                        tag.cls,
                    )}
                >
                    {tag.text}
                </span>
            )}
        </div>
    )
}

function CourierCell({ order }: { order: Order }) {
    const isFinal = order.status === 'Delivered' || order.status === 'Cancelled'

    if (order.courierName) {
        return (
            <div className="flex items-center gap-2">
                <div className="flex size-7 items-center justify-center rounded-full bg-stone-100 text-stone-500">
                    <User className="size-4" />
                </div>
                <span className="font-medium text-stone-800">{order.courierName}</span>
            </div>
        )
    }

    if (isFinal) return <span className="text-stone-300">—</span>

    return (
        <span className="rounded-md bg-amber-50 px-2 py-1 text-xs font-bold text-amber-700">
            Не назначен
        </span>
    )
}

function OrderRow({ order, onOpen }: { order: Order; onOpen: () => void }) {
    return (
        <tr
            onClick={onOpen}
            className="group cursor-pointer border-b border-stone-100 transition-colors last:border-0 hover:bg-amber-50/50"
        >
            <td className="px-5 py-4">
                <p className="font-mono text-sm font-bold text-stone-700">#{orderNumber(order.id)}</p>
                <p className="mt-0.5 text-xs text-stone-400">{formatDateTime(order.createdAt)}</p>
            </td>
            <td className="px-5 py-4">
                <div className="flex items-center gap-3">
                    <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 font-extrabold text-amber-800">
                        {order.shopName.charAt(0).toUpperCase()}
                    </div>
                    <div className="min-w-0">
                        <p className="truncate font-bold text-stone-900">{order.shopName}</p>
                        <p className="truncate text-xs text-stone-500">{order.shopAddress}</p>
                    </div>
                </div>
            </td>
            <td className="px-5 py-4">
                <DeliveryCell order={order} />
            </td>
            <td className="px-5 py-4">
                <StatusBadge status={order.status} />
            </td>
            <td className="px-5 py-4">
                <span
                    className={cn(
                        'rounded-md px-2 py-1 text-xs font-bold',
                        order.paymentType === 'Now'
                            ? 'bg-emerald-50 text-emerald-700'
                            : 'bg-orange-50 text-orange-700',
                    )}
                >
                    {paymentLabels[order.paymentType]}
                </span>
            </td>
            <td className="px-5 py-4 text-right">
                <span className="text-base font-extrabold text-stone-900">{money(order.totalAmount)}</span>
            </td>
            <td className="px-5 py-4">
                <CourierCell order={order} />
            </td>
            <td className="py-4 pr-4">
                <ChevronRight className="size-5 text-stone-300 transition-transform group-hover:translate-x-0.5 group-hover:text-primary" />
            </td>
        </tr>
    )
}

const headCell = 'px-5 py-3 text-left text-xs font-bold uppercase tracking-wide text-stone-500'

export function OrdersPage() {
    const [status, setStatus] = useState<OrderStatus | undefined>()
    const [dateFrom, setDateFrom] = useState('')
    const [dateTo, setDateTo] = useState('')
    const [page, setPage] = useState(1)
    const navigate = useNavigate()

    const { data, isLoading, isFetching, error, refetch } = useOrders({
        status,
        dateFrom,
        dateTo,
        page,
        pageSize: PAGE_SIZE,
    })
    const counts = useStatusCounts(dateFrom, dateTo)

    const orders = data?.items ?? []
    const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / PAGE_SIZE))

    const inProgress =
        counts.Accepted !== undefined && counts.Baking !== undefined
            ? counts.Accepted + counts.Baking
            : undefined

    const changeStatus = (value: OrderStatus | undefined) => {
        setStatus(value)
        setPage(1)
    }

    const setRange = (from: string, to: string) => {
        setDateFrom(from)
        setDateTo(to)
        setPage(1)
    }

    const today = dushanbeDate(0)
    const tomorrow = dushanbeDate(1)
    const isToday = dateFrom === today && dateTo === today
    const isTomorrow = dateFrom === tomorrow && dateTo === tomorrow

    return (
        <div className="mx-auto max-w-7xl space-y-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-3xl font-extrabold text-stone-900">Заказы</h1>
                    <p className="mt-1 text-stone-500">
                        {dateFrom || dateTo ? 'За выбранный период' : 'За всё время'}
                    </p>
                </div>
                <div className="flex gap-2">
                    <Button
                        variant="outline"
                        className="h-10 rounded-xl bg-white"
                        onClick={() => refetch()}
                        disabled={isFetching}
                    >
                        <RefreshCw className={cn('size-4', isFetching && 'animate-spin')} />
                        Обновить
                    </Button>
                    <Button className="h-10 rounded-xl font-bold" onClick={() => navigate('/orders/new')}>
                        <Plus className="size-4" />
                        Новый заказ
                    </Button>
                </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <StatCard icon={Inbox} label="Новые" value={counts.New} tone="bg-sky-100 text-sky-700" />
                <StatCard
                    icon={Flame}
                    label="Принято и выпекается"
                    value={inProgress}
                    tone="bg-amber-100 text-amber-700"
                />
                <StatCard
                    icon={Truck}
                    label="В пути"
                    value={counts.OnTheWay}
                    tone="bg-violet-100 text-violet-700"
                />
                <StatCard
                    icon={CircleCheck}
                    label="Доставлено"
                    value={counts.Delivered}
                    tone="bg-emerald-100 text-emerald-700"
                />
            </div>

            <div className="space-y-5 rounded-2xl border bg-white p-5 shadow-sm">
                <div className="flex flex-wrap gap-2">
                    <Chip active={status === undefined} count={counts.all} onClick={() => changeStatus(undefined)}>
                        Все
                    </Chip>
                    {ORDER_STATUSES.map((s) => (
                        <Chip
                            key={s}
                            active={status === s}
                            count={counts[s]}
                            onClick={() => changeStatus(s)}
                        >
                            {statusLabels[s]}
                        </Chip>
                    ))}
                </div>

                <div className="flex flex-wrap items-center gap-3 border-t pt-5">
                    <div className="flex items-center gap-2 text-sm font-semibold text-stone-600">
                        <CalendarDays className="size-5 text-stone-400" />
                        Доставка
                    </div>
                    <Input
                        type="date"
                        value={dateFrom}
                        onChange={(e) => setRange(e.target.value, dateTo)}
                        className="h-10 w-40 rounded-xl"
                    />
                    <span className="text-stone-400">—</span>
                    <Input
                        type="date"
                        value={dateTo}
                        onChange={(e) => setRange(dateFrom, e.target.value)}
                        className="h-10 w-40 rounded-xl"
                    />

                    <div className="flex gap-2 sm:ml-2">
                        <Button
                            variant={isToday ? 'default' : 'outline'}
                            className="h-10 rounded-xl"
                            onClick={() => setRange(today, today)}
                        >
                            Сегодня
                        </Button>
                        <Button
                            variant={isTomorrow ? 'default' : 'outline'}
                            className="h-10 rounded-xl"
                            onClick={() => setRange(tomorrow, tomorrow)}
                        >
                            Завтра
                        </Button>
                        {(dateFrom || dateTo) && (
                            <Button variant="ghost" className="h-10 rounded-xl" onClick={() => setRange('', '')}>
                                Сбросить
                            </Button>
                        )}
                    </div>
                </div>
            </div>

            <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
                {error ? (
                    <div className="py-20 text-center">
                        <p className="font-semibold text-red-600">Не удалось загрузить заказы</p>
                        <Button variant="outline" className="mt-4" onClick={() => refetch()}>
                            Повторить
                        </Button>
                    </div>
                ) : (
                    <div className="overflow-x-auto">
                        <table className="w-full min-w-[980px] text-sm">
                            <thead className="border-b bg-stone-50">
                                <tr>
                                    <th className={headCell}>Заказ</th>
                                    <th className={headCell}>Магазин</th>
                                    <th className={headCell}>Доставка</th>
                                    <th className={headCell}>Статус</th>
                                    <th className={headCell}>Оплата</th>
                                    <th className={cn(headCell, 'text-right')}>Сумма</th>
                                    <th className={headCell}>Курьер</th>
                                    <th className="w-10" />
                                </tr>
                            </thead>
                            <tbody>
                                {isLoading &&
                                    Array.from({ length: 6 }).map((_, i) => (
                                        <tr key={i} className="border-b border-stone-100">
                                            <td colSpan={8} className="px-5 py-4">
                                                <Skeleton className="h-10 w-full rounded-xl" />
                                            </td>
                                        </tr>
                                    ))}

                                {!isLoading &&
                                    orders.map((order) => (
                                        <OrderRow key={order.id} order={order} onOpen={() => navigate(`/orders/${order.id}`)} />
                                    ))}
                            </tbody>
                        </table>

                        {!isLoading && orders.length === 0 && (
                            <div className="py-20 text-center">
                                <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-stone-100 text-stone-400">
                                    <Inbox className="size-7" />
                                </div>
                                <p className="mt-4 font-bold text-stone-800">Заказов не найдено</p>
                                <p className="mt-1 text-sm text-stone-500">Измените статус или период доставки</p>
                            </div>
                        )}
                    </div>
                )}

                {isFetching && !isLoading && (
                    <div className="flex items-center justify-center gap-2 border-t py-2 text-xs text-stone-400">
                        <Loader2 className="size-3.5 animate-spin" />
                        Обновление…
                    </div>
                )}
            </div>

            {totalPages > 1 && (
                <div className="flex items-center justify-between">
                    <p className="text-sm text-stone-500">
                        Страница {page} из {totalPages} · всего {data?.totalCount}
                    </p>
                    <div className="flex gap-2">
                        <Button
                            variant="outline"
                            className="rounded-xl bg-white"
                            disabled={page <= 1}
                            onClick={() => setPage((p) => p - 1)}
                        >
                            Назад
                        </Button>
                        <Button
                            variant="outline"
                            className="rounded-xl bg-white"
                            disabled={page >= totalPages}
                            onClick={() => setPage((p) => p + 1)}
                        >
                            Вперёд
                        </Button>
                    </div>
                </div>
            )}

        </div>
    )
}