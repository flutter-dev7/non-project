import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { ChevronRight, Loader2, Plus, Search, Store } from 'lucide-react'
import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { NativeSelect } from '@/components/ui/native-select'
import { Skeleton } from '@/components/ui/skeleton'
import { money } from '@/lib/format'
import { useDebounced } from '@/lib/use-debounced'
import { cn } from '@/lib/utils'

import { AddShopDialog } from './add-shop-dialog'
import { fetchShopsPage } from './api'
import { ShopDialog } from './shop-dialog'

const PAGE_SIZE = 20

export function ShopsTab() {
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState<'active' | 'inactive' | 'all'>('active')
  const [page, setPage] = useState(1)
  const [openShop, setOpenShop] = useState<{ id: string; name: string } | null>(null)
  const [adding, setAdding] = useState(false)

  const debounced = useDebounced(search.trim())
  const isActive = activeFilter === 'all' ? undefined : activeFilter === 'active'

  const { data, isLoading, isFetching, error, refetch } = useQuery({
    queryKey: ['shops', 'list', debounced, isActive, page],
    queryFn: () => fetchShopsPage({ search: debounced, isActive, page, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  })

  const shops = data?.items ?? []
  const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / PAGE_SIZE))

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3 rounded-2xl border bg-white p-4 shadow-sm">
        <div className="relative min-w-64 flex-1">
          <Search className="pointer-events-none absolute left-3.5 top-1/2 size-5 -translate-y-1/2 text-stone-400" />
          <Input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(1)
            }}
            placeholder="Поиск по названию, адресу, телефону"
            className="h-10 rounded-xl pl-11"
          />
          {isFetching && (
            <Loader2 className="absolute right-3.5 top-1/2 size-4 -translate-y-1/2 animate-spin text-stone-400" />
          )}
        </div>
        <NativeSelect
          className="w-44 rounded-xl"
          value={activeFilter}
          onChange={(e) => {
            setActiveFilter(e.target.value as typeof activeFilter)
            setPage(1)
          }}
        >
          <option value="active">Активные</option>
          <option value="inactive">Отключённые</option>
          <option value="all">Все</option>
        </NativeSelect>
        <Button className="h-10 rounded-xl font-bold" onClick={() => setAdding(true)}>
          <Plus className="size-4" />
          Добавить магазин
        </Button>
      </div>

      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        {error ? (
          <div className="py-20 text-center">
            <p className="font-semibold text-red-600">Не удалось загрузить магазины</p>
            <Button variant="outline" className="mt-4" onClick={() => refetch()}>
              Повторить
            </Button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[860px] text-sm">
              <thead className="border-b bg-stone-50">
                <tr className="text-left text-xs font-bold uppercase tracking-wide text-stone-500">
                  <th className="px-5 py-3">Магазин</th>
                  <th className="px-5 py-3">Телефон</th>
                  <th className="px-5 py-3">Владелец</th>
                  <th className="px-5 py-3 text-right">Лимит долга</th>
                  <th className="px-5 py-3">Статус</th>
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
                  shops.map((shop) => (
                    <tr
                      key={shop.id}
                      onClick={() => setOpenShop({ id: shop.id, name: shop.name })}
                      className="group cursor-pointer border-b border-stone-100 transition-colors last:border-0 hover:bg-amber-50/50"
                    >
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 font-extrabold text-amber-800">
                            {shop.name.charAt(0).toUpperCase()}
                          </div>
                          <div className="min-w-0">
                            <p className="truncate font-bold text-stone-900">{shop.name}</p>
                            <p className="truncate text-xs text-stone-500">{shop.address}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-4 font-medium text-stone-700">{shop.phone}</td>
                      <td className="px-5 py-4">
                        {shop.ownerName ? (
                          <span className="font-medium text-stone-800">{shop.ownerName}</span>
                        ) : (
                          <span className="rounded-md bg-amber-50 px-2 py-1 text-xs font-bold text-amber-700">
                            Не привязан
                          </span>
                        )}
                      </td>
                      <td className="px-5 py-4 text-right font-bold text-stone-900">
                        {shop.debtLimit !== null ? (
                          money(shop.debtLimit)
                        ) : (
                          <span className="font-normal text-stone-300">—</span>
                        )}
                      </td>
                      <td className="px-5 py-4">
                        <span
                          className={cn(
                            'rounded-full px-2.5 py-1 text-xs font-bold',
                            shop.isActive
                              ? 'bg-emerald-50 text-emerald-700'
                              : 'bg-stone-100 text-stone-500',
                          )}
                        >
                          {shop.isActive ? 'Активен' : 'Отключён'}
                        </span>
                      </td>
                      <td className="py-4 pr-4">
                        <ChevronRight className="size-5 text-stone-300 transition-transform group-hover:translate-x-0.5 group-hover:text-primary" />
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>

            {!isLoading && shops.length === 0 && (
              <div className="py-20 text-center">
                <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-stone-100 text-stone-400">
                  <Store className="size-7" />
                </div>
                <p className="mt-4 font-bold text-stone-800">Магазины не найдены</p>
                <p className="mt-1 text-sm text-stone-500">Измените поиск или добавьте магазин</p>
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

      <ShopDialog
        shopId={openShop?.id ?? null}
        shopName={openShop?.name ?? ''}
        onClose={() => setOpenShop(null)}
      />
      <AddShopDialog open={adding} onClose={() => setAdding(false)} />
    </div>
  )
}