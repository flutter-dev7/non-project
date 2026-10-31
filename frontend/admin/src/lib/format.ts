const TZ = 'Asia/Dushanbe'

export function money(value: number): string {
  return `${new Intl.NumberFormat('ru-RU').format(value)} сом.`
}

export function formatDate(iso: string): string {
  const [y, m, d] = iso.slice(0, 10).split('-')

  return `${d}.${m}.${y}`
}

export function formatDateTime(iso: string): string {
  const hasZone = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso)
  const date = new Date(hasZone ? iso : `${iso}Z`)

  return date.toLocaleString('ru-RU', {
    timeZone: TZ,
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Дата в формате yyyy-MM-dd по времени Таджикистана. */
export function dushanbeDate(offsetDays = 0): string {
  const date = new Date(Date.now() + offsetDays * 24 * 60 * 60 * 1000)

  return date.toLocaleDateString('en-CA', { timeZone: TZ })
}

/** Приводит ввод к виду +992XXXXXXXXX. */
export function toApiPhone(input: string): string {
  const digits = input.replace(/\D/g, '')

  if (digits.startsWith('992') && digits.length >= 12) return `+${digits}`

  return `+992${digits}`
}