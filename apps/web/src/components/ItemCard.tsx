import { getItemThumbPath } from '../api/items'
import type { ItemDto } from '../api/types'
import { AuthImage } from './AuthImage'

interface ItemCardProps {
  item: ItemDto
  onSelect?: (item: ItemDto) => void
}

export function ItemCard({ item, onSelect }: ItemCardProps) {
  return (
    <button
      type="button"
      onClick={() => onSelect?.(item)}
      className="group flex flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white text-left shadow-xs transition hover:border-slate-300 hover:shadow-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900"
    >
      <div className="aspect-[3/4] w-full overflow-hidden bg-slate-100">
        <AuthImage
          src={getItemThumbPath(item.id)}
          alt={item.name}
          className="h-full w-full object-cover transition duration-300 group-hover:scale-[1.02]"
        />
      </div>

      <div className="flex flex-1 flex-col justify-between p-3">
        <div>
          <p className="line-clamp-1 text-sm font-semibold text-slate-900">
            {item.name}
          </p>
          <p className="mt-0.5 text-xs text-slate-500">{item.categoryNameUk}</p>
        </div>

        <div className="mt-2.5 flex items-center gap-1.5">
          {item.colors.slice(0, 3).map((color) => (
            <span
              key={color.colorId}
              title={`${color.nameUk}${color.isPrimary ? ' (основний)' : ''}`}
              className="h-3.5 w-3.5 rounded-full border border-slate-300"
              style={{ backgroundColor: color.hex }}
            />
          ))}
        </div>
      </div>
    </button>
  )
}
