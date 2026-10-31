import { request } from '@/lib/api'
import type { Paged } from '@/lib/types'

export type Product = {
  id: string
  name: string
  weightInGrams: number | null
  price: number
  imageUrl: string | null
  isActive: boolean
}

export type ProductInput = {
  name: string
  weightInGrams: number | null
  price: number
  imageUrl: string | null
}

export type ProductsQuery = {
  search?: string
  isActive?: boolean
  page: number
  pageSize: number
}

/** Активные товары для выбора в заказе. */
export async function fetchProducts(): Promise<Product[]> {
  const data = await request<Product[] | Paged<Product>>({
    method: 'GET',
    url: '/api/Products',
  })

  return Array.isArray(data) ? data : data.items
}

export function fetchProductsPage(query: ProductsQuery) {
  return request<Paged<Product>>({
    method: 'GET',
    url: '/api/Products/manage',
    params: {
      Search: query.search || undefined,
      IsActive: query.isActive,
      Page: query.page,
      PageSize: query.pageSize,
    },
  })
}

export function createProduct(input: ProductInput) {
  return request<Product>({ method: 'POST', url: '/api/Products', data: input })
}

export function updateProduct(id: string, input: ProductInput & { isActive: boolean }) {
  return request<Product>({ method: 'PUT', url: `/api/Products/${id}`, data: input })
}