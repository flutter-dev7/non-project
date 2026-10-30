import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'

import { ApiError, onSessionExpired, publicRequest, request } from '@/lib/api'
import { tokens } from '@/lib/tokens'

import { AuthContext, type AuthStatus, type AuthUser } from './auth-context'

type TokenPair = { accessToken: string; refreshToken: string }

const isAdmin = (user: AuthUser) => user.role.toLowerCase() === 'admin'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<AuthUser | null>(null)

  const resetSession = useCallback(() => {
    tokens.clear()
    setUser(null)
    setStatus('guest')
  }, [])

  useEffect(() => {
    onSessionExpired(resetSession)

    return () => onSessionExpired(null)
  }, [resetSession])

  useEffect(() => {
    let cancelled = false

    async function restore() {
      if (!tokens.access) {
        setStatus('guest')
        return
      }

      try {
        const me = await request<AuthUser>({ method: 'GET', url: '/api/Auth/me' })

        if (cancelled) return

        if (!isAdmin(me)) {
          resetSession()
          return
        }

        setUser(me)
        setStatus('authed')
      } catch {
        if (!cancelled) resetSession()
      }
    }

    restore()

    return () => {
      cancelled = true
    }
  }, [resetSession])

  const login = useCallback(
    async (email: string, password: string) => {
      const pair = await publicRequest<TokenPair>({
        method: 'POST',
        url: '/api/Auth/login',
        data: { email, password },
      })

      tokens.set(pair.accessToken, pair.refreshToken)

      try {
        const me = await request<AuthUser>({ method: 'GET', url: '/api/Auth/me' })

        if (!isAdmin(me)) {
          throw new ApiError('Вход только для администратора.')
        }

        setUser(me)
        setStatus('authed')
      } catch (e) {
        tokens.clear()
        throw e
      }
    },
    [],
  )

  const logout = useCallback(async () => {
    const refreshToken = tokens.refresh

    try {
      if (refreshToken) {
        await request({ method: 'POST', url: '/api/Auth/logout', data: { refreshToken } })
      }
    } catch {
      // выходим локально в любом случае
    } finally {
      resetSession()
    }
  }, [resetSession])

  const value = useMemo(() => ({ status, user, login, logout }), [status, user, login, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}