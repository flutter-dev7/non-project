import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  Ban,
  Check,
  ClipboardCheck,
  Flame,
  Inbox,
  Loader2,
  PackageCheck,
  Phone,
  TriangleAlert,
  Truck,
  User,
} from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { NativeSelect } from '@/components/ui/native-select'
import { ApiError } from '@/lib/api'
import { formatDate, formatDateTime, formatShortDateTime, money } from '@/lib/format'
import { cn } from '@/lib/utils'

import { assignCourier, cancelOrder, changeOrderStatus } from './api'
import { useCouriers, useOrder } from './hooks'
import { StatusBadge } from './status-badge'
import {
  orderNumber,
  paymentLabels,
  sourceLabels,
  statusLabels,
  type Courier,
  type OrderDetails,
  type OrderStatus,
  type PaymentMethod,
} from './types'

const steps = [
  { status: 'New', icon: Inbox },
  { status: 'Accepted', icon: ClipboardCheck },
  { status: 'Baking', icon: Flame },
  { status: 'OnTheWay', icon: Truck },
  { status: 'Delivered', icon: PackageCheck },
] as const

const nextStatus: Partial<Record<OrderStatus, OrderStatus>> = {
  New: 'Accepted',
  Accepted: 'Baking',
  Baking: 'OnTheWay',
  OnTheWay: 'Delivered',
}

const nextLabel: Partial<Record<OrderStatus, string>> = {
  New: 'Принять заказ',
  Accepted: 'Начать выпечку',
  Baking: 'Отправить в путь',
  OnTheWay: 'Отметить доставленным',
}

const nextHint: Partial<Record<OrderStatus, string>> = {
  New: 'Подтвердите заказ, чтобы он пошёл в работу.',
  Accepted: 'Передайте заказ в выпечку.',
  Baking: 'Когда хлеб готов, отправьте заказ курьеру. Для этого должен быть назначен курьер.',
  OnTheWay: 'После передачи хлеба магазину отметьте заказ доставленным.',
}

function courierName(c: Courier): string {
  return c.fullName || `${c.firstName ?? ''} ${c.lastName ?? ''}`.trim()
}

function errorMessage(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Не удалось выполнить действие.'
}

function Panel({
  title,
  action,
  children,
}: {
  title: string
  action?: ReactNode
  children: ReactNode
}) {
  return (
    <section className="rounded-2xl border bg-white p-6 shadow-sm">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-base font-extrabold text-stone-900">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  )
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-4 py-2.5 text-sm">
      <dt className="shrink-0 text-stone-500">{label}</dt>
      <dd className="text-right font-semibold text-stone-900">{children}</dd>
    </div>
  )
}

function Stepper({ order }: { order: OrderDetails }) {
  const currentIndex = steps.findIndex((s) => s.status === order.status)
  const delivered = order.status === 'Delivered'
  const times = new Map<OrderStatus, string>()

  order.history.forEach((h) => times.set(h.status, h.changedAt))

  return (
    <div className="rounded-2xl border bg-white p-6 shadow-sm">
      <ol className="flex items-start">
        {steps.map((step, i) => {
          const done = delivered || i < currentIndex
          const current = !delivered && i === currentIndex
          const Icon = step.icon
          const time = times.get(step.status)

          return (
            <li key={step.status} className="relative flex flex-1 flex-col items-center text-center">
              {i > 0 && (
                <span
                  className={cn(
                    'absolute right-1/2 top-[18px] h-1 w-full rounded-full',
                    done || current ? 'bg-primary' : 'bg-stone-200',
                  )}
                />
              )}
              <span
                className={cn(
                  'relative z-10 flex size-10 items-center justify-center rounded-full border-2 transition-all',
                  done && 'border-primary bg-primary text-white',
                  current && 'border-primary bg-white text-primary ring-4 ring-primary/20',
                  !done && !current && 'border-stone-200 bg-white text-stone-300',
                )}
              >
                {done ? <Check className="size-5" /> : <Icon className="size-5" />}
              </span>
              <p
                className={cn(
                  'mt-2 text-xs font-bold sm:text-sm',
                  done || current ? 'text-stone-900' : 'text-stone-400',
                )}
              >
                {statusLabels[step.status]}
              </p>
              <p className="mt-0.5 min-h-4 text-[11px] text-stone-400">
                {time ? formatShortDateTime(time) : ''}
              </p>
            </li>
          )
        })}
      </ol>
    </div>
  )
}

