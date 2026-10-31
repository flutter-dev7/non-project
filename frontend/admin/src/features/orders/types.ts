export type OrderStatus = 'New' | 'Accepted' | 'Baking' | 'OnTheWay' | 'Delivered' | 'Cancelled'
export type PaymentType = 'Now' | 'Debt'
export type PaymentMethod = 'Cash' | 'Transfer'

export type OrderItem = {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export type Order = {
  id: string
  shopId: string
  shopName: string
  shopAddress: string
  shopPhone: string
  status: OrderStatus
  deliveryDate: string
  paymentType: PaymentType
  totalAmount: number
  courierId: string | null
  courierName: string | null
  source: string
  note: string | null
  createdAt: string
  items: OrderItem[]
}

export type OrderHistoryEntry = {
  status: OrderStatus
  changedByUserId: string | null
  changedByName: string | null
  changedAt: string
}

export type OrderDetails = Order & { history: OrderHistoryEntry[] }

export type Courier = {
  id: string
  firstName?: string
  lastName?: string
  fullName?: string
  phone: string
}

export const ORDER_STATUSES: OrderStatus[] = [
  'New',
  'Accepted',
  'Baking',
  'OnTheWay',
  'Delivered',
  'Cancelled',
]

export const statusLabels: Record<OrderStatus, string> = {
  New: 'Новый',
  Accepted: 'Принят',
  Baking: 'Выпекается',
  OnTheWay: 'В пути',
  Delivered: 'Доставлен',
  Cancelled: 'Отменён',
}

export const paymentLabels: Record<PaymentType, string> = {
  Now: 'Сразу',
  Debt: 'В долг',
}

export const sourceLabels: Record<string, string> = {
  Shop: 'Магазин',
  Agent: 'Агент',
  Admin: 'Админ',
}

export function orderNumber(id: string): string {
  return id.slice(0, 6).toUpperCase()
}
