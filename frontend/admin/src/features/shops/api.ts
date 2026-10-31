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
  ownerUserId: string | null
  ownerPhone: string | null
  ownerName: string | null
}

export type UnpaidOrder = {
  orderId: string
  deliveryDate: string
  totalAmount: number
  paidAmount: number
  remainingAmount: number
}

export type ShopBalance = {
  shopId: string
  shopName: string
  deliveredTotal: number
  paidTotal: number
  debt: number
  pendingDebtOrdersTotal: number
  debtLimit: number | null
  unpaidOrders: UnpaidOrder[]
}

export type ShopsQuery = {
  search?: string
  isActive?: boolean
  page: number
  pageSize: number
}

export type NewShopInput = {
  name: string
  address: string
  phone: string
  contactName?: string
}

/** Короткий список для выбора магазина. */
export function fetchShops(search: string) {
  return request<Paged<Shop>>({
    method: 'GET',
    url: '/api/Shops',
    params: { Search: search || undefined, IsActive: true, Page: 1, PageSize: 8 },
  })
}

export function fetchShopsPage(query: ShopsQuery) {
  return request<Paged<Shop>>({
    method: 'GET',
    url: '/api/Shops',
    params: {
      Search: query.search || undefined,
      IsActive: query.isActive,
      Page: query.page,
      PageSize: query.pageSize,
    },
  })
}

export function fetchShop(id: string) {
  return request<Shop>({ method: 'GET', url: `/api/Shops/${id}` })
}

export function fetchShopBalance(id: string) {
  return request<ShopBalance>({ method: 'GET', url: `/api/Shops/${id}/balance` })
}

export function createShop(input: NewShopInput) {
  return request<Shop>({ method: 'POST', url: '/api/Shops', data: input })
}

export function setDebtLimit(id: string, debtLimit: number | null) {
  return request<unknown>({
    method: 'PUT',
    url: `/api/Shops/${id}/debt-limit`,
    data: { debtLimit },
  })
}

export function setShopOwner(id: string, ownerPhone: string) {
  return request<unknown>({
    method: 'PUT',
    url: `/api/Shops/${id}/owner`,
    data: { ownerPhone },
  })
}

export type UpdateShopInput = NewShopInput & { isActive: boolean }

export function updateShop(id: string, input: UpdateShopInput) {
  return request<Shop>({ method: 'PUT', url: `/api/Shops/${id}`, data: input })
}