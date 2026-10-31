import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'

import { AppLayout } from '@/components/layout/app-layout'
import { LoginPage } from '@/features/auth/login-page'
import { RequireAdmin } from '@/features/auth/require-admin'
import { NewOrderPage } from '@/features/orders/new-order-page'
import { OrderPage } from '@/features/orders/order-page'
import { OrdersPage } from '@/features/orders/orders-page'
import { ProductsPage } from '@/features/products/products-page'
import { SettingsPage } from '@/features/settings/settings-page'
import { ShopsPage } from '@/features/shops/shops-page'
import { UsersPage } from '@/features/users/users-page'
import { NotFoundPage } from '@/pages/not-found-page'
import { PlanPage } from './features/products/plan-page'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<RequireAdmin />}>
          <Route element={<AppLayout />}>
            <Route index element={<Navigate to="/orders" replace />} />
            <Route path="orders" element={<OrdersPage />} />
            <Route path="orders/new" element={<NewOrderPage />} />
            <Route path="orders/:id" element={<OrderPage />} />
            <Route path="plan" element={<PlanPage />} />
            <Route path="shops" element={<ShopsPage />} />
            <Route path="products" element={<ProductsPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="settings" element={<SettingsPage />} />
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  )
}