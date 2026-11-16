import { useState } from 'react'
import { Trash2, X } from 'lucide-react'
import { ApiError } from '../api/client'
import { getItemPhotoPath } from '../api/items'
import { CLOTHING_STYLE_LABELS, type ItemDto } from '../api/types'
import { useDeleteItem } from '../hooks/useWardrobeQueries'
import { AuthImage } from './AuthImage'

interface ItemDetailModalProps {
  item: ItemDto
  onClose: () => void
}

export function ItemDetailModal({ item, onClose }: ItemDetailModalProps) {
  const deleteItemMutation = useDeleteItem()
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleDelete = async () => {
    setError(null)
    try {
      await deleteItemMutation.mutateAsync(item.id)
      onClose()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не вдалося видалити річ.')
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-slate-900/50 p-0 sm:items-center sm:p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="detail-item-title"
    >
      <div className="max-h-[92vh] w-full max-w-md overflow-y-auto rounded-t-2xl bg-white shadow-xl sm:rounded-2xl">
        <div className="relative aspect-[4/5] w-full bg-slate-100">
          <AuthImage
            src={getItemPhotoPath(item.id)}
            alt={item.name}
            className="h-full w-full object-contain"
          />
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
              <button
                type="button"
                onClick={() => setConfirmDelete(true)}
                className="inline-flex items-center gap-1.5 rounded-xl border border-red-200 px-3 py-2 text-xs font-medium text-red-600 hover:bg-red-50"
              >
                <Trash2 className="h-3.5 w-3.5" />
                Видалити річ
              </button>
            ) : (
              <div className="flex items-center justify-between gap-2 rounded-xl bg-red-50 p-3">
                <span className="text-xs font-medium text-red-800">
                  Видалити цю річ назавжди?
                </span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => setConfirmDelete(false)}
                    className="rounded-lg bg-white px-2.5 py-1 text-xs font-medium text-slate-700 border border-slate-200"
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
        </div>
      </div>
    </div>
  )
}
