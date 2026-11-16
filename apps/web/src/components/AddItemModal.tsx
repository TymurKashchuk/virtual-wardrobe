import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { ImagePlus, X } from 'lucide-react'
import { ApiError } from '../api/client'
import {
  CLOTHING_STYLE_LABELS,
  ClothingStyle,
  type CategoryDto,
  type ColorDto,
} from '../api/types'
import { useCreateItem } from '../hooks/useWardrobeQueries'

const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024
const ALLOWED_MIME_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])

interface AddItemModalProps {
  categories: CategoryDto[]
  colors: ColorDto[]
  onClose: () => void
}

export function AddItemModal({ categories, colors, onClose }: AddItemModalProps) {
  const createItemMutation = useCreateItem()

  const [name, setName] = useState('')
  const [categoryId, setCategoryId] = useState<number>(categories[0]?.id ?? 0)
  const [primaryColorId, setPrimaryColorId] = useState<number>(colors[0]?.id ?? 0)
  const [secondaryColorIds, setSecondaryColorIds] = useState<number[]>([])
  const [style, setStyle] = useState<ClothingStyle>(ClothingStyle.Casual)
  const [notes, setNotes] = useState('')
  const [photo, setPhoto] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl)
      }
    }
  }, [previewUrl])

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) {
      return
    }

    if (!ALLOWED_MIME_TYPES.has(file.type)) {
      setValidationError('Підтримуються лише фото у форматі JPG, PNG або WebP.')
      return
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setValidationError('Розмір фото не повинен перевищувати 10 МБ.')
      return
    }

    setValidationError(null)
    setPhoto(file)
    setPreviewUrl(URL.createObjectURL(file))
  }

  const handlePrimaryColorSelect = (id: number) => {
    setPrimaryColorId(id)
    setSecondaryColorIds((prev) => prev.filter((c) => c !== id))
  }

  const toggleSecondaryColor = (id: number) => {
    if (id === primaryColorId) {
      return
    }
    setSecondaryColorIds((prev) => {
      if (prev.includes(id)) {
        return prev.filter((c) => c !== id)
      }
      if (prev.length >= 2) {
        return prev
      }
      return [...prev, id]
    })
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setValidationError(null)

    if (!photo) {
      setValidationError('Будь ласка, додайте фото речі.')
      return
    }
    if (!categoryId || !primaryColorId) {
      setValidationError('Оберіть категорію та основний колір.')
      return
    }

    try {
      await createItemMutation.mutateAsync({
        name: name.trim(),
        categoryId,
        primaryColorId,
        secondaryColorIds,
        style,
        notes: notes.trim() || undefined,
        photo,
      })
      onClose()
    } catch (err) {
      setValidationError(
        err instanceof ApiError ? err.message : 'Не вдалося зберегти річ.',
      )
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-slate-900/50 p-0 sm:items-center sm:p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="add-item-title"
    >
      <div className="max-h-[92vh] w-full max-w-lg overflow-y-auto rounded-t-2xl bg-white p-5 shadow-xl sm:rounded-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 pb-3">
          <h2 id="add-item-title" className="text-base font-bold text-slate-900">
            Додати річ
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Закрити"
            className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          {/* Photo upload */}
          <div>
            <label className="block text-xs font-medium text-slate-700">
              Фото (JPG, PNG, WebP до 10 МБ) *
            </label>
            <label className="mt-1.5 flex cursor-pointer flex-col items-center justify-center overflow-hidden rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 hover:border-slate-400">
              {previewUrl ? (
                <img
                  src={previewUrl}
                  alt="Попередній перегляд"
                  className="max-h-48 rounded-lg object-contain"
                />
              ) : (
                <div className="flex flex-col items-center py-4 text-slate-500">
                  <ImagePlus className="h-7 w-7 text-slate-400" />
                  <span className="mt-1.5 text-xs">Натисніть, щоб обрати фото</span>
                </div>
              )}
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={handleFileChange}
                className="sr-only"
              />
            </label>
          </div>

          {/* Name */}
          <div>
            <label htmlFor="item-name" className="block text-xs font-medium text-slate-700">
              Назва *
            </label>
            <input
              id="item-name"
              type="text"
              required
              minLength={1}
              maxLength={100}
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Наприклад, Біла базова футболка"
              className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
            />
          </div>

          {/* Category + Style */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div>
              <label htmlFor="item-category" className="block text-xs font-medium text-slate-700">
                Категорія *
              </label>
              <select
                id="item-category"
                value={categoryId}
                onChange={(e) => setCategoryId(Number(e.target.value))}
                className="mt-1 w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
              >
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.nameUk}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label htmlFor="item-style" className="block text-xs font-medium text-slate-700">
                Стиль *
              </label>
              <select
                id="item-style"
                value={style}
                onChange={(e) => setStyle(Number(e.target.value) as ClothingStyle)}
                className="mt-1 w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
              >
                {Object.entries(CLOTHING_STYLE_LABELS).map(([val, label]) => (
                  <option key={val} value={val}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Primary color */}
          <div>
            <span className="block text-xs font-medium text-slate-700">
              Основний колір *
            </span>
            <div className="mt-1.5 flex flex-wrap gap-1.5">
              {colors.map((color) => {
                const selected = color.id === primaryColorId
                return (
                  <button
                    key={color.id}
                    type="button"
                    onClick={() => handlePrimaryColorSelect(color.id)}
                    className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs transition ${
                      selected
                        ? 'bg-slate-900 text-white'
                        : 'border border-slate-200 bg-white text-slate-700 hover:bg-slate-100'
                    }`}
                  >
                    <span
                      className="h-3 w-3 rounded-full border border-slate-300"
                      style={{ backgroundColor: color.hex }}
                    />
                    {color.nameUk}
                  </button>
                )
              })}
            </div>
          </div>

          {/* Secondary colors */}
          <div>
            <span className="block text-xs font-medium text-slate-700">
              Додаткові кольори (до 2)
            </span>
            <div className="mt-1.5 flex flex-wrap gap-1.5">
              {colors
                .filter((color) => color.id !== primaryColorId)
                .map((color) => {
                  const selected = secondaryColorIds.includes(color.id)
                  const disabled = !selected && secondaryColorIds.length >= 2
                  return (
                    <button
                      key={color.id}
                      type="button"
                      disabled={disabled}
                      onClick={() => toggleSecondaryColor(color.id)}
                      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs transition disabled:opacity-40 ${
                        selected
                          ? 'bg-slate-900 text-white'
                          : 'border border-slate-200 bg-white text-slate-700 hover:bg-slate-100'
                      }`}
                    >
                      <span
                        className="h-3 w-3 rounded-full border border-slate-300"
                        style={{ backgroundColor: color.hex }}
                      />
                      {color.nameUk}
                    </button>
                  )
                })}
            </div>
          </div>

          {/* Notes */}
          <div>
            <label htmlFor="item-notes" className="block text-xs font-medium text-slate-700">
              Нотатка (необов'язково)
            </label>
            <textarea
              id="item-notes"
              rows={2}
              maxLength={1000}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Матеріал, бренд або сезон..."
              className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
            />
          </div>

          {validationError && (
            <div className="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700">
              {validationError}
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-xl border border-slate-200 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-100"
            >
              Скасувати
            </button>
            <button
              type="submit"
              disabled={createItemMutation.isPending}
              className="rounded-xl bg-slate-900 px-4 py-2 text-xs font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            >
              {createItemMutation.isPending ? 'Зберігаємо...' : 'Зберегти річ'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
