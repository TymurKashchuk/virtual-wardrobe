import type { WardrobeSummaryDto } from '../api/types'

interface WardrobeSummaryBarProps {
  summary: WardrobeSummaryDto
  selectedCategoryIds: number[]
  onToggleCategory: (categoryId: number) => void
}

export function WardrobeSummaryBar({
  summary,
  selectedCategoryIds,
  onToggleCategory,
}: WardrobeSummaryBarProps) {
  const nonEmptyCategories = summary.categories.filter((c) => c.count > 0)
  const fillPercent =
    summary.maxItemsLimit > 0
      ? Math.min(100, Math.round((summary.totalCount / summary.maxItemsLimit) * 100))
      : 0

  return (
    <section
      aria-label="Статистика гардеробу"
      className="rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-2xs"
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2 text-xs text-slate-600">
          <span className="font-semibold text-slate-900">
            У шафі: {summary.totalCount}
          </span>
          <span className="text-slate-400">/ {summary.maxItemsLimit} ліміт</span>
        </div>

        <div className="h-1.5 w-28 overflow-hidden rounded-full bg-slate-100">
          <div
            className="h-full rounded-full bg-slate-900 transition-all"
            style={{ width: `${fillPercent}%` }}
          />
        </div>
      </div>

      {nonEmptyCategories.length > 0 && (
        <div className="mt-2.5 flex flex-wrap gap-1.5 border-t border-slate-100 pt-2.5">
          {nonEmptyCategories.map((cat) => {
            const active = selectedCategoryIds.includes(cat.categoryId)
            return (
              <button
                key={cat.categoryId}
                type="button"
                onClick={() => onToggleCategory(cat.categoryId)}
                className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-[11px] font-medium transition ${
                  active
                    ? 'bg-slate-900 text-white'
                    : 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                }`}
              >
                <span>{cat.nameUk}</span>
                <span className={active ? 'text-slate-300' : 'text-slate-500'}>
                  {cat.count}
                </span>
              </button>
            )
          })}
        </div>
      )}
    </section>
  )
}
