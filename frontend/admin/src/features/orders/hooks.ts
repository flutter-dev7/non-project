import { keepPreviousData, useQueries, useQuery } from '@tanstack/react-query'

import { fetchCouriers, fetchOrder, fetchOrders, type OrdersFilter } from './api'
import { ORDER_STATUSES, type OrderStatus } from './types'

export function useOrders(filter: OrdersFilter) {
  return useQuery({
    queryKey: ['orders', 'list', filter],
    queryFn: () => fetchOrders(filter),
    placeholderData: keepPreviousData,
  })
}

export function useOrder(id: string) {
  return useQuery({
    queryKey: ['orders', 'detail', id],
    queryFn: () => fetchOrder(id),
  })
}

export function useCouriers() {
  return useQuery({
    queryKey: ['users', 'couriers'],
    queryFn: fetchCouriers,
    staleTime: 5 * 60 * 1000,
  })
}

/** Количество заказов по каждому статусу за период. Ключ 'all' — все заказы. */
export function useStatusCounts(dateFrom: string, dateTo: string) {
  const statuses: (OrderStatus | undefined)[] = [undefined, ...ORDER_STATUSES]

  const results = useQueries({
    queries: statuses.map((status) => ({
      queryKey: ['orders', 'count', status ?? 'all', dateFrom, dateTo],
      queryFn: () =>
        fetchOrders({ status, dateFrom, dateTo, page: 1, pageSize: 1 }),
      select: (data: { totalCount: number }) => data.totalCount,
      staleTime: 15_000,
    })),
  })

  const counts: Record<string, number | undefined> = {}

  statuses.forEach((status, index) => {
    counts[status ?? 'all'] = results[index].data
  })

  return counts
}

export function useNewOrdersCount() {
  return useQuery({
    queryKey: ['orders', 'count', 'New', '', ''],
    queryFn: () =>
      fetchOrders({ status: 'New', dateFrom: '', dateTo: '', page: 1, pageSize: 1 }),
    select: (data) => data.totalCount,
    refetchInterval: 30_000,
  })
}