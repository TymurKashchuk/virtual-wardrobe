import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  createItem,
  deleteItem,
  getCategories,
  getColors,
  getItems,
  type CreateItemPayload,
} from '../api/items'
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

export function useCreateItem() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateItemPayload) => createItem(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['items'] })
    },
  })
}

export function useDeleteItem() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deleteItem(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['items'] })
    },
  })
}
