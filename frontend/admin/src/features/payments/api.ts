import type { PaymentMethod } from '@/features/orders/types'
import { request } from '@/lib/api'
import type { Paged } from '@/lib/types'

export type Payment = {
  id: string
  shopId: string
  shopName: string
  orderId: string | null
  amount: number
  method: PaymentMethod
  receivedByUserId: string | null
  receivedByName: string | null
  isReversal: boolean
  isReversed: boolean
  reversalOfPaymentId: string | null
  reversalReason: string | null
  createdAt: string
}

export function fetchPayments(shopId: string) {
  return request<Paged<Payment>>({
    method: 'GET',
    url: '/api/Payments',
    params: { ShopId: shopId, Page: 1, PageSize: 50 },
  })
}

export function createPayment(input: {
  shopId: string
  orderId?: string
  amount: number
  method: PaymentMethod
}) {
  return request<unknown>({ method: 'POST', url: '/api/Payments', data: input })
}

export function reversePayment(id: string, reason: string) {
  return request<unknown>({
    method: 'POST',
    url: `/api/Payments/${id}/reverse`,
    data: { reason },
  })
}