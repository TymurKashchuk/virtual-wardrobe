import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import * as authApi from '../api/auth'
import { ApiError } from '../api/client'

export function RegisterPage() {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const result = await authApi.register({ displayName, email, password })
      setRegisteredEmail(result.email)
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Не вдалося завершити реєстрацію.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  if (registeredEmail) {
    return (
      <main className="min-h-screen bg-slate-50 flex items-center justify-center px-4 py-8">
        <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-sm border border-slate-200 text-center">
          <h1 className="text-xl font-bold text-slate-900">Перевірте пошту</h1>
          <p className="mt-2 text-sm text-slate-600">
            Ми надіслали лист із токеном підтвердження на{' '}
            <span className="font-medium text-slate-900">{registeredEmail}</span>.
          </p>
          <p className="mt-2 text-xs text-slate-500">
            Локально листи можна переглянути в{' '}
            <a
              href="http://localhost:8025"
              target="_blank"
              rel="noreferrer"
              className="underline text-slate-700"
            >
              Mailpit (localhost:8025)
            </a>
            .
          </p>
          <Link
            to="/confirm-email"
            className="mt-5 inline-block w-full rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-800"
          >
            Ввести токен підтвердження
          </Link>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-slate-50 flex items-center justify-center px-4 py-8">
      <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-sm border border-slate-200">
        <h1 className="text-xl font-bold text-slate-900">Реєстрація</h1>
        <p className="mt-1 text-sm text-slate-500">Створіть цифрову шафу</p>

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label htmlFor="displayName" className="block text-sm font-medium text-slate-700">
              Ім'я
            </label>
            <input
              id="displayName"
              type="text"
              required
              minLength={2}
              maxLength={50}
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
              placeholder="Олена"
            />
          </div>

          <div>
            <label htmlFor="reg-email" className="block text-sm font-medium text-slate-700">
              Email
            </label>
            <input
              id="reg-email"
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
            <label htmlFor="reg-password" className="block text-sm font-medium text-slate-700">
              Пароль
            </label>
            <input
              id="reg-password"
              type="password"
              required
              minLength={8}
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
              placeholder="Мін. 8 символів, літери та цифри"
            />
          </div>

          {error && (
            <div className="rounded-lg bg-red-50 border border-red-200 p-3 text-xs text-red-700">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {isSubmitting ? 'Реєструємо...' : 'Зареєструватися'}
          </button>
        </form>

        <p className="mt-5 text-center text-xs text-slate-500">
          Вже є акаунт?{' '}
          <Link to="/login" className="font-medium text-slate-900 underline">
            Увійти
          </Link>
        </p>
      </div>
    </main>
  )
}
