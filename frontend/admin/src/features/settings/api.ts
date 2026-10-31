import { request } from '@/lib/api'

export type Settings = {
  orderCutoffTime: string
}

export function fetchSettings() {
  return request<Settings>({ method: 'GET', url: '/api/Settings' })
}

export function updateSettings(orderCutoffTime: string) {
  return request<unknown>({
    method: 'PUT',
    url: '/api/Settings',
    data: { orderCutoffTime },
  })
}