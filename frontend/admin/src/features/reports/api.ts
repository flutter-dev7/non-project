import { request } from '@/lib/api'

export type ProductionItem = {
  productId: string
  productName: string
  quantity: number
}

export type ProductionReport = {
  date: string
  ordersCount: number
  totalQuantity: number
  items: ProductionItem[]
}

export type SalesDay = {
  date: string
  ordersCount: number
  amount: number
}

export type SalesProduct = {
  productId: string
  productName: string
  quantity: number
  amount: number
}

export type SalesReport = {
  from: string
  to: string
  ordersCount: number
  totalQuantity: number
  totalAmount: number
  receivedPayments: number
  byDay: SalesDay[]
  byProduct: SalesProduct[]
}

export function fetchProduction(date: string) {
  return request<ProductionReport>({
    method: 'GET',
    url: '/api/Reports/production',
    params: { Date: date },
  })
}

export function fetchSales(from: string, to: string) {
  return request<SalesReport>({
    method: 'GET',
    url: '/api/Reports/sales',
    params: { From: from, To: to },
  })
}