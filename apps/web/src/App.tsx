import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { GuestOnlyRoute, ProtectedRoute } from './components/RouteGuards'
import { AuthProvider } from './context/AuthContext'
import { ConfirmEmailPage } from './pages/ConfirmEmailPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { WardrobePage } from './pages/WardrobePage'

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<GuestOnlyRoute />}>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/confirm-email" element={<ConfirmEmailPage />} />
          </Route>

          <Route element={<ProtectedRoute />}>
            <Route path="/wardrobe" element={<WardrobePage />} />
          </Route>

          <Route path="*" element={<Navigate to="/wardrobe" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
