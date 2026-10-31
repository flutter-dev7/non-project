import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2, Minus, Plus } from 'lucide-react'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/lib/api'
import { dushanbeDate, money } from '@/lib/format'
import { cn } from '@/lib/utils'

import { fetchProducts } from '@/features/products/api'
import type { Shop } from '@/features/shops/api'

import { createOrder } from './api'
import { ShopPicker } from './shop-picker'
import type { PaymentType } from './types'

function SectionTitle({ n, children }: { n: number; children: string }) {
  return (
    <div className="mb-4 flex items-center gap-3">
      <span className="flex size-7 items-center justify-center rounded-full bg-primary text-sm font-extrabold text-primary-foreground">
        {n}
      </span>
      <h2 className="text-lg font-extrabold text-stone-900">{children}</h2>
    </div>
  )
}

export function NewOrderPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const orderId = useRef(crypto.randomUUID())

  const [shop, setShop] = useState<Shop | null>(null)
  const [quantities, setQuantities] = useState<Record<string, number>>({})
  const [date, setDate] = useState(dushanbeDate(1))
  const [payment, setPayment] = useState<PaymentType>('Now')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)

  const products = useQuery({ queryKey: ['products', 'active'], queryFn: fetchProducts })

  const list = (products.data ?? []).filter((p) => p.isActive)
  const lines = list.filter((p) => (quantities[p.id] ?? 0) > 0)
  const total = lines.reduce((sum, p) => sum + p.price * (quantities[p.id] ?? 0), 0)
  const count = lines.reduce((sum, p) => sum + (quantities[p.id] ?? 0), 0)

  const setQty = (id: string, value: number) => {
    setQuantities((prev) => ({ ...prev, [id]: Math.max(0, Math.min(9999, value)) }))
  }

  const mutation = useMutation({
    mutationFn: () =>
      createOrder({
        id: orderId.current,
        shopId: shop!.id,
        deliveryDate: date,
        paymentType: payment,
        note: note.trim() || undefined,
        items: lines.map((p) => ({ productId: p.id, quantity: quantities[p.id] })),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] })
      navigate('/orders')
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Не удалось создать заказ.'),
  })

  const submit = () => {
    setError(null)

    if (!shop) return setError('Выберите магазин.')
    if (lines.length === 0) return setError('Добавьте хотя бы один товар.')
    if (!date) return setError('Выберите дату доставки.')

    mutation.mutate()
  }

  return (
    <div className="mx-auto max-w-6xl">
      <button
        type="button"
        onClick={() => navigate('/orders')}
        className="mb-4 flex items-center gap-2 text-sm font-semibold text-stone-500 hover:text-stone-800"
      >
        <ArrowLeft className="size-4" />
        К заказам
      </button>

      <h1 className="text-3xl font-extrabold text-stone-900">Новый заказ</h1>
      <p className="mt-1 text-stone-500">Оформление заказа от имени администратора</p>

      <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_360px]">
        <div className="space-y-8">
          <section className="rounded-2xl border bg-white p-6 shadow-sm">
            <SectionTitle n={1}>Магазин</SectionTitle>
            <ShopPicker value={shop} onChange={setShop} />
          </section>

          <section className="rounded-2xl border bg-white p-6 shadow-sm">
            <SectionTitle n={2}>Товары</SectionTitle>

            {products.isLoading && (
              <div className="space-y-3">
                {Array.from({ length: 4 }).map((_, i) => (
                  <Skeleton key={i} className="h-16 w-full rounded-xl" />
                ))}
              </div>
            )}

            {products.error && (
              <p className="text-sm text-red-600">Не удалось загрузить товары.</p>
            )}

            <div className="divide-y divide-stone-100">
              {list.map((product) => {
                const qty = quantities[product.id] ?? 0

                return (
                  <div
                    key={product.id}
                    className={cn(
                      'flex items-center gap-4 py-3 transition-colors',
                      qty > 0 && 'bg-amber-50/40',
                    )}
                  >
                    <div className="min-w-0 flex-1 px-1">
                      <p className="truncate font-bold text-stone-900">{product.name}</p>
                      <p className="text-xs text-stone-500">
                        {product.weightInGrams} г · {money(product.price)}
                      </p>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <Button
                        variant="outline"
                        size="icon"
                        className="size-9 rounded-lg"
                        disabled={qty === 0}
                        onClick={() => setQty(product.id, qty - 1)}
                        aria-label="Меньше"
                      >
                        <Minus className="size-4" />
                      </Button>
                      <Input
                        type="number"
                        min={0}
                        value={qty === 0 ? '' : qty}
                        placeholder="0"
                        onChange={(e) => setQty(product.id, Number(e.target.value) || 0)}
                        className="h-9 w-16 rounded-lg text-center font-bold"
                      />
                      <Button
                        size="icon"
                        className="size-9 rounded-lg"
                        onClick={() => setQty(product.id, qty + 1)}
                        aria-label="Больше"
                      >
                        <Plus className="size-4" />
                      </Button>
                    </div>

                    <p className="w-28 text-right font-extrabold text-stone-900">
                      {qty > 0 ? money(product.price * qty) : ''}
                    </p>
                  </div>
                )
              })}
            </div>
          </section>

          <section className="rounded-2xl border bg-white p-6 shadow-sm">
            <SectionTitle n={3}>Доставка и оплата</SectionTitle>

            <div className="grid gap-6 sm:grid-cols-2">
              <div>
                <p className="mb-2 text-sm font-semibold text-stone-700">Дата доставки</p>
                <Input
                  type="date"
                  value={date}
                  min={dushanbeDate(1)}
                  max={dushanbeDate(30)}
                  onChange={(e) => setDate(e.target.value)}
                  className="h-11 rounded-xl"
                />
              </div>

              <div>
                <p className="mb-2 text-sm font-semibold text-stone-700">Оплата</p>
                <div className="grid grid-cols-2 gap-2 rounded-xl bg-stone-100 p-1">
                  {(['Now', 'Debt'] as const).map((type) => (
                    <button
                      key={type}
                      type="button"
                      onClick={() => setPayment(type)}
                      className={cn(
                        'h-9 rounded-lg text-sm font-bold transition-all',
                        payment === type
                          ? 'bg-white text-stone-900 shadow-sm'
                          : 'text-stone-500 hover:text-stone-800',
                      )}
                    >
                      {type === 'Now' ? 'Сразу' : 'В долг'}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="mt-6">
              <p className="mb-2 text-sm font-semibold text-stone-700">Примечание</p>
              <Input
                value={note}
                onChange={(e) => setNote(e.target.value)}
                maxLength={500}
                placeholder="Необязательно"
                className="h-11 rounded-xl"
              />
            </div>
          </section>
        </div>

        <aside className="lg:sticky lg:top-8 lg:self-start">
          <div className="rounded-2xl border bg-white p-6 shadow-sm">
            <h2 className="text-lg font-extrabold text-stone-900">Итог заказа</h2>

            <dl className="mt-5 space-y-3 text-sm">
              <div className="flex justify-between gap-4">
                <dt className="text-stone-500">Магазин</dt>
                <dd className="text-right font-semibold text-stone-900">{shop?.name ?? '—'}</dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-stone-500">Доставка</dt>
                <dd className="font-semibold text-stone-900">
                  {date ? date.split('-').reverse().join('.') : '—'}
                </dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-stone-500">Оплата</dt>
                <dd className="font-semibold text-stone-900">
                  {payment === 'Now' ? 'Сразу' : 'В долг'}
                </dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-stone-500">Позиций</dt>
                <dd className="font-semibold text-stone-900">
                  {lines.length} ({count} шт.)
                </dd>
              </div>
            </dl>

            {lines.length > 0 && (
              <ul className="mt-5 space-y-1.5 border-t pt-5 text-sm">
                {lines.map((p) => (
                  <li key={p.id} className="flex justify-between gap-3 text-stone-600">
                    <span className="truncate">
                      {p.name} × {quantities[p.id]}
                    </span>
                    <span className="font-semibold text-stone-800">
                      {money(p.price * quantities[p.id])}
                    </span>
                  </li>
                ))}
              </ul>
            )}

            <div className="mt-5 flex items-end justify-between border-t pt-5">
              <span className="font-semibold text-stone-600">Итого</span>
              <span className="text-3xl font-extrabold text-stone-900">{money(total)}</span>
            </div>

            {error && (
              <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <Button
              className="mt-5 h-12 w-full rounded-xl text-base font-bold"
              onClick={submit}
              disabled={mutation.isPending}
            >
              {mutation.isPending && <Loader2 className="size-5 animate-spin" />}
              Создать заказ
            </Button>
          </div>
        </aside>
      </div>
    </div>
  )
}