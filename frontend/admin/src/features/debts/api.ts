import { request } from '@/lib/api'
import type { Paged } from '@/lib/types'

export type DebtItem = {
  shopId: string
  shopName: string
  shopPhone: string
  debt: number
  debtLimit: number | null
  isOverLimit: boolean
}

export function fetchDebts() {
  return request<Paged<DebtItem>>({
    method: 'GET',
    url: '/api/Debts',
    params: { Page: 1, PageSize: 100 },
  })
}