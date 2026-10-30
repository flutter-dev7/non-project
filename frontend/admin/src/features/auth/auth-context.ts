import { createContext, useContext } from 'react'

export type AuthUser = {
  id: string
  firstName: string
  lastName: string
  phone: string
  email: string
  role: string
}

export type AuthStatus = 'loading' | 'authed' | 'guest'

export type AuthContextValue = {
  status: AuthStatus
  user: AuthUser | null
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)

  if (!value) throw new Error('useAuth must be used inside AuthProvider')

  return value
}