import axios, {
  AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios'

import { tokens } from './tokens'

export type Envelope<T> = {
  isSuccess: boolean
  data: T
  error?: string | null
  errorType?: string | null
}

export class ApiError extends Error {
  errorType?: string
  status?: number

  constructor(message: string, errorType?: string, status?: number) {
    super(message)
    this.errorType = errorType
    this.status = status
  }
}

const baseURL = import.meta.env.VITE_API_URL as string

const publicHttp = axios.create({ baseURL, timeout: 15000 })
const http = axios.create({ baseURL, timeout: 15000 })

let sessionExpiredHandler: (() => void) | null = null

export function onSessionExpired(handler: (() => void) | null) {
  sessionExpiredHandler = handler
}

type TokenPair = { accessToken: string; refreshToken: string }
type RefreshOutcome = 'success' | 'rejected' | 'failed'

let refreshing: Promise<RefreshOutcome> | null = null

async function refreshTokens(): Promise<RefreshOutcome> {
  const refreshToken = tokens.refresh

  if (!refreshToken) return 'rejected'

  try {
    const res = await publicHttp.post<Envelope<TokenPair>>('/api/Auth/refresh', { refreshToken })

    if (res.data.isSuccess && res.data.data) {
      tokens.set(res.data.data.accessToken, res.data.data.refreshToken)
      return 'success'
    }

    return 'rejected'
  } catch (e) {
    const status = axios.isAxiosError(e) ? e.response?.status : undefined

    return status === 400 || status === 401 || status === 403 ? 'rejected' : 'failed'
  }
}

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean }

http.interceptors.request.use((config) => {
  const token = tokens.access

  if (token) config.headers.Authorization = `Bearer ${token}`

  return config
})

http.interceptors.response.use(undefined, async (error: AxiosError) => {
  const config = error.config as RetriableConfig | undefined

  if (error.response?.status !== 401 || !config || config._retried) throw error

  const sent = String(config.headers.Authorization ?? '')
  const current = tokens.access
  const alreadyRefreshed = !!current && sent !== `Bearer ${current}`

  if (!alreadyRefreshed) {
    refreshing ??= refreshTokens().finally(() => {
      refreshing = null
    })

    const outcome = await refreshing

    if (outcome === 'rejected') {
      tokens.clear()
      sessionExpiredHandler?.()
      throw error
    }

    if (outcome === 'failed') throw error
  }

  config._retried = true
  config.headers.Authorization = `Bearer ${tokens.access}`

  return http.request(config)
})

function unwrap<T>(body: Envelope<T>, status?: number): T {
  if (body && typeof body === 'object' && 'isSuccess' in body) {
    if (body.isSuccess) return body.data

    throw new ApiError(
      body.error ?? 'Не удалось выполнить запрос.',
      body.errorType ?? undefined,
      status,
    )
  }

  return body as unknown as T
}

function toApiError(e: unknown): ApiError {
  if (e instanceof ApiError) return e

  if (axios.isAxiosError(e)) {
    const body = e.response?.data as Partial<Envelope<unknown>> | undefined

    if (body && typeof body === 'object' && body.error) {
      return new ApiError(body.error, body.errorType ?? undefined, e.response?.status)
    }

    if (!e.response) return new ApiError('Нет соединения с сервером.', 'Network')

    if (e.response.status === 429) {
      return new ApiError('Слишком много запросов. Попробуйте позже.', 'TooManyRequests', 429)
    }

    return new ApiError('Не удалось выполнить запрос.', undefined, e.response.status)
  }

  return new ApiError('Неизвестная ошибка.')
}

export async function request<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const res = await http.request<Envelope<T>>(config)

    return unwrap(res.data, res.status)
  } catch (e) {
    throw toApiError(e)
  }
}

export async function publicRequest<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const res = await publicHttp.request<Envelope<T>>(config)

    return unwrap(res.data, res.status)
  } catch (e) {
    throw toApiError(e)
  }
}