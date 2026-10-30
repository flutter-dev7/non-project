import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, Loader2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Navigate } from 'react-router-dom'
import { z } from 'zod'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/lib/api'

import { useAuth } from './auth-context'

const schema = z.object({
  email: z.string().min(1, 'Введите email').email('Некорректный email'),
  password: z.string().min(1, 'Введите пароль'),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const { status, login } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  if (status === 'authed') return <Navigate to="/orders" replace />

  const onSubmit = async (values: FormValues) => {
    setError(null)

    try {
      await login(values.email.trim(), values.password)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Не удалось войти.')
    }
  }

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <div className="relative hidden flex-col justify-between overflow-hidden bg-gradient-to-br from-[#3B2A1A] via-[#7A4E16] to-[#C8801A] p-12 text-white lg:flex">
        <span className="text-5xl" style={{ fontFamily: "'Playfair Display', serif", fontWeight: 900 }}>
          Нон
        </span>
        <div>
          <h2 className="text-4xl font-extrabold leading-tight">
            Свежий хлеб —<br />
            под полным контролем
          </h2>
          <p className="mt-4 max-w-md text-lg text-white/80">
            Заказы, производство, магазины и долги в одном месте.
          </p>
        </div>
        <span className="text-sm text-white/60">Панель администратора</span>
      </div>

      <div className="flex items-center justify-center bg-stone-50 p-6">
        <div className="w-full max-w-sm">
          <span
            className="text-4xl text-[#3B2A1A] lg:hidden"
            style={{ fontFamily: "'Playfair Display', serif", fontWeight: 900 }}
          >
            Нон
          </span>

          <h1 className="mt-6 text-3xl font-extrabold text-stone-900 lg:mt-0">Вход</h1>
          <p className="mt-2 text-stone-500">Войдите в панель администратора</p>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5" noValidate>
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                placeholder="admin@example.com"
                className="h-11"
                disabled={isSubmitting}
                {...register('email')}
              />
              {errors.email && <p className="text-sm text-red-600">{errors.email.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Пароль</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  className="h-11 pr-11"
                  disabled={isSubmitting}
                  {...register('password')}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-stone-500 hover:text-stone-800"
                  aria-label={showPassword ? 'Скрыть пароль' : 'Показать пароль'}
                >
                  {showPassword ? <EyeOff className="size-5" /> : <Eye className="size-5" />}
                </button>
              </div>
              {errors.password && <p className="text-sm text-red-600">{errors.password.message}</p>}
            </div>

            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <Button type="submit" className="h-11 w-full text-base font-bold" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="size-4 animate-spin" />}
              Войти
            </Button>
          </form>
        </div>
      </div>
    </div>
  )
}