import { useQuery } from '@tanstack/react-query'
import { Loader2, Search, Store } from 'lucide-react'
import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useDebounced } from '@/lib/use-debounced'
import { money } from '@/lib/format'

import { fetchShops, type Shop } from '@/features/shops/api'

export function ShopPicker({
  value,
  onChange,
}: {
  value: Shop | null
  onChange: (shop: Shop | null) => void
}) {
  const [search, setSearch] = useState('')
  const debounced = useDebounced(search.trim())

  const { data, isFetching } = useQuery({
    queryKey: ['shops', 'picker', debounced],
    queryFn: () => fetchShops(debounced),
    enabled: value === null,
  })

  if (value) {
    return (
      <div className="flex items-center gap-4 rounded-2xl border-2 border-primary/40 bg-amber-50/60 p-4">
        <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-800">
          <Store className="size-6" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="truncate text-lg font-extrabold text-stone-900">{value.name}</p>
          <p className="truncate text-sm text-stone-500">{value.address}</p>
          {value.debtLimit !== null && (
            <p className="mt-0.5 text-xs font-semibold text-amber-700">
              Лимит долга: {money(value.debtLimit)}
            </p>
          )}
        </div>
        <Button variant="outline" className="rounded-xl bg-white" onClick={() => onChange(null)}>
          Изменить
        </Button>
      </div>
    )
  }

  const shops = data?.items ?? []

  return (
    <div className="space-y-3">
      <div className="relative">
        <Search className="pointer-events-none absolute left-3.5 top-1/2 size-5 -translate-y-1/2 text-stone-400" />
        <Input
          autoFocus
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Поиск магазина по названию, адресу, телефону"
          className="h-12 rounded-xl pl-11 text-base"
        />
        {isFetching && (
          <Loader2 className="absolute right-3.5 top-1/2 size-5 -translate-y-1/2 animate-spin text-stone-400" />
        )}
      </div>

      <div className="overflow-hidden rounded-2xl border bg-white">
        {shops.length === 0 && !isFetching ? (
          <p className="py-8 text-center text-sm text-stone-500">Магазины не найдены</p>
        ) : (
          shops.map((shop) => (
            <button
              key={shop.id}
              type="button"
              onClick={() => onChange(shop)}
              className="flex w-full items-center gap-3 border-b border-stone-100 px-4 py-3 text-left transition-colors last:border-0 hover:bg-amber-50/60"
            >
              <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 font-extrabold text-amber-800">
                {shop.name.charAt(0).toUpperCase()}
              </div>
              <div className="min-w-0">
                <p className="truncate font-bold text-stone-900">{shop.name}</p>
                <p className="truncate text-xs text-stone-500">{shop.address}</p>
              </div>
            </button>
          ))
        )}
      </div>
    </div>
  )
}