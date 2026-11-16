import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { getCategories, getColors, getItems } from '../api/items'
import type { ItemFilterParams } from '../api/types'

const DICTIONARY_STALE_TIME_MS = 30 * 60 * 1000
const ITEMS_PAGE_SIZE = 24

export function useCategories() {
  return useQuery({
    queryKey: ['categories'],
    queryFn: getCategories,
    staleTime: DICTIONARY_STALE_TIME_MS,
  })
}

export function useColors() {
  return useQuery({
    queryKey: ['colors'],
    queryFn: getColors,
    staleTime: DICTIONARY_STALE_TIME_MS,
  })
}

export type ItemsQueryFilters = Omit<ItemFilterParams, 'page' | 'pageSize'>

export function useInfiniteItems(filters: ItemsQueryFilters) {
  return useInfiniteQuery({
    queryKey: ['items', filters],
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      getItems({
        ...filters,
        page: pageParam,
        pageSize: ITEMS_PAGE_SIZE,
      }),
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.page + 1 : undefined,
  })
}
