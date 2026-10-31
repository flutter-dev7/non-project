import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2, Package } from 'lucide-react'
import { useState } from 'react'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/lib/api'
import { cn } from '@/lib/utils'

import { createProduct, updateProduct, type Product } from './api'

function Form({ product, onClose }: { product: Product | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const isEdit = product !== null

  const [name, setName] = useState(product?.name ?? '')
  const [weight, setWeight] = useState(product?.weightInGrams?.toString() ?? '')
  const [price, setPrice] = useState(product ? String(product.price) : '')
  const [imageUrl, setImageUrl] = useState(product?.imageUrl ?? '')
  const [isActive, setIsActive] = useState(product?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: (input: {
      name: string
      weightInGrams: number | null
      price: number
      imageUrl: string | null
    }) => (isEdit ? updateProduct(product.id, { ...input, isActive }) : createProduct(input)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      onClose()
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Не удалось сохранить товар.'),
  })

  const submit = () => {
    setError(null)

    const priceNumber = Number(price.replace(',', '.'))
    const weightNumber = weight.trim() === '' ? null : Number(weight)

    if (!name.trim()) return setError('Введите название.')
    if (price.trim() === '' || !Number.isFinite(priceNumber) || priceNumber < 0) {
      return setError('Введите корректную цену.')
    }
    if (
      weightNumber !== null &&
      (!Number.isInteger(weightNumber) || weightNumber < 1 || weightNumber > 100000)
    ) {
      return setError('Вес — целое число от 1 до 100000 г.')
    }

    mutation.mutate({
      name: name.trim(),
      weightInGrams: weightNumber,
      price: priceNumber,
      imageUrl: imageUrl.trim() || null,
    })
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Название</Label>
        <Input
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="h-11"
          maxLength={200}
          autoFocus
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label>Цена, сом.</Label>
          <Input
            value={price}
            onChange={(e) => setPrice(e.target.value)}
            inputMode="decimal"
            className="h-11"
          />
        </div>
        <div className="space-y-2">
          <Label>Вес, г (необязательно)</Label>
          <Input
            value={weight}
            onChange={(e) => setWeight(e.target.value)}
            inputMode="numeric"
            className="h-11"
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label>Ссылка на фото (необязательно)</Label>
        <Input
          value={imageUrl}
          onChange={(e) => setImageUrl(e.target.value)}
          placeholder="https://…"
          maxLength={500}
          className="h-11"
        />
        {imageUrl.trim() && (
          <img
            key={imageUrl}
            src={imageUrl.trim()}
            alt=""
            className="mt-2 size-24 rounded-xl border object-cover"
            onError={(e) => {
              e.currentTarget.style.display = 'none'
            }}
          />
        )}
      </div>

      {isEdit && (
        <div className="space-y-2">
          <Label>Статус</Label>
          <div className="inline-flex gap-1 rounded-xl bg-stone-100 p-1">
            {[true, false].map((value) => (
              <button
                key={String(value)}
                type="button"
                onClick={() => setIsActive(value)}
                className={cn(
                  'rounded-lg px-4 py-1.5 text-sm font-bold transition-all',
                  isActive === value
                    ? 'bg-white text-stone-900 shadow-sm'
                    : 'text-stone-500 hover:text-stone-800',
                )}
              >
                {value ? 'В продаже' : 'Скрыт'}
              </button>
            ))}
          </div>
        </div>
      )}

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <Button className="h-11 w-full font-bold" onClick={submit} disabled={mutation.isPending}>
        {mutation.isPending && <Loader2 className="size-4 animate-spin" />}
        {isEdit ? 'Сохранить' : 'Добавить товар'}
      </Button>
    </div>
  )
}

export type ProductDialogTarget = { product: Product | null }

export function ProductDialog({
  target,
  onClose,
}: {
  target: ProductDialogTarget | null
  onClose: () => void
}) {
  return (
    <Dialog open={target !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl font-extrabold">
            <Package className="size-5 text-primary" />
            {target?.product ? 'Редактирование товара' : 'Новый товар'}
          </DialogTitle>
          <DialogDescription className="sr-only">Форма товара</DialogDescription>
        </DialogHeader>
        {target && <Form key={target.product?.id ?? 'new'} product={target.product} onClose={onClose} />}
      </DialogContent>
    </Dialog>
  )
}