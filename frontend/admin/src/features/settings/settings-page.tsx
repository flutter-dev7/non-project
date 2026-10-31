import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CircleCheck, Clock, Loader2 } from 'lucide-react'
import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ApiError } from '@/lib/api'

import { fetchSettings, updateSettings } from './api'

export function SettingsPage() {
  const queryClient = useQueryClient()
  const { data, isLoading, error: loadError } = useQuery({
    queryKey: ['settings'],
    queryFn: fetchSettings,
  })

  const [time, setTime] = useState<string | null>(null)
  const [notice, setNotice] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const saved = data?.orderCutoffTime.slice(0, 5) ?? ''
  const value = time ?? saved
  const changed = time !== null && time !== saved

  const mutation = useMutation({
    mutationFn: () => updateSettings(value),
    onSuccess: () => {
      setTime(null)
      setError(null)
      setNotice(true)
      queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
    onError: (e) => {
      setNotice(false)
      setError(e instanceof ApiError ? e.message : 'Не удалось сохранить настройки.')
    },
  })

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-3xl font-extrabold text-stone-900">Настройки</h1>
        <p className="mt-1 text-stone-500">Правила работы приёма заказов</p>
      </div>

      <div className="rounded-2xl border bg-white p-6 shadow-sm">
        <div className="flex items-start gap-4">
          <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-700">
            <Clock className="size-6" />
          </div>
          <div>
            <h2 className="text-lg font-extrabold text-stone-900">Время приёма заказов</h2>
            <p className="mt-1 text-sm text-stone-500">
              Магазины и агенты могут оформить заказ на завтра только до этого времени (время
              Таджикистана). После него заказы принимаются на послезавтра и позже. Для
              администратора ограничение не действует.
            </p>
          </div>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-8">
            <Loader2 className="size-6 animate-spin text-primary" />
          </div>
        ) : loadError ? (
          <p className="mt-6 text-sm text-red-600">Не удалось загрузить настройки.</p>
        ) : (
          <div className="mt-6 flex flex-wrap items-center gap-3">
            <Input
              type="time"
              value={value}
              onChange={(e) => {
                setTime(e.target.value)
                setNotice(false)
              }}
              className="h-12 w-40 rounded-xl text-lg font-bold"
            />
            <Button
              className="h-12 rounded-xl px-6 font-bold"
              disabled={!changed || !value || mutation.isPending}
              onClick={() => mutation.mutate()}
            >
              {mutation.isPending && <Loader2 className="size-4 animate-spin" />}
              Сохранить
            </Button>
          </div>
        )}

        {notice && (
          <div className="mt-5 flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">
            <CircleCheck className="size-4" />
            Время приёма заказов сохранено
          </div>
        )}

        {error && (
          <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}
      </div>
    </div>
  )
}