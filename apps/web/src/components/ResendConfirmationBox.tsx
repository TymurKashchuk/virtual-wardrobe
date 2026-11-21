import { useEffect, useState } from 'react'
import * as authApi from '../api/auth'
import { ApiError } from '../api/client'

interface ResendConfirmationBoxProps {
  fixedEmail?: string
  defaultEmail?: string
  initialCooldownSeconds?: number
}

const COOLDOWN_SECONDS = 60

function getCooldownStorageKey(email: string): string {
  return `resend_confirm_until:${email.trim().toLowerCase()}`
}

function readStoredCooldownSeconds(email: string): number {
  const normalized = email.trim().toLowerCase()
  if (!normalized) {
    return 0
  }

  try {
    const raw = window.sessionStorage.getItem(getCooldownStorageKey(normalized))
    if (!raw) {
      return 0
    }
    const deadlineMs = Number(raw)
    if (!Number.isFinite(deadlineMs)) {
      return 0
    }
    return Math.max(0, Math.ceil((deadlineMs - Date.now()) / 1000))
  } catch {
    return 0
  }
}

function writeCooldownDeadline(email: string, seconds: number): void {
  const normalized = email.trim().toLowerCase()
  if (!normalized || seconds <= 0) {
    return
  }

  try {
    const deadlineMs = Date.now() + seconds * 1000
    window.sessionStorage.setItem(
      getCooldownStorageKey(normalized),
      String(deadlineMs),
    )
  } catch {
    // Ignore storage quota or privacy-mode errors.
  }
}

export function ResendConfirmationBox({
  fixedEmail,
  defaultEmail = '',
  initialCooldownSeconds = 0,
}: ResendConfirmationBoxProps) {
  const [emailInput, setEmailInput] = useState(defaultEmail)
  const [secondsLeft, setSecondsLeft] = useState(() => {
    const initialTarget = fixedEmail ?? defaultEmail
    const stored = readStoredCooldownSeconds(initialTarget)
    if (stored > 0) {
      return stored
    }
    if (initialCooldownSeconds > 0 && initialTarget.trim()) {
      writeCooldownDeadline(initialTarget, initialCooldownSeconds)
    }
    return initialCooldownSeconds
  })
  const [isSending, setIsSending] = useState(false)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(
    null,
  )

  const hasCooldown = secondsLeft > 0

  useEffect(() => {
    if (!hasCooldown) {
      return
    }

    const interval = window.setInterval(() => {
      setSecondsLeft((prev) => (prev <= 1 ? 0 : prev - 1))
    }, 1000)

    return () => {
      window.clearInterval(interval)
    }
  }, [hasCooldown])

  const targetEmail = (fixedEmail ?? emailInput).trim()

  const handleResend = async () => {
    if (!targetEmail) {
      setFeedback({ type: 'error', text: 'Введіть email для повторного надсилання.' })
      return
    }

    setIsSending(true)
    setFeedback(null)

    try {
      await authApi.resendConfirmation(targetEmail)
      writeCooldownDeadline(targetEmail, COOLDOWN_SECONDS)
      setSecondsLeft(COOLDOWN_SECONDS)
      setFeedback({
        type: 'success',
        text: 'Якщо акаунт ще не підтверджено, новий лист надіслано.',
      })
    } catch (err) {
      if (err instanceof ApiError && err.status === 429) {
        writeCooldownDeadline(targetEmail, COOLDOWN_SECONDS)
        setSecondsLeft(COOLDOWN_SECONDS)
        setFeedback({
          type: 'error',
          text: 'Зачекайте 1 хвилину перед повторним надсиланням.',
        })
      } else {
        setFeedback({
          type: 'error',
          text: err instanceof ApiError ? err.message : 'Не вдалося надіслати лист.',
        })
      }
    } finally {
      setIsSending(false)
    }
  }

  return (
    <div className="space-y-2">
      {!fixedEmail && (
        <input
          type="email"
          value={emailInput}
          onChange={(e) => setEmailInput(e.target.value)}
          placeholder="Ваш email для повторного листа"
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 focus:border-slate-900 focus:outline-none"
        />
      )}

      <button
        type="button"
        disabled={isSending || hasCooldown}
        onClick={() => void handleResend()}
        className="w-full rounded-lg border border-slate-200 bg-white px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
      >
        {isSending
          ? 'Надсилаємо...'
          : hasCooldown
            ? `Надіслати ще раз (${secondsLeft} с)`
            : 'Надіслати лист ще раз'}
      </button>

      {feedback && (
        <p
          className={`text-xs ${
            feedback.type === 'success' ? 'text-emerald-700' : 'text-red-600'
          }`}
        >
          {feedback.text}
        </p>
      )}
    </div>
  )
}
