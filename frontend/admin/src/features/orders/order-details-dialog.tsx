import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2, Phone } from 'lucide-react'
import { useState, type ReactNode } from 'react'

import { Button } from '@/components/ui/button'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { NativeSelect } from '@/components/ui/native-select'
import { ApiError } from '@/lib/api'
import { formatDate, formatDateTime, money } from '@/lib/format'

import { assignCourier, cancelOrder, changeOrderStatus } from './api'
import { useCouriers, useOrder } from './hooks'
import { StatusBadge } from './status-badge'
import {
    orderNumber,
    paymentLabels,
    sourceLabels,
    statusLabels,
    type OrderStatus,
    type PaymentMethod,
} from './types'

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

function Field({ label, children }: { label: string; children: ReactNode }) {
    return (
        <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-stone-400">{label}</p>
            <div className="mt-1 text-sm font-semibold text-stone-800">{children}</div>
        </div>
    )
}

function errorMessage(e: unknown): string {
    return e instanceof ApiError ? e.message : 'Не удалось выполнить действие.'
}

function OrderBody({ id }: { id: string }) {
    const queryClient = useQueryClient()
    const { data: order, isLoading, error: loadError } = useOrder(id)
    const couriers = useCouriers()

    const [courierChoice, setCourierChoice] = useState<string | null>(null)
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

    if (isLoading) {
        return (
            <div className="flex justify-center py-16">
                <Loader2 className="size-8 animate-spin text-primary" />
            </div>
        )
    }

    if (loadError || !order) {
        return (
            <p className="py-10 text-center text-sm text-red-600">{errorMessage(loadError)}</p>
        )
    }

    const isFinal = order.status === 'Delivered' || order.status === 'Cancelled'
    const next = nextStatus[order.status]
    const canCancel = order.status === 'New' || order.status === 'Accepted'
    const needsCourier = next === 'OnTheWay' && !order.courierId
    const selectedCourier = courierChoice ?? order.courierId ?? ''
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
        <div className="space-y-6">
            <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Магазин">
                    <p>{order.shopName}</p>
                    <p className="font-normal text-stone-500">{order.shopAddress}</p>
                    <a
                        href={`tel:${order.shopPhone}`}
                        className="mt-1 inline-flex items-center gap-1 text-primary hover:underline"
                    >
                        <Phone className="size-3.5" />
                        {order.shopPhone}
                    </a>
                </Field>
                <Field label="Доставка">{formatDate(order.deliveryDate)}</Field>
                <Field label="Оплата">{paymentLabels[order.paymentType]}</Field>
                <Field label="Источник">{sourceLabels[order.source] ?? order.source}</Field>
                <Field label="Создан">{formatDateTime(order.createdAt)}</Field>
                <Field label="Курьер">{order.courierName ?? 'Не назначен'}</Field>
                {order.note && (
                    <div className="sm:col-span-2">
                        <Field label="Примечание">{order.note}</Field>
                    </div>
                )}
            </div>

            <div className="overflow-hidden rounded-xl border">
                <table className="w-full text-sm">
                    <thead className="bg-stone-50 text-left text-xs uppercase text-stone-500">
                        <tr>
                            <th className="px-4 py-2 font-semibold">Товар</th>
                            <th className="px-4 py-2 text-right font-semibold">Кол-во</th>
                            <th className="px-4 py-2 text-right font-semibold">Цена</th>
                            <th className="px-4 py-2 text-right font-semibold">Сумма</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y">
                        {order.items.map((item) => (
                            <tr key={item.productId}>
                                <td className="px-4 py-2 font-medium">{item.productName}</td>
                                <td className="px-4 py-2 text-right">{item.quantity}</td>
                                <td className="px-4 py-2 text-right">{money(item.unitPrice)}</td>
                                <td className="px-4 py-2 text-right font-semibold">{money(item.lineTotal)}</td>
                            </tr>
                        ))}
                    </tbody>
                    <tfoot>
                        <tr className="border-t bg-stone-50">
                            <td colSpan={3} className="px-4 py-3 text-right font-bold">
                                Итого
                            </td>
                            <td className="px-4 py-3 text-right text-base font-extrabold">
                                {money(order.totalAmount)}
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>

            {!isFinal && (
                <div className="space-y-4 rounded-xl border bg-stone-50 p-4">
                    <div>
                        <p className="mb-2 text-sm font-bold text-stone-800">Курьер</p>
                        <div className="flex gap-2">
                            <NativeSelect
                                value={selectedCourier}
                                onChange={(e) => setCourierChoice(e.target.value)}
                                disabled={busy}
                            >
                                <option value="">Выберите курьера</option>
                                {courierList.map((c) => (
                                    <option key={c.id} value={c.id}>
                                        {c.fullName || `${c.firstName ?? ''} ${c.lastName ?? ''}`.trim()}                  </option>
                                ))}
                            </NativeSelect>
                            <Button
                                variant="outline"
                                className="h-10"
                                disabled={busy || !selectedCourier || selectedCourier === order.courierId}
                                onClick={() => courierMutation.mutate(selectedCourier)}
                            >
                                {courierMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                                Назначить
                            </Button>
                        </div>
                    </div>

                    {payOpen && (
                        <div className="space-y-3 rounded-lg border bg-white p-4">
                            <p className="text-sm font-bold text-stone-800">Оплата при доставке</p>
                            <div className="grid gap-3 sm:grid-cols-2">
                                <Input
                                    type="number"
                                    min={0}
                                    value={amount}
                                    onChange={(e) => setAmount(e.target.value)}
                                    className="h-10"
                                    placeholder="Сумма"
                                />
                                <NativeSelect
                                    value={method}
                                    onChange={(e) => setMethod(e.target.value as PaymentMethod)}
                                >
                                    <option value="Cash">Наличные</option>
                                    <option value="Transfer">Перевод</option>
                                </NativeSelect>
                            </div>
                            <div className="flex gap-2">
                                <Button onClick={onConfirmPayment} disabled={busy}>
                                    {statusMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                                    Подтвердить доставку
                                </Button>
                                <Button variant="ghost" onClick={() => setPayOpen(false)} disabled={busy}>
                                    Отмена
                                </Button>
                            </div>
                        </div>
                    )}

                    {!payOpen && (
                        <div className="flex flex-wrap items-center gap-2">
                            {next && (
                                <Button className="h-10" onClick={onNext} disabled={busy || needsCourier}>
                                    {statusMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                                    {nextLabel[order.status]}
                                </Button>
                            )}

                            {canCancel && !confirmCancel && (
                                <Button
                                    variant="outline"
                                    className="h-10 text-red-600 hover:text-red-700"
                                    onClick={() => setConfirmCancel(true)}
                                    disabled={busy}
                                >
                                    Отменить заказ
                                </Button>
                            )}

                            {canCancel && confirmCancel && (
                                <>
                                    <Button
                                        className="h-10 bg-red-600 text-white hover:bg-red-700"
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
                                </>
                            )}
                        </div>
                    )}

                    {needsCourier && !payOpen && (
                        <p className="text-sm text-amber-700">
                            Чтобы отправить заказ в путь, сначала назначьте курьера.
                        </p>
                    )}
                </div>
            )}

            {error && (
                <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {error}
                </div>
            )}

            {order.history.length > 0 && (
                <div>
                    <p className="mb-3 text-sm font-bold text-stone-800">История</p>
                    <ol className="space-y-3 border-l-2 border-stone-200 pl-4">
                        {order.history.map((h, index) => (
                            <li key={`${h.status}-${index}`} className="relative">
                                <span className="absolute -left-[1.45rem] top-1.5 size-2.5 rounded-full bg-primary" />
                                <p className="text-sm font-semibold text-stone-800">{statusLabels[h.status]}</p>
                                <p className="text-xs text-stone-500">
                                    {formatDateTime(h.changedAt)}
                                    {h.changedByName ? ` · ${h.changedByName}` : ''}
                                </p>
                            </li>
                        ))}
                    </ol>
                </div>
            )}
        </div>
    )
}

export function OrderDetailsDialog({
    orderId,
    onClose,
}: {
    orderId: string | null
    onClose: () => void
}) {
    return (
        <Dialog open={orderId !== null} onOpenChange={(open) => !open && onClose()}>
            <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
                {orderId && (
                    <>
                        <DialogHeader>
                            <DialogTitle className="flex items-center gap-3 text-xl font-extrabold">
                                Заказ №{orderNumber(orderId)}
                                <OrderStatusInTitle id={orderId} />
                            </DialogTitle>
                            <DialogDescription className="sr-only">Детали заказа</DialogDescription>
                        </DialogHeader>
                        <OrderBody key={orderId} id={orderId} />
                    </>
                )}
            </DialogContent>
        </Dialog>
    )
}

function OrderStatusInTitle({ id }: { id: string }) {
    const { data } = useOrder(id)

    return data ? <StatusBadge status={data.status} /> : null
}