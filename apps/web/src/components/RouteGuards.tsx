import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../context/useAuth'

function FullScreenSpinner() {
  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center">
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-slate-300 border-t-slate-900" />
    </div>
  )
}

export function ProtectedRoute() {
  const { user, isInitializing } = useAuth()

  if (isInitializing) {
    return <FullScreenSpinner />
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

export function GuestOnlyRoute() {
  const { user, isInitializing } = useAuth()

  if (isInitializing) {
    return <FullScreenSpinner />
  }

  if (user) {
    return <Navigate to="/wardrobe" replace />
  }

  return <Outlet />
}
