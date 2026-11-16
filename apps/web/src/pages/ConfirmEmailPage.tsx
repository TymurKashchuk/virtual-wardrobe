import { useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import * as authApi from '../api/auth'
import { ApiError } from '../api/client'

export function ConfirmEmailPage() {
  const [searchParams] = useSearchParams()
  const [token, setToken] = useState(searchParams.get('token') ?? '')
  const [status, setStatus] = useState<'idle' | 'submitting' | 'success' | 'error'>('idle')
  const [message, setMessage] = useState<string | null>(null)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setStatus('submitting')
    setMessage(null)

    try {
      await authApi.confirmEmail(token.trim())
      setStatus('success')
      setMessage('Email успішно підтверджено! Тепер ви можете увійти.')
    } catch (err) {
      setStatus('error')
      setMessage(
        err instanceof ApiError
          ? err.message
          : 'Невалідний або прострочений токен підтвердження.',
      )
    }
  }

  return (
    <main className="min-h-screen bg-slate-50 flex items-center justify-center px-4 py-8">
      <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-sm border border-slate-200">
        <h1 className="text-xl font-bold text-slate-900">Підтвердження email</h1>
        <p className="mt-1 text-xs text-slate-500">
          Вставте токен із листа (у Mailpit на localhost:8025).
        </p>

        {status === 'success' ? (
          <div className="mt-5 space-y-4">
            <div className="rounded-lg bg-emerald-50 border border-emerald-200 p-3 text-sm text-emerald-800">
              {message}
            </div>
            <Link
              to="/login"
              className="block w-full text-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-800"
            >
              Перейти до входу
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="mt-5 space-y-4">
            <div>
              <label htmlFor="token" className="block text-sm font-medium text-slate-700">
                Токен підтвердження
              </label>
              <textarea
                id="token"
                rows={4}
                required
                value={token}
                onChange={(e) => setToken(e.target.value)}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-xs font-mono text-slate-900 focus:border-slate-900 focus:outline-none"
                placeholder="eyJhbGciOiJIUzI1NiIs..."
              />
            </div>

            {status === 'error' && message && (
              <div className="rounded-lg bg-red-50 border border-red-200 p-3 text-xs text-red-700">
                {message}
              </div>
            )}

            <button
              type="submit"
              disabled={status === 'submitting'}
              className="w-full rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            >
              {status === 'submitting' ? 'Перевіряємо...' : 'Підтвердити'}
            </button>
          </form>
        )}

        <p className="mt-5 text-center text-xs text-slate-500">
          <Link to="/login" className="underline text-slate-700">
            Назад до входу
          </Link>
        </p>
      </div>
    </main>
  )
}
