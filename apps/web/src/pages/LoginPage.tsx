import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../context/useAuth'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [needsConfirmation, setNeedsConfirmation] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setNeedsConfirmation(false)
    setIsSubmitting(true)

    try {
      await login({ email, password })
      navigate('/wardrobe', { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.code === 'email_not_confirmed') {
          setNeedsConfirmation(true)
          setError('Email ще не підтверджено. Перевірте пошту та підтвердьте акаунт.')
        } else {
          setError(err.message)
        }
      } else {
        setError('Не вдалося виконати вхід. Спробуйте ще раз.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="min-h-screen bg-slate-50 flex items-center justify-center px-4 py-8">
      <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-sm border border-slate-200">
        <h1 className="text-xl font-bold text-slate-900">Вхід у шафу</h1>
        <p className="mt-1 text-sm text-slate-500">Virtual Wardrobe</p>

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-slate-700">
              Email
            </label>
            <input
              id="email"
              type="email"
              required
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
              placeholder="name@example.com"
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-slate-700">
              Пароль
            </label>
            <input
              id="password"
              type="password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
              placeholder="••••••••"
            />
          </div>

          {error && (
            <div className="rounded-lg bg-red-50 border border-red-200 p-3 text-xs text-red-700">
              <p>{error}</p>
              {needsConfirmation && (
                <Link
                  to="/confirm-email"
                  state={{ email }}
                  className="mt-1.5 inline-block font-semibold underline hover:text-red-900"
                >
                  Перейти до підтвердження email
                </Link>
              )}
            </div>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {isSubmitting ? 'Входимо...' : 'Увійти'}
          </button>
        </form>

        <p className="mt-5 text-center text-xs text-slate-500">
          Ще немає акаунту?{' '}
          <Link to="/register" className="font-medium text-slate-900 underline">
            Зареєструватися
          </Link>
        </p>
      </div>
    </main>
  )
}
