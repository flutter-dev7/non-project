import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'

import { AppLayout } from '@/components/layout/app-layout'
import { LoginPage } from '@/features/auth/login-page'
import { RequireAdmin } from '@/features/auth/require-admin'
import { PlaceholderPage } from '@/pages/placeholder-page'
import { OrdersPage } from '@/features/orders/orders-page'
import { NewOrderPage } from './features/orders/new-order-page'
import { PlanPage } from './features/products/plan-page'
import { ShopsPage } from './features/shops/shops-page'
import { SettingsPage } from './features/settings/settings-page'
import { ProductsPage } from './features/products/products-page'
import { UsersPage } from './features/users/users-page'

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
            <Route path="plan" element={<PlanPage />} />
            <Route path="shops" element={<ShopsPage />} />
            <Route path="products" element={<ProductsPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="settings" element={<SettingsPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}