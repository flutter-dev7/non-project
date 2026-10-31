import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CircleCheck, Loader2, Phone, Store } from 'lucide-react'
import { useState, type ReactNode } from 'react'

import { Button } from '@/components/ui/button'

import { Input } from '@/components/ui/input'
import { NativeSelect } from '@/components/ui/native-select'
import type { PaymentMethod } from '@/features/orders/types'
import { createPayment, fetchPayments, reversePayment } from '@/features/payments/api'
import { ApiError } from '@/lib/api'
import { formatDate, formatDateTime, money, toApiPhone } from '@/lib/format'
import { cn } from '@/lib/utils'

import { fetchShop, fetchShopBalance, setDebtLimit, setShopOwner, updateShop } from './api'
import { AppDialog } from '@/components/app-dialog'

const methodLabels: Record<PaymentMethod, string> = {
    Cash: 'Наличные',
    Transfer: 'Перевод',
}

function errorMessage(e: unknown): string {
    return e instanceof ApiError ? e.message : 'Не удалось выполнить действие.'
}

function Section({ title, children }: { title: string; children: ReactNode }) {
    return (
        <section className="rounded-xl border bg-white p-4">
            <h3 className="mb-3 text-sm font-extrabold uppercase tracking-wide text-stone-500">
                {title}
            </h3>
            {children}
        </section>
    )
}

function Tile({ label, value, tone }: { label: string; value: string; tone?: string }) {
    return (
        <div className="rounded-xl bg-stone-50 p-3">
            <p className="text-xs font-semibold text-stone-500">{label}</p>
            <p className={cn('mt-1 text-lg font-extrabold text-stone-900', tone)}>{value}</p>
        </div>
    )
}

