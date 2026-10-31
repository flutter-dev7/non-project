import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ImagePlus, Loader2, Package, RefreshCw, Trash2 } from 'lucide-react'
import { useRef, useState } from 'react'

import { AppDialog } from '@/components/app-dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/lib/api'
import { resolveImageUrl } from '@/lib/image'
import { cn } from '@/lib/utils'

import { createProduct, updateProduct, uploadProductImage, type Product } from './api'

const MAX_BYTES = 5 * 1024 * 1024

function ImageField({ value, onChange }: { value: string; onChange: (url: string) => void }) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)
  const [dragOver, setDragOver] = useState(false)
  const [showUrl, setShowUrl] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const upload = async (file: File | undefined) => {
    if (!file) return

    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      return setError('Допустимые форматы: JPG, PNG, WEBP.')
    }
    if (file.size > MAX_BYTES) return setError('Размер фото не должен превышать 5 МБ.')

    setError(null)
    setUploading(true)

    try {
      onChange(await uploadProductImage(file))
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Не удалось загрузить фото.')
    } finally {
      setUploading(false)
    }
  }

  const preview = resolveImageUrl(value.trim())

  return (
    <div className="space-y-3">
      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={(e) => {
          upload(e.target.files?.[0])
          e.target.value = ''
        }}
      />

      {preview ? (
        <div className="flex items-center gap-4 rounded-2xl border bg-stone-50 p-3">
          <img
            key={preview}
            src={preview}
            alt=""
            className="size-24 shrink-0 rounded-xl border bg-white object-cover"
          />
          <div className="flex flex-col gap-2">
            <Button
              variant="outline"
              size="sm"
              className="rounded-lg bg-white"
              disabled={uploading}
              onClick={() => inputRef.current?.click()}
            >
              {uploading ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                <RefreshCw className="size-4" />
              )}
              Заменить
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="rounded-lg text-red-600 hover:text-red-700"
              disabled={uploading}
              onClick={() => onChange('')}
            >
              <Trash2 className="size-4" />
              Удалить
            </Button>
          </div>
        </div>
      ) : (
        <button
          type="button"
          disabled={uploading}
          onClick={() => inputRef.current?.click()}
          onDragOver={(e) => {
            e.preventDefault()
            setDragOver(true)
          }}
          onDragLeave={() => setDragOver(false)}
          onDrop={(e) => {
            e.preventDefault()
            setDragOver(false)
            upload(e.dataTransfer.files?.[0])
          }}
          className={cn(
            'flex w-full flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed px-4 py-8 text-center transition-colors',
            dragOver
              ? 'border-primary bg-amber-50'
              : 'border-stone-300 bg-stone-50 hover:border-primary/60 hover:bg-amber-50/40',
          )}
        >
          {uploading ? (
            <Loader2 className="size-8 animate-spin text-primary" />
          ) : (
            <ImagePlus className="size-8 text-primary" />
          )}
          <p className="text-sm font-bold text-stone-800">
            {uploading ? 'Загрузка…' : 'Выберите фото или перетащите сюда'}
          </p>
          <p className="text-xs text-stone-500">JPG, PNG или WEBP, до 5 МБ</p>
        </button>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}

      <button
        type="button"
        onClick={() => setShowUrl((v) => !v)}
        className="text-xs font-semibold text-stone-500 underline-offset-2 hover:text-stone-800 hover:underline"
      >
        {showUrl ? 'Скрыть ссылку' : 'Или указать ссылку на фото'}
      </button>

      {showUrl && (
        <Input
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="https://…"
          maxLength={500}
          className="h-10 rounded-xl"
        />
      )}
    </div>
  )
}

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
    <div className="space-y-5">
      <div className="space-y-2">
        <Label className="font-semibold text-stone-700">Название</Label>
        <Input
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="h-11 rounded-xl"
          maxLength={200}
          autoFocus
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label className="font-semibold text-stone-700">Цена, сом.</Label>
          <Input
            value={price}
            onChange={(e) => setPrice(e.target.value)}
            inputMode="decimal"
            className="h-11 rounded-xl"
          />
        </div>
        <div className="space-y-2">
          <Label className="font-semibold text-stone-700">Вес, г</Label>
          <Input
            value={weight}
            onChange={(e) => setWeight(e.target.value)}
            inputMode="numeric"
            placeholder="Необязательно"
            className="h-11 rounded-xl"
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label className="font-semibold text-stone-700">Фото</Label>
        <ImageField value={imageUrl} onChange={setImageUrl} />
      </div>

      {isEdit && (
        <div className="space-y-2">
          <Label className="font-semibold text-stone-700">Статус</Label>
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

      <Button
        className="h-12 w-full rounded-xl text-base font-bold"
        onClick={submit}
        disabled={mutation.isPending}
      >
        {mutation.isPending && <Loader2 className="size-5 animate-spin" />}
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
  const isEdit = !!target?.product

  return (
    <AppDialog
      open={target !== null}
      onClose={onClose}
      icon={Package}
      title={isEdit ? 'Редактирование товара' : 'Новый товар'}
      subtitle={isEdit ? target?.product?.name : 'Заполните данные и добавьте фото'}
    >
      {target && (
        <Form key={target.product?.id ?? 'new'} product={target.product} onClose={onClose} />
      )}
    </AppDialog>
  )
}