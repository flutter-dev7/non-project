import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
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
import { toApiPhone } from '@/lib/format'

import { createShop } from './api'

function Form({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [address, setAddress] = useState('')
  const [phone, setPhone] = useState('')
  const [contact, setContact] = useState('')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      createShop({
        name: name.trim(),
        address: address.trim(),
        phone: toApiPhone(phone),
        contactName: contact.trim() || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shops'] })
      onClose()
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Не удалось добавить магазин.'),
  })

  const submit = () => {
    setError(null)

    if (!name.trim() || !address.trim() || !phone.trim()) {
      return setError('Заполните название, адрес и телефон.')
    }

    mutation.mutate()
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Название</Label>
        <Input value={name} onChange={(e) => setName(e.target.value)} className="h-11" autoFocus />
      </div>
      <div className="space-y-2">
        <Label>Адрес</Label>
        <Input value={address} onChange={(e) => setAddress(e.target.value)} className="h-11" />
      </div>
      <div className="space-y-2">
        <Label>Телефон магазина</Label>
        <div className="flex items-center gap-2">
          <span className="rounded-lg bg-stone-100 px-3 py-2.5 text-sm font-semibold text-stone-600">
            +992
          </span>
          <Input
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="900 00 00 00"
            className="h-11"
          />
        </div>
      </div>
      <div className="space-y-2">
        <Label>Контактное лицо (необязательно)</Label>
        <Input value={contact} onChange={(e) => setContact(e.target.value)} className="h-11" />
      </div>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <Button className="h-11 w-full font-bold" onClick={submit} disabled={mutation.isPending}>
        {mutation.isPending && <Loader2 className="size-4 animate-spin" />}
        Добавить магазин
      </Button>
    </div>
  )
}

export function AddShopDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Dialog open={open} onOpenChange={(value) => !value && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-xl font-extrabold">Новый магазин</DialogTitle>
          <DialogDescription className="sr-only">Добавление магазина</DialogDescription>
        </DialogHeader>
        {open && <Form onClose={onClose} />}
      </DialogContent>
    </Dialog>
  )
}