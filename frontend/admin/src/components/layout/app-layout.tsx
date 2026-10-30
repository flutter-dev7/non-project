import {
  ClipboardList,
  LogOut,
  Menu,
  Package,
  Settings,
  Store,
  Users,
  Wheat,
} from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'

import { Button } from '@/components/ui/button'
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet'
import { useAuth } from '@/features/auth/auth-context'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/orders', label: 'Заказы', icon: ClipboardList },
  { to: '/plan', label: 'План', icon: Wheat },
  { to: '/shops', label: 'Магазины и долги', icon: Store },
  { to: '/products', label: 'Товары', icon: Package },
  { to: '/users', label: 'Пользователи', icon: Users },
  { to: '/settings', label: 'Настройки', icon: Settings },
]

function Wordmark() {
  return (
    <span
      className="text-3xl text-[#3B2A1A]"
      style={{ fontFamily: "'Playfair Display', serif", fontWeight: 900 }}
    >
      Нон
    </span>
  )
}

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const { user, logout } = useAuth()

  return (
    <div className="flex h-full flex-col">
      <div className="px-6 py-6">
        <Wordmark />
        <p className="mt-1 text-xs font-semibold uppercase tracking-wide text-stone-400">
          Администратор
        </p>
      </div>

      <nav className="flex-1 space-y-1 px-3">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground shadow-sm'
                  : 'text-stone-600 hover:bg-stone-100',
              )
            }
          >
            <Icon className="size-5" />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="border-t p-4">
        <p className="truncate text-sm font-bold text-stone-800">
          {user ? `${user.firstName} ${user.lastName}` : ''}
        </p>
        <p className="truncate text-xs text-stone-500">{user?.email}</p>
        <Button variant="outline" size="sm" className="mt-3 w-full" onClick={logout}>
          <LogOut className="size-4" />
          Выйти
        </Button>
      </div>
    </div>
  )
}

export function AppLayout() {
  const [open, setOpen] = useState(false)

  return (
    <div className="min-h-screen bg-stone-50 md:grid md:grid-cols-[260px_1fr]">
      <aside className="sticky top-0 hidden h-screen border-r bg-white md:block">
        <SidebarContent />
      </aside>

      <div className="flex min-w-0 flex-col">
        <header className="sticky top-0 z-10 flex h-14 items-center gap-3 border-b bg-white px-4 md:hidden">
          <Sheet open={open} onOpenChange={setOpen}>
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" aria-label="Меню">
                <Menu className="size-5" />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-72 p-0">
              <SheetHeader className="sr-only">
                <SheetTitle>Меню</SheetTitle>
              </SheetHeader>
              <SidebarContent onNavigate={() => setOpen(false)} />
            </SheetContent>
          </Sheet>
          <Wordmark />
        </header>

        <main className="flex-1 p-4 md:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}