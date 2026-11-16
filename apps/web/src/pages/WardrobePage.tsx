import { LogOut } from 'lucide-react'
import { useAuth } from '../context/useAuth'

export function WardrobePage() {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <div>
            <h1 className="text-lg font-bold tracking-tight">Моя шафа</h1>
            {user && <p className="text-xs text-slate-500">{user.displayName}</p>}
          </div>
          <button
            type="button"
            onClick={() => void logout()}
            className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-100"
          >
            <LogOut className="h-3.5 w-3.5" />
            Вийти
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 py-6">
        <p className="text-sm text-slate-500">
          Сітка речей та фільтри будуть додані в наступному кроці.
        </p>
      </main>
    </div>
  )
}
