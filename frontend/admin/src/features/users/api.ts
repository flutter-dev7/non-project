import { request } from '@/lib/api'
import type { Paged } from '@/lib/types'

export type UserRole = 'Shop' | 'Agent' | 'Courier' | 'Admin'

export const USER_ROLES: UserRole[] = ['Shop', 'Agent', 'Courier', 'Admin']

export const roleLabels: Record<UserRole, string> = {
  Shop: 'Магазин',
  Agent: 'Агент',
  Courier: 'Курьер',
  Admin: 'Администратор',
}

export type UserListItem = {
  id: string
  firstName?: string
  lastName?: string
  fullName?: string
  phone: string
  email: string | null
  emailConfirmed?: boolean
  role: string
  isActive: boolean
  createdAt?: string
}

export type UserDetails = {
  id: string
  firstName: string
  lastName: string
  fullName: string
  phone: string
  email: string | null
  emailConfirmed: boolean
  role: string
  isActive: boolean
  createdAt: string
}

export function userName(u: { fullName?: string; firstName?: string; lastName?: string }): string {
  return u.fullName || `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim()
}

export type UsersQuery = {
  search?: string
  role?: UserRole
  isActive?: boolean
  page: number
  pageSize: number
}

export function fetchUsersPage(query: UsersQuery) {
  return request<Paged<UserListItem>>({
    method: 'GET',
    url: '/api/Users',
    params: {
      Search: query.search || undefined,
      Role: query.role,
      IsActive: query.isActive,
      Page: query.page,
      PageSize: query.pageSize,
    },
  })
}

export function fetchUser(id: string) {
  return request<UserDetails>({ method: 'GET', url: `/api/Users/${id}` })
}

export type NewUserInput = {
  firstName: string
  lastName: string
  phone: string
  email: string
  role: UserRole
}

export function createUser(input: NewUserInput) {
  return request<unknown>({ method: 'POST', url: '/api/Users', data: input })
}

export type UpdateUserInput = {
  firstName: string
  lastName: string
  email?: string
  role: UserRole
  isActive: boolean
}

export function updateUser(id: string, input: UpdateUserInput) {
  return request<unknown>({ method: 'PUT', url: `/api/Users/${id}`, data: input })
}