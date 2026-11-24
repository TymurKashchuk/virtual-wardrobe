import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { ImagePlus, Pencil, Trash2, X } from 'lucide-react'
import { ApiError } from '../api/client'
import { getItemPhotoPath } from '../api/items'
import {
  CLOTHING_STYLE_LABELS,
  ClothingStyle,
  type CategoryDto,
  type ColorDto,
  type ItemDto,
} from '../api/types'
import { useDeleteItem, useUpdateItem } from '../hooks/useWardrobeQueries'
import { AuthImage } from './AuthImage'

const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024
const ALLOWED_MIME_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])

interface ItemDetailModalProps {
  item: ItemDto
  categories: CategoryDto[]
  colors: ColorDto[]
  onUpdated: (updated: ItemDto) => void
  onClose: () => void
}

export function ItemDetailModal({
  item,
  categories,
  colors,
  onUpdated,
  onClose,
}: ItemDetailModalProps) {
  const deleteItemMutation = useDeleteItem()
  const updateItemMutation = useUpdateItem()

  const [isEditing, setIsEditing] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [photoVersion, setPhotoVersion] = useState(1)

  const initialPrimaryColorId =
    item.colors.find((c) => c.isPrimary)?.colorId ?? item.colors[0]?.colorId ?? 0
  const initialSecondaryColorIds = item.colors
    .filter((c) => !c.isPrimary)
    .map((c) => c.colorId)

  const [name, setName] = useState(item.name)
  const [categoryId, setCategoryId] = useState<number>(item.categoryId)
  const [primaryColorId, setPrimaryColorId] = useState<number>(initialPrimaryColorId)
  const [secondaryColorIds, setSecondaryColorIds] = useState<number[]>(
    initialSecondaryColorIds,
  )
  const [style, setStyle] = useState<ClothingStyle>(item.style)
  const [notes, setNotes] = useState(item.notes ?? '')
  const [newPhoto, setNewPhoto] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl)
      }
    }
  }, [previewUrl])

  const resetEditForm = () => {
    setName(item.name)
    setCategoryId(item.categoryId)
    setPrimaryColorId(initialPrimaryColorId)
    setSecondaryColorIds(initialSecondaryColorIds)
    setStyle(item.style)
    setNotes(item.notes ?? '')
    setNewPhoto(null)
    setPreviewUrl(null)
    setError(null)
  }

  const startEditing = () => {
    resetEditForm()
    setConfirmDelete(false)
    setIsEditing(true)
  }

  const cancelEditing = () => {
    resetEditForm()
    setIsEditing(false)
  }

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) {
      return
    }

    if (!ALLOWED_MIME_TYPES.has(file.type)) {
      setError('Підтримуються лише фото у форматі JPG, PNG або WebP.')
      return
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setError('Розмір фото не повинен перевищувати 10 МБ.')
      return
    }

    setError(null)
    setNewPhoto(file)
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

  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    if (!name.trim()) {
      setError('Введіть назву речі.')
      return
    }
    if (!categoryId || !primaryColorId) {
      setError('Оберіть категорію та основний колір.')
      return
    }

    try {
      const updated = await updateItemMutation.mutateAsync({
        id: item.id,
        payload: {
          name: name.trim(),
          categoryId,
          primaryColorId,
          secondaryColorIds,
          style,
          notes: notes.trim() || undefined,
          newPhoto: newPhoto ?? undefined,
        },
      })
      if (newPhoto) {
        setPhotoVersion((v) => v + 1)
      }
      setNewPhoto(null)
      setPreviewUrl(null)
      setIsEditing(false)
      onUpdated(updated)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не вдалося оновити річ.')
    }
  }

  const handleDelete = async () => {
    setError(null)
    try {
      await deleteItemMutation.mutateAsync(item.id)
      onClose()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не вдалося видалити річ.')
    }
  }

  const photoSrc =
    photoVersion > 1
      ? `${getItemPhotoPath(item.id)}?v=${photoVersion}`
      : getItemPhotoPath(item.id)

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-slate-900/50 p-0 sm:items-center sm:p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="detail-item-title"
    >
      <div className="max-h-[92vh] w-full max-w-md overflow-y-auto rounded-t-2xl bg-white shadow-xl sm:rounded-2xl">
        <div className="relative aspect-[4/5] w-full bg-slate-100">
          {isEditing && previewUrl ? (
            <img
              src={previewUrl}
              alt="Нове фото"
              className="h-full w-full object-contain"
            />
          ) : (
            <AuthImage
              src={photoSrc}
              alt={item.name}
              className="h-full w-full object-contain"
            />
          )}
          <button
            type="button"
            onClick={onClose}
            aria-label="Закрити"
            className="absolute right-3 top-3 rounded-full bg-white/90 p-1.5 text-slate-700 shadow-sm hover:bg-white"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="space-y-4 p-5">
          {!isEditing ? (
            <>
              <div>
                <h2 id="detail-item-title" className="text-lg font-bold text-slate-900">
                  {item.name}
                </h2>
                <p className="mt-0.5 text-xs text-slate-500">
                  {item.categoryNameUk} · {CLOTHING_STYLE_LABELS[item.style]}
                </p>
              </div>

              <div className="flex flex-wrap gap-1.5">
                {item.colors.map((color) => (
                  <span
                    key={color.colorId}
                    className="inline-flex items-center gap-1.5 rounded-full border border-slate-200 px-2.5 py-1 text-xs text-slate-700"
                  >
                    <span
                      className="h-3 w-3 rounded-full border border-slate-300"
                      style={{ backgroundColor: color.hex }}
                    />
                    {color.nameUk}
                    {color.isPrimary && (
                      <span className="text-[10px] font-medium text-slate-400">
                        основний
                      </span>
                    )}
                  </span>
                ))}
              </div>

              {item.notes && (
                <p className="rounded-xl bg-slate-50 p-3 text-xs text-slate-600">
                  {item.notes}
                </p>
              )}

              {error && (
                <div className="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700">
                  {error}
                </div>
              )}

              <div className="border-t border-slate-100 pt-3">
                {!confirmDelete ? (
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <button
                      type="button"
                      onClick={startEditing}
                      className="inline-flex items-center gap-1.5 rounded-xl border border-slate-200 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                      Редагувати
                    </button>

                    <button
                      type="button"
                      onClick={() => setConfirmDelete(true)}
                      className="inline-flex items-center gap-1.5 rounded-xl border border-red-200 px-3 py-2 text-xs font-medium text-red-600 hover:bg-red-50"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                      Видалити річ
                    </button>
                  </div>
                ) : (
                  <div className="flex items-center justify-between gap-2 rounded-xl bg-red-50 p-3">
                    <span className="text-xs font-medium text-red-800">
                      Видалити цю річ назавжди?
                    </span>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => setConfirmDelete(false)}
                        className="rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700"
                      >
                        Ні
                      </button>
                      <button
                        type="button"
                        disabled={deleteItemMutation.isPending}
                        onClick={() => void handleDelete()}
                        className="rounded-lg bg-red-600 px-3 py-1 text-xs font-medium text-white hover:bg-red-700 disabled:opacity-50"
                      >
                        {deleteItemMutation.isPending ? 'Видалення...' : 'Так'}
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </>
          ) : (
            <form onSubmit={handleSave} className="space-y-4">
              <div className="flex items-center justify-between">
                <h2 id="detail-item-title" className="text-base font-bold text-slate-900">
                  Редагувати річ
                </h2>
                <label className="inline-flex cursor-pointer items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50">
                  <ImagePlus className="h-3.5 w-3.5" />
                  {newPhoto ? 'Інше фото' : 'Замінити фото'}
                  <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    onChange={handleFileChange}
                    className="sr-only"
                  />
                </label>
              </div>

              <div>
                <label htmlFor="edit-name" className="block text-xs font-medium text-slate-700">
                  Назва *
                </label>
                <input
                  id="edit-name"
                  type="text"
                  required
                  maxLength={120}
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
                />
              </div>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="edit-category"
                    className="block text-xs font-medium text-slate-700"
                  >
                    Категорія *
                  </label>
                  <select
                    id="edit-category"
                    value={categoryId}
                    onChange={(e) => setCategoryId(Number(e.target.value))}
                    className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
                  >
                    {categories.map((cat) => (
                      <option key={cat.id} value={cat.id}>
                        {cat.nameUk}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label htmlFor="edit-style" className="block text-xs font-medium text-slate-700">
                    Стиль *
                  </label>
                  <select
                    id="edit-style"
                    value={style}
                    onChange={(e) => setStyle(Number(e.target.value) as ClothingStyle)}
                    className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
                  >
                    {Object.entries(CLOTHING_STYLE_LABELS).map(([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

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
                            : 'border border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
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

              <div>
                <span className="block text-xs font-medium text-slate-700">
                  Додаткові кольори (до 2)
                </span>
                <div className="mt-1.5 flex flex-wrap gap-1.5">
                  {colors
                    .filter((color) => color.id !== primaryColorId)
                    .map((color) => {
                      const selected = secondaryColorIds.includes(color.id)
                      return (
                        <button
                          key={color.id}
                          type="button"
                          onClick={() => toggleSecondaryColor(color.id)}
                          className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs transition ${
                            selected
                              ? 'bg-slate-700 text-white'
                              : 'border border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
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

              <div>
                <label htmlFor="edit-notes" className="block text-xs font-medium text-slate-700">
                  Нотатки
                </label>
                <textarea
                  id="edit-notes"
                  rows={2}
                  maxLength={500}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-slate-900 focus:outline-none"
                  placeholder="Наприклад: улюблена базова річ"
                />
              </div>

              {error && (
                <div className="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700">
                  {error}
                </div>
              )}

              <div className="flex gap-2 border-t border-slate-100 pt-3">
                <button
                  type="button"
                  onClick={cancelEditing}
                  className="flex-1 rounded-xl border border-slate-200 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50"
                >
                  Скасувати
                </button>
                <button
                  type="submit"
                  disabled={updateItemMutation.isPending}
                  className="flex-1 rounded-xl bg-slate-900 px-4 py-2 text-xs font-medium text-white hover:bg-slate-800 disabled:opacity-50"
                >
                  {updateItemMutation.isPending ? 'Збереження...' : 'Зберегти'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
