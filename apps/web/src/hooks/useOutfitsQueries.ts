import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { getOutfit, getOutfits } from '../api/outfits'
import type { OutfitFilterParams } from '../api/types'

const OUTFITS_PAGE_SIZE = 24

export type OutfitsQueryFilters = Omit<OutfitFilterParams, 'page' | 'pageSize'>

export const outfitKeys = {
  all: ['outfits'] as const,
  lists: () => [...outfitKeys.all, 'list'] as const,
  list: (filters: OutfitsQueryFilters) => [...outfitKeys.lists(), filters] as const,
  details: () => [...outfitKeys.all, 'detail'] as const,
  detail: (id: string) => [...outfitKeys.details(), id] as const,
}

export function useInfiniteOutfits(filters: OutfitsQueryFilters = {}) {
  return useInfiniteQuery({
    queryKey: outfitKeys.list(filters),
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      getOutfits({
        ...filters,
        page: pageParam,
        pageSize: OUTFITS_PAGE_SIZE,
      }),
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.page + 1 : undefined,
  })
}

export function useOutfit(id: string | undefined) {
  return useQuery({
    queryKey: outfitKeys.detail(id ?? ''),
    queryFn: () => getOutfit(id!),
    enabled: Boolean(id),
  })
}
