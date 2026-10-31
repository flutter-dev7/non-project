import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronRight, Loader2, Plus, Search, Users } from 'lucide-react'
import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { NativeSelect } from '@/components/ui/native-select'
import { Skeleton } from '@/components/ui/skeleton'
import { formatDate } from '@/lib/format'
import { useDebounced } from '@/lib/use-debounced'
import { cn } from '@/lib/utils'

import { fetchUsersPage, roleLabels, USER_ROLES, userName, type UserRole } from './api'
import { UserDialog, type UserDialogTarget } from './user-dialog'

const PAGE_SIZE = 20

const roleStyles: Record<string, string> = {
  Admin: 'bg-violet-50 text-violet-700',
  Agent: 'bg-sky-50 text-sky-700',
  Courier: 'bg-amber-50 text-amber-700',
  Shop: 'bg-emerald-50 text-emerald-700',
}

export function UsersPage() {
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<UserRole | ''>('')
  const [status, setStatus] = useState<'all' | 'active' | 'blocked'>('all')
  const [page, setPage] = useState(1)
  const [target, setTarget] = useState<UserDialogTarget | null>(null)

  const debounced = useDebounced(search.trim())
  const isActive = status === 'all' ? undefined : status === 'active'

  const { data, isLoading, isFetching, error, refetch } = useQuery({
    queryKey: ['users', 'list', debounced, role, isActive, page],
    queryFn: () =>
      fetchUsersPage({
        search: debounced,
        role: role || undefined,
        isActive,
        page,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
  })

  const users = data?.items ?? []
  const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / PAGE_SIZE))

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-extrabold text-stone-900">Пользователи</h1>
          <p className="mt-1 text-stone-500">Агенты, курьеры, администраторы и владельцы магазинов</p>
        </div>
        <Button className="h-10 rounded-xl font-bold" onClick={() => setTarget({ mode: 'create' })}>
          <Plus className="size-4" />
          Добавить пользователя
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-2xl border bg-white p-4 shadow-sm">
        <div className="relative min-w-64 flex-1">
          <Search className="pointer-events-none absolute left-3.5 top-1/2 size-5 -translate-y-1/2 text-stone-400" />
          <Input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(1)
            }}
            placeholder="Поиск по имени, телефону, email"
            className="h-10 rounded-xl pl-11"
          />
          {isFetching && (
            <Loader2 className="absolute right-3.5 top-1/2 size-4 -translate-y-1/2 animate-spin text-stone-400" />
          )}
        </div>
        <NativeSelect
          className="w-48 rounded-xl"
          value={role}
          onChange={(e) => {
            setRole(e.target.value as UserRole | '')
            setPage(1)
          }}
        >
          <option value="">Все роли</option>
          {USER_ROLES.map((r) => (
            <option key={r} value={r}>
              {roleLabels[r]}
            </option>
          ))}
        </NativeSelect>
        <NativeSelect
          className="w-44 rounded-xl"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as typeof status)
            setPage(1)
          }}
        >
          <option value="all">Любой статус</option>
          <option value="active">Активные</option>
          <option value="blocked">Заблокированные</option>
        </NativeSelect>
      </div>

      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        {error ? (
          <div className="py-20 text-center">
            <p className="font-semibold text-red-600">Не удалось загрузить пользователей</p>
            <Button variant="outline" className="mt-4" onClick={() => refetch()}>
              Повторить
            </Button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-sm">
              <thead className="border-b bg-stone-50">
                <tr className="text-left text-xs font-bold uppercase tracking-wide text-stone-500">
                  <th className="px-5 py-3">Пользователь</th>
                  <th className="px-5 py-3">Телефон</th>
                  <th className="px-5 py-3">Роль</th>
                  <th className="px-5 py-3">Статус</th>
                  <th className="px-5 py-3">Создан</th>
                  <th className="w-10" />
                </tr>
              </thead>
              <tbody>
                {isLoading &&
                  Array.from({ length: 6 }).map((_, i) => (
                    <tr key={i} className="border-b border-stone-100">
                      <td colSpan={6} className="px-5 py-4">
                        <Skeleton className="h-10 w-full rounded-xl" />
                      </td>
                    </tr>
                  ))}

                {!isLoading &&
                  users.map((user) => {
                    const name = userName(user)

                    return (
                      <tr
                        key={user.id}
                        onClick={() => setTarget({ mode: 'edit', id: user.id })}
                        className="group cursor-pointer border-b border-stone-100 transition-colors last:border-0 hover:bg-amber-50/50"
                      >
                        <td className="px-5 py-3">
                          <div className="flex items-center gap-3">
                            <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-stone-100 font-extrabold text-stone-600">
                              {name.charAt(0).toUpperCase()}
                            </div>
                            <div className="min-w-0">
                              <p className="truncate font-bold text-stone-900">{name}</p>
                              <p className="truncate text-xs text-stone-500">{user.email ?? '—'}</p>
                            </div>
                          </div>
                        </td>
                        <td className="px-5 py-3 font-medium text-stone-700">{user.phone}</td>
                        <td className="px-5 py-3">
                          <span
                            className={cn(
                              'rounded-full px-2.5 py-1 text-xs font-bold',
                              roleStyles[user.role] ?? 'bg-stone-100 text-stone-600',
                            )}
                          >
                            {roleLabels[user.role as UserRole] ?? user.role}
                          </span>
                        </td>
                        <td className="px-5 py-3">
                          <span
                            className={cn(
                              'rounded-full px-2.5 py-1 text-xs font-bold',
                              user.isActive
                                ? 'bg-emerald-50 text-emerald-700'
                                : 'bg-red-50 text-red-700',
                            )}
                          >
                            {user.isActive ? 'Активен' : 'Заблокирован'}
                          </span>
                        </td>
                        <td className="px-5 py-3 text-stone-500">
                          {user.createdAt ? formatDate(user.createdAt) : '—'}
                        </td>
                        <td className="py-3 pr-4">
                          <ChevronRight className="size-5 text-stone-300 transition-transform group-hover:translate-x-0.5 group-hover:text-primary" />
                        </td>
                      </tr>
                    )
                  })}
              </tbody>
            </table>

            {!isLoading && users.length === 0 && (
              <div className="py-20 text-center">
                <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-stone-100 text-stone-400">
                  <Users className="size-7" />
                </div>
                <p className="mt-4 font-bold text-stone-800">Пользователи не найдены</p>
              </div>
            )}
          </div>
        )}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-stone-500">
            Страница {page} из {totalPages} · всего {data?.totalCount}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              className="rounded-xl bg-white"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Назад
            </Button>
            <Button
              variant="outline"
              className="rounded-xl bg-white"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Вперёд
            </Button>
          </div>
        </div>
      )}

      <UserDialog target={target} onClose={() => setTarget(null)} />
    </div>
  )
}