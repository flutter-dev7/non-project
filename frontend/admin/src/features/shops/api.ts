import { request } from '@/lib/api'
import type { Paged } from '@/lib/types'

export type Shop = {
  id: string
  name: string
  address: string
  phone: string
  contactName: string | null
  debtLimit: number | null
  isActive: boolean
}

export function fetchShops(search: string) {
  return request<Paged<Shop>>({
    method: 'GET',
    url: '/api/Shops',
    params: {
      Search: search || undefined,
      IsActive: true,
      Page: 1,
      PageSize: 8,
    },
  })
}