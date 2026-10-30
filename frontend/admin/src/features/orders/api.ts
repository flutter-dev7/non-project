import { request } from '@/lib/api'
import type { Paged } from '@/lib/types'

import type {
  Courier,
  Order,
  OrderDetails,
  OrderStatus,
  PaymentMethod,
} from './types'

export type OrdersFilter = {
  status?: OrderStatus
  dateFrom?: string
  dateTo?: string
  page: number
  pageSize: number
}

export function fetchOrders(filter: OrdersFilter) {
  return request<Paged<Order>>({
    method: 'GET',
    url: '/api/Orders',
    params: {
      Status: filter.status,
      DeliveryDateFrom: filter.dateFrom || undefined,
      DeliveryDateTo: filter.dateTo || undefined,
      Page: filter.page,
      PageSize: filter.pageSize,
    },
  })
}

export function fetchOrder(id: string) {
  return request<OrderDetails>({ method: 'GET', url: `/api/Orders/${id}` })
}

export function changeOrderStatus(
  id: string,
  body: { status: OrderStatus; paymentAmount?: number; paymentMethod?: PaymentMethod },
) {
  return request<unknown>({ method: 'PUT', url: `/api/Orders/${id}/status`, data: body })
}

export function assignCourier(id: string, courierId: string) {
  return request<unknown>({ method: 'PUT', url: `/api/Orders/${id}/courier`, data: { courierId } })
}

export function cancelOrder(id: string) {
  return request<unknown>({ method: 'POST', url: `/api/Orders/${id}/cancel` })
}

export function fetchCouriers() {
  return request<Paged<Courier>>({
    method: 'GET',
    url: '/api/Users',
    params: { Role: 'Courier', IsActive: true, Page: 1, PageSize: 100 },
  })
}