export function OrderPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: order, isLoading, error: loadError } = useOrder(id)
  const couriers = useCouriers()

  const [payOpen, setPayOpen] = useState(false)
  const [amount, setAmount] = useState('')
  const [method, setMethod] = useState<PaymentMethod>('Cash')
  const [confirmCancel, setConfirmCancel] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const onDone = () => {
    setError(null)
    setPayOpen(false)
    setConfirmCancel(false)
    queryClient.invalidateQueries({ queryKey: ['orders'] })
  }

  const statusMutation = useMutation({
    mutationFn: (body: Parameters<typeof changeOrderStatus>[1]) => changeOrderStatus(id, body),
    onSuccess: onDone,
    onError: (e) => setError(errorMessage(e)),
  })

  const courierMutation = useMutation({
    mutationFn: (courierId: string) => assignCourier(id, courierId),
    onSuccess: onDone,
    onError: (e) => setError(errorMessage(e)),
  })

  const cancelMutation = useMutation({
    mutationFn: () => cancelOrder(id),
    onSuccess: onDone,
    onError: (e) => setError(errorMessage(e)),
  })

  const busy = statusMutation.isPending || courierMutation.isPending || cancelMutation.isPending

  const back = (
    <button
      type="button"
      onClick={() => navigate('/orders')}
      className="mb-4 flex items-center gap-2 text-sm font-semibold text-stone-500 hover:text-stone-800"
    >
      <ArrowLeft className="size-4" />
      К заказам
    </button>
  )

  if (isLoading) {
    return (
      <div className="mx-auto max-w-6xl">
        {back}
        <div className="flex justify-center py-24">
          <Loader2 className="size-8 animate-spin text-primary" />
        </div>
      </div>
    )
  }

  if (loadError || !order) {
    return (
      <div className="mx-auto max-w-6xl">
        {back}
        <div className="rounded-2xl border bg-white py-20 text-center">
          <p className="font-semibold text-red-600">{errorMessage(loadError)}</p>
        </div>
      </div>
    )
  }

  const isFinal = order.status === 'Delivered' || order.status === 'Cancelled'
  const next = nextStatus[order.status]
  const canCancel = order.status === 'New' || order.status === 'Accepted'
  const needsCourier = next === 'OnTheWay' && !order.courierId
  const courierList = couriers.data?.items ?? []

  const onNext = () => {
    if (!next) return

    if (next === 'Delivered' && order.paymentType === 'Now') {
      setAmount(String(order.totalAmount))
      setPayOpen(true)
      return
    }

    statusMutation.mutate({ status: next })
  }

  const onConfirmPayment = () => {
    const value = Number(amount.replace(',', '.'))

    if (!Number.isFinite(value) || value <= 0) {
      setError('Введите сумму оплаты.')
      return
    }

    statusMutation.mutate({ status: 'Delivered', paymentAmount: value, paymentMethod: method })
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div>
        {back}
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-3xl font-extrabold text-stone-900">
            Заказ #{orderNumber(order.id)}
          </h1>
          <StatusBadge status={order.status} />
        </div>
        <p className="mt-1 text-stone-500">
          Создан {formatDateTime(order.createdAt)} · {sourceLabels[order.source] ?? order.source}
        </p>
      </div>

      {order.status === 'Cancelled' ? (
        <div className="flex items-center gap-3 rounded-2xl border border-red-200 bg-red-50 p-5 text-red-700">
          <Ban className="size-6 shrink-0" />
          <div>
            <p className="font-extrabold">Заказ отменён</p>
            <p className="text-sm">Дальнейшие действия недоступны.</p>
          </div>
        </div>
      ) : (
        <Stepper order={order} />
      )}

      <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
        <div className="space-y-6">
          <Panel title="Состав заказа">
            <div className="overflow-hidden rounded-xl border">
              <table className="w-full text-sm">
                <thead className="bg-stone-50 text-left text-xs uppercase text-stone-500">
                  <tr>
                    <th className="px-4 py-2.5 font-semibold">Товар</th>
                    <th className="px-4 py-2.5 text-right font-semibold">Кол-во</th>
                    <th className="px-4 py-2.5 text-right font-semibold">Цена</th>
                    <th className="px-4 py-2.5 text-right font-semibold">Сумма</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {order.items.map((item) => (
                    <tr key={item.productId}>
                      <td className="px-4 py-3 font-semibold text-stone-900">{item.productName}</td>
                      <td className="px-4 py-3 text-right">{item.quantity}</td>
                      <td className="px-4 py-3 text-right text-stone-600">
                        {money(item.unitPrice)}
                      </td>
                      <td className="px-4 py-3 text-right font-bold">{money(item.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="border-t bg-stone-50">
                    <td colSpan={3} className="px-4 py-3.5 text-right font-bold">
                      Итого
                    </td>
                    <td className="px-4 py-3.5 text-right text-lg font-extrabold">
                      {money(order.totalAmount)}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </Panel>

          <Panel title="История">
            {order.history.length === 0 ? (
              <p className="text-sm text-stone-500">Изменений пока нет</p>
            ) : (
              <ol className="space-y-4 border-l-2 border-stone-200 pl-5">
                {order.history.map((h, index) => (
                  <li key={`${h.status}-${index}`} className="relative">
                    <span className="absolute -left-[1.7rem] top-1 size-3 rounded-full border-2 border-white bg-primary ring-2 ring-primary/30" />
                    <p className="text-sm font-bold text-stone-900">{statusLabels[h.status]}</p>
                    <p className="text-xs text-stone-500">
                      {formatDateTime(h.changedAt)}
                      {h.changedByName ? ` · ${h.changedByName}` : ''}
                    </p>
                  </li>
                ))}
              </ol>
            )}
          </Panel>
        </div>

        <div className="space-y-6">
          <Panel title={isFinal ? 'Итог' : 'Следующий шаг'}>
            {order.status === 'Delivered' && (
              <p className="flex items-center gap-2 font-semibold text-emerald-700">
                <PackageCheck className="size-5" />
                Заказ доставлен
              </p>
            )}
            {order.status === 'Cancelled' && (
              <p className="flex items-center gap-2 font-semibold text-red-700">
                <Ban className="size-5" />
                Заказ отменён
              </p>
            )}

            {!isFinal && next && !payOpen && (
              <div className="space-y-4">
                <p className="text-sm text-stone-600">{nextHint[order.status]}</p>

                <Button
                  className="h-12 w-full rounded-xl text-base font-bold"
                  onClick={onNext}
                  disabled={busy || needsCourier}
                >
                  {statusMutation.isPending && <Loader2 className="size-5 animate-spin" />}
                  {nextLabel[order.status]}
                </Button>

                {needsCourier && (
                  <div className="flex gap-3 rounded-xl bg-amber-50 p-3.5 text-sm text-amber-800">
                    <TriangleAlert className="mt-0.5 size-4 shrink-0" />
                    <p>
                      Сначала назначьте курьера: выберите его в блоке «Курьер» ниже, он будет
                      назначен сразу.
                    </p>
                  </div>
                )}

                {order.paymentType === 'Debt' && order.status === 'OnTheWay' && (
                  <p className="text-xs text-stone-500">
                    Заказ в долг: после доставки сумма добавится к долгу магазина.
                  </p>
                )}

                {canCancel && !confirmCancel && (
                  <Button
                    variant="outline"
                    className="h-10 w-full rounded-xl text-red-600 hover:text-red-700"
                    onClick={() => setConfirmCancel(true)}
                    disabled={busy}
                  >
                    Отменить заказ
                  </Button>
                )}

                {canCancel && confirmCancel && (
                  <div className="space-y-2 rounded-xl border border-red-200 bg-red-50 p-3.5">
                    <p className="text-sm font-semibold text-red-700">Точно отменить заказ?</p>
                    <div className="flex gap-2">
                      <Button
                        className="h-10 flex-1 bg-red-600 text-white hover:bg-red-700"
                        onClick={() => cancelMutation.mutate()}
                        disabled={busy}
                      >
                        {cancelMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                        Да, отменить
                      </Button>
                      <Button
                        variant="ghost"
                        className="h-10"
                        onClick={() => setConfirmCancel(false)}
                        disabled={busy}
                      >
                        Нет
                      </Button>
                    </div>
                  </div>
                )}
              </div>
            )}

            {payOpen && (
              <div className="space-y-3">
                <p className="text-sm font-bold text-stone-800">Оплата при доставке</p>
                <Input
                  type="number"
                  min={0}
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="Сумма"
                  className="h-11 rounded-xl"
                />
                <NativeSelect
                  className="h-11"
                  value={method}
                  onChange={(e) => setMethod(e.target.value as PaymentMethod)}
                >
                  <option value="Cash">Наличные</option>
                  <option value="Transfer">Перевод</option>
                </NativeSelect>
                <div className="flex gap-2">
                  <Button
                    className="h-11 flex-1 rounded-xl font-bold"
                    onClick={onConfirmPayment}
                    disabled={busy}
                  >
                    {statusMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                    Подтвердить доставку
                  </Button>
                  <Button
                    variant="ghost"
                    className="h-11"
                    onClick={() => setPayOpen(false)}
                    disabled={busy}
                  >
                    Отмена
                  </Button>
                </div>
              </div>
            )}

            {error && (
              <div className="mt-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {error}
              </div>
            )}
          </Panel>

          <Panel
            title="Курьер"
            action={
              order.courierId ? (
                <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-bold text-emerald-700">
                  Назначен
                </span>
              ) : (
                <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-bold text-amber-700">
                  Не назначен
                </span>
              )
            }
          >
            {order.courierName && (
              <div className="mb-4 flex items-center gap-3 rounded-xl bg-stone-50 p-3">
                <div className="flex size-10 items-center justify-center rounded-full bg-primary/15 text-primary">
                  <User className="size-5" />
                </div>
                <p className="font-bold text-stone-900">{order.courierName}</p>
              </div>
            )}

            {!isFinal && (
              <>
                <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-stone-400">
                  {order.courierId ? 'Заменить курьера' : 'Нажмите на курьера, чтобы назначить'}
                </p>

                {couriers.isLoading ? (
                  <Loader2 className="size-5 animate-spin text-primary" />
                ) : courierList.length === 0 ? (
                  <p className="text-sm text-stone-500">
                    Нет активных курьеров. Добавьте их в разделе «Пользователи».
                  </p>
                ) : (
                  <ul className="space-y-2">
                    {courierList.map((c) => {
                      const isCurrent = c.id === order.courierId
                      const isAssigning =
                        courierMutation.isPending && courierMutation.variables === c.id

                      return (
                        <li key={c.id}>
                          <button
                            type="button"
                            disabled={busy || isCurrent}
                            onClick={() => courierMutation.mutate(c.id)}
                            className={cn(
                              'flex w-full items-center gap-3 rounded-xl border p-3 text-left transition-all',
                              isCurrent
                                ? 'border-primary bg-amber-50/60'
                                : 'border-stone-200 hover:border-primary/50 hover:bg-amber-50/40',
                              busy && !isCurrent && 'opacity-60',
                            )}
                          >
                            <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-stone-100 font-extrabold text-stone-600">
                              {courierName(c).charAt(0).toUpperCase()}
                            </div>
                            <div className="min-w-0 flex-1">
                              <p className="truncate font-bold text-stone-900">{courierName(c)}</p>
                              <p className="truncate text-xs text-stone-500">{c.phone}</p>
                            </div>
                            {isAssigning ? (
                              <Loader2 className="size-5 animate-spin text-primary" />
                            ) : isCurrent ? (
                              <Check className="size-5 text-primary" />
                            ) : (
                              <span className="text-xs font-bold text-primary">Назначить</span>
                            )}
                          </button>
                        </li>
                      )
                    })}
                  </ul>
                )}
              </>
            )}
          </Panel>

          <Panel title="Магазин">
            <p className="text-lg font-extrabold text-stone-900">{order.shopName}</p>
            <p className="mt-1 text-sm text-stone-500">{order.shopAddress}</p>
            <a
              href={`tel:${order.shopPhone}`}
              className="mt-3 inline-flex items-center gap-2 rounded-xl bg-amber-50 px-3.5 py-2 text-sm font-bold text-primary hover:bg-amber-100"
            >
              <Phone className="size-4" />
              {order.shopPhone}
            </a>
          </Panel>

          <Panel title="Доставка и оплата">
            <dl className="divide-y divide-stone-100">
              <Row label="Дата доставки">{formatDate(order.deliveryDate)}</Row>
              <Row label="Оплата">{paymentLabels[order.paymentType]}</Row>
              <Row label="Сумма">{money(order.totalAmount)}</Row>
              {order.note && <Row label="Примечание">{order.note}</Row>}
            </dl>
          </Panel>
        </div>
      </div>
    </div>
  )
}