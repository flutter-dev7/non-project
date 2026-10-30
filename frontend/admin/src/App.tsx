import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'

import { AppLayout } from '@/components/layout/app-layout'
import { LoginPage } from '@/features/auth/login-page'
import { RequireAdmin } from '@/features/auth/require-admin'
import { PlaceholderPage } from '@/pages/placeholder-page'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<RequireAdmin />}>
          <Route element={<AppLayout />}>
            <Route index element={<Navigate to="/orders" replace />} />
            <Route path="orders" element={<PlaceholderPage title="Заказы" />} />
            <Route path="plan" element={<PlaceholderPage title="План" />} />
            <Route path="shops" element={<PlaceholderPage title="Магазины и долги" />} />
            <Route path="products" element={<PlaceholderPage title="Товары" />} />
            <Route path="users" element={<PlaceholderPage title="Пользователи" />} />
            <Route path="settings" element={<PlaceholderPage title="Настройки" />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}