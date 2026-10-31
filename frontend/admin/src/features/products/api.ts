import { request } from '@/lib/api'
import type { Paged } from '@/lib/types'

export type Product = {
  id: string
  name: string
  weightInGrams: number
  price: number
  imageUrl: string | null
  isActive: boolean
}

export async function fetchProducts(): Promise<Product[]> {
  const data = await request<Product[] | Paged<Product>>({
    method: 'GET',
    url: '/api/Products',
  })

  return Array.isArray(data) ? data : data.items
}