function ShopBody({ id }: { id: string }) {
    const queryClient = useQueryClient()

    const shopQuery = useQuery({ queryKey: ['shops', 'detail', id], queryFn: () => fetchShop(id) })
    const balanceQuery = useQuery({
        queryKey: ['shops', 'balance', id],
        queryFn: () => fetchShopBalance(id),
    })
    const paymentsQuery = useQuery({
        queryKey: ['payments', id],
        queryFn: () => fetchPayments(id),
    })

    const [payAmount, setPayAmount] = useState('')
    const [payMethod, setPayMethod] = useState<PaymentMethod>('Cash')
    const [payOrder, setPayOrder] = useState('')
    const [limitInput, setLimitInput] = useState<string | null>(null)
    const [ownerInput, setOwnerInput] = useState('')
    const [reversing, setReversing] = useState<{ id: string; reason: string } | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [notice, setNotice] = useState<string | null>(null)

    const [edit, setEdit] = useState<{
        name: string
        address: string
        phone: string
        contact: string
        isActive: boolean
    } | null>(null)

    const done = (message: string) => {
        setError(null)
        setNotice(message)
        queryClient.invalidateQueries({ queryKey: ['shops'] })
        queryClient.invalidateQueries({ queryKey: ['debts'] })
        queryClient.invalidateQueries({ queryKey: ['payments'] })
    }

    const fail = (e: unknown) => {
        setNotice(null)
        setError(errorMessage(e))
    }

    const payMutation = useMutation({
        mutationFn: () =>
            createPayment({
                shopId: id,
                orderId: payOrder || undefined,
                amount: Number(payAmount.replace(',', '.')),
                method: payMethod,
            }),
        onSuccess: () => {
            setPayAmount('')
            setPayOrder('')
            done('Оплата принята')
        },
        onError: fail,
    })

    const limitMutation = useMutation({
        mutationFn: (value: number | null) => setDebtLimit(id, value),
        onSuccess: () => {
            setLimitInput(null)
            done('Лимит долга сохранён')
        },
        onError: fail,
    })

    const ownerMutation = useMutation({
        mutationFn: () => setShopOwner(id, toApiPhone(ownerInput)),
        onSuccess: () => {
            setOwnerInput('')
            done('Владелец привязан')
        },
        onError: fail,
    })

    const updateMutation = useMutation({
        mutationFn: () => {
            const v = edit!

            return updateShop(id, {
                name: v.name.trim(),
                address: v.address.trim(),
                phone: toApiPhone(v.phone),
                contactName: v.contact.trim() || undefined,
                isActive: v.isActive,
            })
        },
        onSuccess: () => {
            setEdit(null)
            done('Данные магазина сохранены')
        },
        onError: fail,
    })

    const reverseMutation = useMutation({
        mutationFn: (v: { id: string; reason: string }) => reversePayment(v.id, v.reason),
        onSuccess: () => {
            setReversing(null)
            done('Оплата отменена')
        },
        onError: fail,
    })

    const shop = shopQuery.data
    const balance = balanceQuery.data

    if (shopQuery.isLoading || balanceQuery.isLoading) {
        return (
            <div className="flex justify-center py-16">
                <Loader2 className="size-8 animate-spin text-primary" />
            </div>
        )
    }

    if (!shop || !balance) {
        return (
            <p className="py-10 text-center text-sm text-red-600">
                {errorMessage(shopQuery.error ?? balanceQuery.error)}
            </p>
        )
    }

    const limitValue = limitInput ?? (shop.debtLimit !== null ? String(shop.debtLimit) : '')
    const limitChanged = limitInput !== null && limitInput !== (shop.debtLimit?.toString() ?? '')
    const payNumber = Number(payAmount.replace(',', '.'))
    const payValid = Number.isFinite(payNumber) && payNumber > 0
    const overLimit = balance.debtLimit !== null && balance.debt > balance.debtLimit
    const payments = paymentsQuery.data?.items ?? []

    const form = edit ?? {
        name: shop.name,
        address: shop.address,
        phone: shop.phone,
        contact: shop.contactName ?? '',
        isActive: shop.isActive,
    }
    const patchForm = (patch: Partial<typeof form>) => setEdit({ ...form, ...patch })
    const canSaveShop =
        edit !== null && edit.name.trim() !== '' && edit.address.trim() !== '' && edit.phone.trim() !== ''

    const saveLimit = () => {
        const text = limitValue.trim()

        if (text === '') return limitMutation.mutate(null)

        const value = Number(text.replace(',', '.'))

        if (!Number.isFinite(value) || value < 0) return setError('Введите корректный лимит.')

        limitMutation.mutate(value)
    }

    return (
        <div className="space-y-5">
            <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                    <p className="text-stone-600">{shop.address}</p>
                    <a
                        href={`tel:${shop.phone}`}
                        className="mt-1 inline-flex items-center gap-1.5 font-semibold text-primary hover:underline"
                    >
                        <Phone className="size-4" />
                        {shop.phone}
                    </a>
                    {shop.contactName && (
                        <p className="mt-1 text-sm text-stone-500">Контакт: {shop.contactName}</p>
                    )}
                </div>
                <span
                    className={cn(
                        'rounded-full px-3 py-1 text-xs font-bold',
                        shop.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-stone-100 text-stone-500',
                    )}
                >
                    {shop.isActive ? 'Активен' : 'Отключён'}
                </span>
            </div>

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                <Tile
                    label="Долг"
                    value={money(balance.debt)}
                    tone={balance.debt > 0 ? 'text-red-600' : undefined}
                />
                <Tile label="Доставлено" value={money(balance.deliveredTotal)} />
                <Tile label="Оплачено" value={money(balance.paidTotal)} />
                <Tile
                    label="Лимит долга"
                    value={balance.debtLimit !== null ? money(balance.debtLimit) : 'Нет'}
                    tone={overLimit ? 'text-red-600' : undefined}
                />
            </div>

            {overLimit && (
                <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
                    Долг превышает лимит
                </p>
            )}

            <Section title="Принять оплату">
                <div className="grid gap-3 sm:grid-cols-[1fr_1fr]">
                    <Input
                        type="number"
                        min={0}
                        value={payAmount}
                        onChange={(e) => setPayAmount(e.target.value)}
                        placeholder={balance.debt > 0 ? `Сумма (долг ${money(balance.debt)})` : 'Сумма'}
                        className="h-10"
                    />
                    <NativeSelect
                        value={payMethod}
                        onChange={(e) => setPayMethod(e.target.value as PaymentMethod)}
                    >
                        <option value="Cash">Наличные</option>
                        <option value="Transfer">Перевод</option>
                    </NativeSelect>
                </div>

                {balance.unpaidOrders.length > 0 && (
                    <NativeSelect
                        className="mt-3"
                        value={payOrder}
                        onChange={(e) => setPayOrder(e.target.value)}
                    >
                        <option value="">Без привязки к заказу</option>
                        {balance.unpaidOrders.map((o) => (
                            <option key={o.orderId} value={o.orderId}>
                                Заказ от {formatDate(o.deliveryDate)} — осталось {money(o.remainingAmount)}
                            </option>
                        ))}
                    </NativeSelect>
                )}

                <Button
                    className="mt-3 h-10 font-bold"
                    disabled={!payValid || payMutation.isPending}
                    onClick={() => payMutation.mutate()}
                >
                    {payMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                    Принять оплату
                </Button>
            </Section>

            <Section title="Данные магазина">
                <div className="grid gap-3 sm:grid-cols-2">
                    <Input
                        value={form.name}
                        onChange={(e) => patchForm({ name: e.target.value })}
                        placeholder="Название"
                        className="h-10"
                    />
                    <Input
                        value={form.phone}
                        onChange={(e) => patchForm({ phone: e.target.value })}
                        placeholder="Телефон"
                        className="h-10"
                    />
                    <Input
                        value={form.address}
                        onChange={(e) => patchForm({ address: e.target.value })}
                        placeholder="Адрес"
                        className="h-10"
                    />
                    <Input
                        value={form.contact}
                        onChange={(e) => patchForm({ contact: e.target.value })}
                        placeholder="Контактное лицо"
                        className="h-10"
                    />
                </div>

                <div className="mt-3 flex flex-wrap items-center gap-3">
                    <div className="inline-flex gap-1 rounded-xl bg-stone-100 p-1">
                        {[true, false].map((value) => (
                            <button
                                key={String(value)}
                                type="button"
                                onClick={() => patchForm({ isActive: value })}
                                className={cn(
                                    'rounded-lg px-4 py-1.5 text-sm font-bold transition-all',
                                    form.isActive === value
                                        ? 'bg-white text-stone-900 shadow-sm'
                                        : 'text-stone-500 hover:text-stone-800',
                                )}
                            >
                                {value ? 'Активен' : 'Отключён'}
                            </button>
                        ))}
                    </div>

                    <Button
                        className="h-10 font-bold"
                        disabled={!canSaveShop || updateMutation.isPending}
                        onClick={() => updateMutation.mutate()}
                    >
                        {updateMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                        Сохранить изменения
                    </Button>
                    {edit && (
                        <Button variant="ghost" className="h-10" onClick={() => setEdit(null)}>
                            Сбросить
                        </Button>
                    )}
                </div>
            </Section>

            <div className="grid gap-5 sm:grid-cols-2">
                <Section title="Лимит долга">
                    <div className="flex gap-2">
                        <Input
                            type="number"
                            min={0}
                            value={limitValue}
                            onChange={(e) => setLimitInput(e.target.value)}
                            placeholder="Без лимита"
                            className="h-10"
                        />
                        <Button
                            variant="outline"
                            className="h-10"
                            disabled={!limitChanged || limitMutation.isPending}
                            onClick={saveLimit}
                        >
                            {limitMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                            Сохранить
                        </Button>
                    </div>
                    <p className="mt-2 text-xs text-stone-500">Пустое поле — без ограничения.</p>
                </Section>

                <Section title="Владелец магазина">
                    <p className="mb-3 text-sm font-semibold text-stone-800">
                        {shop.ownerName ? `${shop.ownerName} · ${shop.ownerPhone ?? ''}` : 'Не привязан'}
                    </p>
                    <div className="flex gap-2">
                        <Input
                            value={ownerInput}
                            onChange={(e) => setOwnerInput(e.target.value)}
                            placeholder="Телефон пользователя"
                            className="h-10"
                        />
                        <Button
                            variant="outline"
                            className="h-10"
                            disabled={ownerInput.trim() === '' || ownerMutation.isPending}
                            onClick={() => ownerMutation.mutate()}
                        >
                            {ownerMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                            Привязать
                        </Button>
                    </div>
                </Section>
            </div>

            {balance.unpaidOrders.length > 0 && (
                <Section title="Неоплаченные заказы">
                    <ul className="divide-y divide-stone-100">
                        {balance.unpaidOrders.map((o) => (
                            <li key={o.orderId} className="flex items-center justify-between gap-3 py-2.5 text-sm">
                                <span className="font-semibold text-stone-800">{formatDate(o.deliveryDate)}</span>
                                <span className="text-stone-500">
                                    оплачено {money(o.paidAmount)} из {money(o.totalAmount)}
                                </span>
                                <span className="font-extrabold text-red-600">{money(o.remainingAmount)}</span>
                            </li>
                        ))}
                    </ul>
                </Section>
            )}

            <Section title="История оплат">
                {paymentsQuery.isLoading ? (
                    <Loader2 className="size-5 animate-spin text-primary" />
                ) : payments.length === 0 ? (
                    <p className="text-sm text-stone-500">Оплат пока нет</p>
                ) : (
                    <ul className="divide-y divide-stone-100">
                        {payments.map((p) => (
                            <li key={p.id} className="py-3">
                                <div className="flex flex-wrap items-center justify-between gap-3">
                                    <div>
                                        <p
                                            className={cn(
                                                'font-extrabold',
                                                p.isReversal ? 'text-red-600' : 'text-stone-900',
                                            )}
                                        >
                                            {p.isReversal ? '−' : ''}
                                            {money(p.amount)}
                                            {p.isReversal && (
                                                <span className="ml-2 rounded-md bg-red-50 px-1.5 py-0.5 text-xs font-bold">
                                                    Отмена
                                                </span>
                                            )}
                                            {p.isReversed && (
                                                <span className="ml-2 rounded-md bg-stone-100 px-1.5 py-0.5 text-xs font-bold text-stone-500">
                                                    Отменён
                                                </span>
                                            )}
                                        </p>
                                        <p className="text-xs text-stone-500">
                                            {formatDateTime(p.createdAt)} · {methodLabels[p.method]}
                                            {p.receivedByName ? ` · ${p.receivedByName}` : ''}
                                        </p>
                                        {p.reversalReason && (
                                            <p className="text-xs text-stone-500">Причина: {p.reversalReason}</p>
                                        )}
                                    </div>

                                    {!p.isReversal && !p.isReversed && reversing?.id !== p.id && (
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            className="text-red-600 hover:text-red-700"
                                            onClick={() => setReversing({ id: p.id, reason: '' })}
                                        >
                                            Отменить
                                        </Button>
                                    )}
                                </div>

                                {reversing?.id === p.id && (
                                    <div className="mt-3 flex gap-2">
                                        <Input
                                            autoFocus
                                            value={reversing.reason}
                                            onChange={(e) => setReversing({ id: p.id, reason: e.target.value })}
                                            placeholder="Причина отмены"
                                            className="h-10"
                                        />
                                        <Button
                                            className="h-10 bg-red-600 text-white hover:bg-red-700"
                                            disabled={reversing.reason.trim() === '' || reverseMutation.isPending}
                                            onClick={() =>
                                                reverseMutation.mutate({ id: p.id, reason: reversing.reason.trim() })
                                            }
                                        >
                                            {reverseMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                                            Отменить оплату
                                        </Button>
                                        <Button variant="ghost" className="h-10" onClick={() => setReversing(null)}>
                                            Нет
                                        </Button>
                                    </div>
                                )}
                            </li>
                        ))}
                    </ul>
                )}
            </Section>

            {notice && (
                <div className="flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">
                    <CircleCheck className="size-4" />
                    {notice}
                </div>
            )}

            {error && (
                <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {error}
                </div>
            )}
        </div>
    )
}

export function ShopDialog({
  shopId,
  shopName,
  onClose,
}: {
  shopId: string | null
  shopName: string
  onClose: () => void
}) {
  return (
    <AppDialog
      open={shopId !== null}
      onClose={onClose}
      icon={Store}
      title={shopName}
      subtitle="Карточка магазина"
      wide
    >
      {shopId && <ShopBody key={shopId} id={shopId} />}
    </AppDialog>
  )
}