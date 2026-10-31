const base = (import.meta.env.VITE_API_URL as string).replace(/\/$/, '')

export function resolveImageUrl(url: string | null | undefined): string | null {
  if (!url) return null

  return url.startsWith('/') ? `${base}${url}` : url
}