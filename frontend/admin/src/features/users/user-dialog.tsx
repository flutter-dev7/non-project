import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Info, Loader2 } from 'lucide-react'
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
import { NativeSelect } from '@/components/ui/native-select'
import { ApiError } from '@/lib/api'
import { toApiPhone } from '@/lib/format'
import { cn } from '@/lib/utils'

import {
  createUser,
  fetchUser,
  roleLabels,
  updateUser,
  USER_ROLES,
  type UserDetails,
  type UserRole,
} from './api'

function RoleSelect({ value, onChange }: { value: UserRole; onChange: (role: UserRole) => void }) {
  return (
    <NativeSelect
      className="h-11"
      value={value}
      onChange={(e) => onChange(e.target.value as UserRole)}
    >
      {USER_ROLES.map((role) => (
        <option key={role} value={role}>
          {roleLabels[role]}
        </option>
      ))}
    </NativeSelect>
  )
}

function ErrorBox({ message }: { message: string | null }) {
  if (!message) return null

  return (
    <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {message}
    </div>
  )
}

function CreateForm({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<UserRole>('Courier')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      createUser({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: toApiPhone(phone),
        email: email.trim(),
        role,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      onClose()
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Не удалось создать пользователя.'),
  })

  const submit = () => {
    setError(null)

    if (firstName.trim().length < 2 || lastName.trim().length < 2) {
      return setError('Имя и фамилия — минимум 2 символа.')
    }
    if (!phone.trim()) return setError('Введите телефон.')
    if (!email.trim().includes('@')) return setError('Введите корректный email.')

    mutation.mutate()
  }

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label>Имя</Label>
          <Input
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            className="h-11"
            autoFocus
          />
        </div>
        <div className="space-y-2">
          <Label>Фамилия</Label>
          <Input value={lastName} onChange={(e) => setLastName(e.target.value)} className="h-11" />
        </div>
      </div>

      <div className="space-y-2">
        <Label>Телефон</Label>
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
        <Label>Email</Label>
        <Input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="h-11"
        />
      </div>

      <div className="space-y-2">
        <Label>Роль</Label>
        <RoleSelect value={role} onChange={setRole} />
      </div>

      <div className="flex gap-3 rounded-xl bg-sky-50 p-4 text-sm text-sky-800">
        <Info className="mt-0.5 size-4 shrink-0" />
        <p>
          Пароль пользователь задаёт сам: в приложении нужно нажать «Забыли пароль» и ввести этот
          email.
        </p>
      </div>

      <ErrorBox message={error} />

      <Button className="h-11 w-full font-bold" onClick={submit} disabled={mutation.isPending}>
        {mutation.isPending && <Loader2 className="size-4 animate-spin" />}
        Создать пользователя
      </Button>
    </div>
  )
}

function EditFields({ user, onClose }: { user: UserDetails; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [firstName, setFirstName] = useState(user.firstName)
  const [lastName, setLastName] = useState(user.lastName)
  const [email, setEmail] = useState(user.email ?? '')
  const [role, setRole] = useState<UserRole>(user.role as UserRole)
  const [isActive, setIsActive] = useState(user.isActive)
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      updateUser(user.id, {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim() || undefined,
        role,
        isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      onClose()
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Не удалось сохранить.'),
  })

  const submit = () => {
    setError(null)

    if (firstName.trim().length < 2 || lastName.trim().length < 2) {
      return setError('Имя и фамилия — минимум 2 символа.')
    }

    mutation.mutate()
  }

  return (
    <div className="space-y-4">
      <div className="rounded-xl bg-stone-50 p-4 text-sm">
        <p className="text-stone-500">
          Телефон: <span className="font-bold text-stone-800">{user.phone}</span>
        </p>
        <p className="mt-1 text-stone-500">
          Email:{' '}
          <span className="font-bold text-stone-800">
            {user.emailConfirmed ? 'подтверждён' : 'не подтверждён'}
          </span>
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label>Имя</Label>
          <Input value={firstName} onChange={(e) => setFirstName(e.target.value)} className="h-11" />
        </div>
        <div className="space-y-2">
          <Label>Фамилия</Label>
          <Input value={lastName} onChange={(e) => setLastName(e.target.value)} className="h-11" />
        </div>
      </div>

      <div className="space-y-2">
        <Label>Email</Label>
        <Input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="h-11"
        />
      </div>

      <div className="space-y-2">
        <Label>Роль</Label>
        <RoleSelect value={role} onChange={setRole} />
      </div>

      <div className="space-y-2">
        <Label>Доступ</Label>
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
              {value ? 'Активен' : 'Заблокирован'}
            </button>
          ))}
        </div>
      </div>

      <ErrorBox message={error} />

      <Button className="h-11 w-full font-bold" onClick={submit} disabled={mutation.isPending}>
        {mutation.isPending && <Loader2 className="size-4 animate-spin" />}
        Сохранить
      </Button>
    </div>
  )
}

function EditForm({ id, onClose }: { id: string; onClose: () => void }) {
  const { data, isLoading, error } = useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: () => fetchUser(id),
  })

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="size-7 animate-spin text-primary" />
      </div>
    )
  }

  if (!data) {
    return (
      <p className="py-8 text-center text-sm text-red-600">
        {error instanceof ApiError ? error.message : 'Не удалось загрузить пользователя.'}
      </p>
    )
  }

  return <EditFields user={data} onClose={onClose} />
}

export type UserDialogTarget = { mode: 'create' } | { mode: 'edit'; id: string }

export function UserDialog({
  target,
  onClose,
}: {
  target: UserDialogTarget | null
  onClose: () => void
}) {
  return (
    <Dialog open={target !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-xl font-extrabold">
            {target?.mode === 'edit' ? 'Редактирование пользователя' : 'Новый пользователь'}
          </DialogTitle>
          <DialogDescription className="sr-only">Форма пользователя</DialogDescription>
        </DialogHeader>

        {target?.mode === 'create' && <CreateForm onClose={onClose} />}
        {target?.mode === 'edit' && <EditForm key={target.id} id={target.id} onClose={onClose} />}
      </DialogContent>
    </Dialog>
  )
}