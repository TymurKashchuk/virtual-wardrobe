import { apiFetch } from './client'
import type {
  OutfitDto,
  OutfitFilterParams,
  OutfitSummaryDto,
  PagedResult,
} from './types'

export function getOutfits(
  params: OutfitFilterParams = {},
): Promise<PagedResult<OutfitSummaryDto>> {
  const query = new URLSearchParams()

  if (params.search?.trim()) {
    query.set('search', params.search.trim())
  }
  if (params.isFavorite !== undefined) {
    query.set('isFavorite', String(params.isFavorite))
  }
  if (params.style !== undefined) {
    query.set('style', String(params.style))
  }
  if (params.page) {
    query.set('page', String(params.page))
  }
  if (params.pageSize) {
    query.set('pageSize', String(params.pageSize))
  }

  const queryString = query.toString()
  return apiFetch<PagedResult<OutfitSummaryDto>>(
    queryString ? `/api/v1/outfits?${queryString}` : '/api/v1/outfits',
  )
}

export function getOutfit(id: string): Promise<OutfitDto> {
  return apiFetch<OutfitDto>(`/api/v1/outfits/${id}`)
}
