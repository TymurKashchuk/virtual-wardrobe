import { useState, type ChangeEvent, type FormEvent } from 'react'
import { ArrowLeft, Camera, KeyRound, LogOut, Trash2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ApiError } from '../api/client'
import { getAvatarPath } from '../api/profile'
import { AuthImage } from '../components/AuthImage'
import { useAuth } from '../context/useAuth'
import {
  useChangePassword,
  useDeleteAccount,
  useDeleteAvatar,
  useProfile,
  useUpdateProfile,
  useUploadAvatar,
} from '../hooks/useProfileQueries'

const MAX_AVATAR_BYTES = 10 * 1024 * 1024
const ALLOWED_MIME_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])

export function ProfilePage() {
  const { logout } = useAuth()
  const { data: profile, isLoading, isError } = useProfile()

  const updateProfileMutation = useUpdateProfile()
  const uploadAvatarMutation = useUploadAvatar()
  const deleteAvatarMutation = useDeleteAvatar()
  const changePasswordMutation = useChangePassword()
  const deleteAccountMutation = useDeleteAccount()

  const [nameDraft, setNameDraft] = useState<string | null>(null)
  const [profileMessage, setProfileMessage] = useState<{
    type: 'success' | 'error'
    text: string
  } | null>(null)
  const [avatarError, setAvatarError] = useState<string | null>(null)
  const [avatarVersion, setAvatarVersion] = useState(1)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [passwordError, setPasswordError] = useState<string | null>(null)

  const [confirmDeleteAccount, setConfirmDeleteAccount] = useState(false)
  const [deleteAccountError, setDeleteAccountError] = useState<string | null>(null)

  const displayNameValue = nameDraft ?? profile?.displayName ?? ''
  const initialLetter = (profile?.displayName?.[0] ?? profile?.email?.[0] ?? '?').toUpperCase()

  const handleAvatarChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) {
      return
    }

    if (!ALLOWED_MIME_TYPES.has(file.type)) {
      setAvatarError('Підтримуються лише фото у форматі JPG, PNG або WebP.')
      return
    }

    if (file.size > MAX_AVATAR_BYTES) {
      setAvatarError('Розмір аватара не повинен перевищувати 10 МБ.')
      return
    }

    setAvatarError(null)
    try {
      await uploadAvatarMutation.mutateAsync(file)
      setAvatarVersion((v) => v + 1)
    } catch (err) {
      setAvatarError(
        err instanceof ApiError ? err.message : 'Не вдалося оновити аватар.',
      )
    }
  }

  const handleDeleteAvatar = async () => {
    setAvatarError(null)
    try {
      await deleteAvatarMutation.mutateAsync()
    } catch (err) {
      setAvatarError(
        err instanceof ApiError ? err.message : 'Не вдалося видалити аватар.',
      )
    }
  }

  const handleProfileSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setProfileMessage(null)

    try {
      await updateProfileMutation.mutateAsync({
        displayName: displayNameValue.trim(),
      })
      setNameDraft(null)
      setProfileMessage({ type: 'success', text: 'Профіль оновлено.' })
    } catch (err) {
      setProfileMessage({
        type: 'error',
        text: err instanceof ApiError ? err.message : 'Не вдалося оновити профіль.',
      })
    }
  }

  const handlePasswordSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setPasswordError(null)

    try {
      await changePasswordMutation.mutateAsync({
        currentPassword: profile?.hasPassword ? currentPassword : undefined,
        newPassword,
      })
    } catch (err) {
      setPasswordError(
        err instanceof ApiError ? err.message : 'Не вдалося змінити пароль.',
      )
    }
  }

  const handleDeleteAccount = async () => {
    setDeleteAccountError(null)
    try {
      await deleteAccountMutation.mutateAsync()
    } catch (err) {
      setDeleteAccountError(
        err instanceof ApiError ? err.message : 'Не вдалося видалити акаунт.',
      )
    }
  }

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-2xl items-center justify-between px-4 py-3">
          <Link
            to="/wardrobe"
            className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-700 hover:text-slate-900"
          >
            <ArrowLeft className="h-4 w-4" />
            До шафи
          </Link>

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

      <main className="mx-auto max-w-2xl space-y-5 px-4 py-6">
        <h1 className="text-xl font-bold tracking-tight">Профіль</h1>

        {isLoading && (
          <div className="h-48 animate-pulse rounded-2xl bg-slate-200" />
        )}

        {isError && (
          <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            Не вдалося завантажити дані профілю.
          </div>
        )}

        {profile && (
          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-xs space-y-5">
            {/* Avatar block */}
            <div className="flex flex-wrap items-center gap-4">
              <div className="h-20 w-20 overflow-hidden rounded-full border border-slate-200 bg-slate-100 flex items-center justify-center shrink-0">
                {profile.hasAvatar ? (
                  <AuthImage
                    src={getAvatarPath(avatarVersion)}
                    alt={profile.displayName}
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <span className="text-2xl font-bold text-slate-500">
                    {initialLetter}
                  </span>
                )}
              </div>

              <div className="space-y-2">
                <div className="flex flex-wrap gap-2">
                  <label className="inline-flex cursor-pointer items-center gap-1.5 rounded-xl bg-slate-900 px-3.5 py-2 text-xs font-medium text-white hover:bg-slate-800">
                    <Camera className="h-3.5 w-3.5" />
                    {uploadAvatarMutation.isPending
                      ? 'Завантаження...'
                      : profile.hasAvatar
                        ? 'Змінити фото'
                        : 'Додати фото'}
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      disabled={uploadAvatarMutation.isPending}
                      onChange={(e) => void handleAvatarChange(e)}
                      className="sr-only"
                    />
                  </label>

                  {profile.hasAvatar && (
                    <button
                      type="button"
                      disabled={deleteAvatarMutation.isPending}
                      onClick={() => void handleDeleteAvatar()}
                      className="inline-flex items-center gap-1.5 rounded-xl border border-slate-200 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-50"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                      Видалити
                    </button>
                  )}
                </div>
                <p className="text-[11px] text-slate-500">
                  JPG, PNG або WebP до 10 МБ
                </p>
                {avatarError && (
                  <p className="text-xs text-red-600">{avatarError}</p>
                )}
              </div>
            </div>

            {/* Profile info & displayName form */}
            <form onSubmit={handleProfileSubmit} className="space-y-4 border-t border-slate-100 pt-4">
              <div>
                <label className="block text-xs font-medium text-slate-500">
                  Email
                </label>
                <p className="mt-1 text-sm font-medium text-slate-800">
                  {profile.email}
                </p>
              </div>

              <div>
                <label htmlFor="profile-display-name" className="block text-xs font-medium text-slate-700">
                  Ім'я
                </label>
                <input
                  id="profile-display-name"
                  type="text"
                  required
                  minLength={2}
                  maxLength={50}
                  value={displayNameValue}
                  onChange={(e) => setNameDraft(e.target.value)}
                  className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
                />
              </div>

              {profileMessage && (
                <p
                  className={`text-xs ${
                    profileMessage.type === 'success'
                      ? 'text-emerald-700'
                      : 'text-red-600'
                  }`}
                >
                  {profileMessage.text}
                </p>
              )}

              <button
                type="submit"
                disabled={updateProfileMutation.isPending}
                className="rounded-xl bg-slate-900 px-4 py-2 text-xs font-medium text-white hover:bg-slate-800 disabled:opacity-50"
              >
                {updateProfileMutation.isPending ? 'Зберігаємо...' : 'Зберегти зміни'}
              </button>
            </form>
          </section>
        )}

        {profile && (
          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-xs space-y-4">
            <div className="flex items-center gap-2">
              <KeyRound className="h-4 w-4 text-slate-500" />
              <h2 className="text-sm font-bold text-slate-900">
                {profile.hasPassword ? 'Зміна пароля' : 'Встановлення пароля'}
              </h2>
            </div>

            {!profile.hasPassword && (
              <p className="text-xs text-slate-500">
                Ви зареєструвалися через Google. Задайте пароль, щоб також входити за email.
              </p>
            )}

            <form onSubmit={handlePasswordSubmit} className="space-y-3">
              {profile.hasPassword && (
                <div>
                  <label htmlFor="current-password" className="block text-xs font-medium text-slate-700">
                    Поточний пароль
                  </label>
                  <input
                    id="current-password"
                    type="password"
                    required
                    autoComplete="current-password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
                  />
                </div>
              )}

              <div>
                <label htmlFor="new-password" className="block text-xs font-medium text-slate-700">
                  Новий пароль
                </label>
                <input
                  id="new-password"
                  type="password"
                  required
                  minLength={8}
                  autoComplete="new-password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="Мін. 8 символів, літери та цифри"
                  className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
                />
              </div>

              {passwordError && (
                <p className="text-xs text-red-600">{passwordError}</p>
              )}

              <p className="text-[11px] text-slate-500">
                Після зміни пароля всі активні сесії буде завершено.
              </p>

              <button
                type="submit"
                disabled={changePasswordMutation.isPending}
                className="rounded-xl bg-slate-900 px-4 py-2 text-xs font-medium text-white hover:bg-slate-800 disabled:opacity-50"
              >
                {changePasswordMutation.isPending
                  ? 'Оновлюємо...'
                  : profile.hasPassword
                    ? 'Змінити пароль'
                    : 'Встановити пароль'}
              </button>
            </form>
          </section>
        )}

        {profile && (
          <section className="rounded-2xl border border-red-200 bg-white p-5 shadow-xs space-y-3">
            <h2 className="text-sm font-bold text-red-700">Видалення акаунта</h2>
            <p className="text-xs text-slate-600">
              Акаунт, усі речі, образи та завантажені фото будуть видалені безповоротно.
            </p>

            {deleteAccountError && (
              <p className="text-xs text-red-600">{deleteAccountError}</p>
            )}

            {!confirmDeleteAccount ? (
              <button
                type="button"
                onClick={() => setConfirmDeleteAccount(true)}
                className="inline-flex items-center gap-1.5 rounded-xl border border-red-200 px-3.5 py-2 text-xs font-medium text-red-600 hover:bg-red-50"
              >
                <Trash2 className="h-3.5 w-3.5" />
                Видалити акаунт
              </button>
            ) : (
              <div className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-red-50 p-3">
                <span className="text-xs font-medium text-red-800">
                  Ви впевнені? Цю дію не можна скасувати.
                </span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => setConfirmDeleteAccount(false)}
                    className="rounded-lg border border-slate-200 bg-white px-3 py-1 text-xs font-medium text-slate-700"
                  >
                    Скасувати
                  </button>
                  <button
                    type="button"
                    disabled={deleteAccountMutation.isPending}
                    onClick={() => void handleDeleteAccount()}
                    className="rounded-lg bg-red-600 px-3 py-1 text-xs font-medium text-white hover:bg-red-700 disabled:opacity-50"
                  >
                    {deleteAccountMutation.isPending ? 'Видалення...' : 'Підтвердити'}
                  </button>
                </div>
              </div>
            )}
          </section>
        )}
      </main>
    </div>
  )
}
