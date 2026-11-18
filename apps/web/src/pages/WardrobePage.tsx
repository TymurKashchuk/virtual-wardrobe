import { useMemo, useState } from 'react'
import { LogOut, Plus, RotateCcw, Search, Shirt, User } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { ItemDto } from '../api/types'
import { AddItemModal } from '../components/AddItemModal'
import { ItemCard } from '../components/ItemCard'
import { ItemDetailModal } from '../components/ItemDetailModal'
import { useAuth } from '../context/useAuth'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import {
  useCategories,
  useColors,
  useInfiniteItems,
} from '../hooks/useWardrobeQueries'

function toggleId(list: number[], id: number): number[] {
  return list.includes(id) ? list.filter((item) => item !== id) : [...list, id]
}

export function WardrobePage() {
  const { user, logout } = useAuth()

  const [selectedCategories, setSelectedCategories] = useState<number[]>([])
  const [selectedColors, setSelectedColors] = useState<number[]>([])
  const [searchInput, setSearchInput] = useState('')
  const [sortBy, setSortBy] = useState<'newest' | 'name'>('newest')
  const [isAddOpen, setIsAddOpen] = useState(false)
  const [selectedItem, setSelectedItem] = useState<ItemDto | null>(null)

  const debouncedSearch = useDebouncedValue(searchInput, 300)

  const { data: categories = [] } = useCategories()
  const { data: colors = [] } = useColors()

  const filters = useMemo(
    () => ({
      categoryIds: selectedCategories,
      colorIds: selectedColors,
      search: debouncedSearch,
      sortBy,
    }),
    [selectedCategories, selectedColors, debouncedSearch, sortBy],
  )

  const {
    data,
    isLoading,
    isError,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
  } = useInfiniteItems(filters)

  const items = useMemo(
    () => data?.pages.flatMap((page) => page.items) ?? [],
    [data],
  )
  const totalCount = data?.pages[0]?.totalCount ?? 0

  const hasActiveFilters =
    selectedCategories.length > 0 ||
    selectedColors.length > 0 ||
    debouncedSearch.trim().length > 0

  const resetFilters = () => {
    setSelectedCategories([])
    setSelectedColors([])
    setSearchInput('')
  }

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <div>
            <h1 className="text-lg font-bold tracking-tight">Моя шафа</h1>
            {user && (
              <p className="text-xs text-slate-500">
                {user.displayName} · {totalCount} речей
              </p>
            )}
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => setIsAddOpen(true)}
              className="inline-flex items-center gap-1.5 rounded-lg bg-slate-900 px-3 py-1.5 text-xs font-medium text-white hover:bg-slate-800"
            >
              <Plus className="h-3.5 w-3.5" />
              Додати річ
            </button>
            <Link
              to="/profile"
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-100"
            >
              <User className="h-3.5 w-3.5" />
              Профіль
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
        </div>
      </header>

      <main className="mx-auto max-w-5xl space-y-4 px-4 py-5">
        {/* Search and sort bar */}
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              type="search"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Пошук за назвою..."
              className="w-full rounded-xl border border-slate-200 bg-white py-2 pl-9 pr-3 text-sm text-slate-900 placeholder:text-slate-400 focus:border-slate-900 focus:outline-none"
            />
          </div>

          <select
            aria-label="Сортування"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as 'newest' | 'name')}
            className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-slate-900 focus:outline-none"
          >
            <option value="newest">Нові спочатку</option>
            <option value="name">За назвою (А–Я)</option>
          </select>
        </div>

        {/* Category chips */}
        {categories.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            {categories.map((category) => {
              const active = selectedCategories.includes(category.id)
              return (
                <button
                  key={category.id}
                  type="button"
                  onClick={() =>
                    setSelectedCategories((prev) => toggleId(prev, category.id))
                  }
                  className={`rounded-full px-3 py-1 text-xs font-medium transition ${
                    active
                      ? 'bg-slate-900 text-white'
                      : 'border border-slate-200 bg-white text-slate-700 hover:bg-slate-100'
                  }`}
                >
                  {category.nameUk}
                </button>
              )
            })}
          </div>
        )}

        {/* Color chips */}
        {colors.length > 0 && (
          <div className="flex flex-wrap items-center gap-1.5">
            {colors.map((color) => {
              const active = selectedColors.includes(color.id)
              return (
                <button
                  key={color.id}
                  type="button"
                  onClick={() =>
                    setSelectedColors((prev) => toggleId(prev, color.id))
                  }
                  className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs transition ${
                    active
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

            {hasActiveFilters && (
              <button
                type="button"
                onClick={resetFilters}
                className="inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium text-slate-600 hover:bg-slate-200/70"
              >
                <RotateCcw className="h-3 w-3" />
                Скинути
              </button>
            )}
          </div>
        )}

        {/* Content states */}
        {isLoading && (
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
            {Array.from({ length: 8 }).map((_, idx) => (
              <div
                key={idx}
                className="aspect-[3/4] animate-pulse rounded-2xl bg-slate-200"
              />
            ))}
          </div>
        )}

        {isError && (
          <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            Не вдалося завантажити речі. Перевірте підключення до API.
          </div>
        )}

        {!isLoading && !isError && items.length === 0 && (
          <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-14 text-center">
            <Shirt className="h-10 w-10 text-slate-300" />
            {hasActiveFilters ? (
              <>
                <p className="mt-3 text-sm font-medium text-slate-900">
                  За обраними фільтрами нічого не знайдено
                </p>
                <button
                  type="button"
                  onClick={resetFilters}
                  className="mt-4 rounded-lg bg-slate-900 px-4 py-2 text-xs font-medium text-white hover:bg-slate-800"
                >
                  Скинути фільтри
                </button>
              </>
            ) : (
              <>
                <p className="mt-3 text-sm font-medium text-slate-900">
                  Ваша шафа поки порожня
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  Додайте першу річ, щоб почати збирати образи.
                </p>
                <button
                  type="button"
                  onClick={() => setIsAddOpen(true)}
                  className="mt-4 inline-flex items-center gap-1.5 rounded-lg bg-slate-900 px-4 py-2 text-xs font-medium text-white hover:bg-slate-800"
                >
                  <Plus className="h-3.5 w-3.5" />
                  Додати першу річ
                </button>
              </>
            )}
          </div>
        )}

        {!isLoading && !isError && items.length > 0 && (
          <>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
              {items.map((item) => (
                <ItemCard
                  key={item.id}
                  item={item}
                  onSelect={(clicked) => setSelectedItem(clicked)}
                />
              ))}
            </div>

            {hasNextPage && (
              <div className="flex justify-center pt-2">
                <button
                  type="button"
                  disabled={isFetchingNextPage}
                  onClick={() => void fetchNextPage()}
                  className="rounded-xl border border-slate-200 bg-white px-5 py-2 text-xs font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-50"
                >
                  {isFetchingNextPage ? 'Завантаження...' : 'Завантажити ще'}
                </button>
              </div>
            )}
          </>
        )}
      </main>

      {isAddOpen && (
        <AddItemModal
          categories={categories}
          colors={colors}
          onClose={() => setIsAddOpen(false)}
        />
      )}

      {selectedItem && (
        <ItemDetailModal
          item={selectedItem}
          onClose={() => setSelectedItem(null)}
        />
      )}
    </div>
  )
}
