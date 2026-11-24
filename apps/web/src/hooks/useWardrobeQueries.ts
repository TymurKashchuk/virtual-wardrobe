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
  getWardrobeSummary,
  updateItem,
  type CreateItemPayload,
  type UpdateItemPayload,
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

export function useWardrobeSummary() {
  return useQuery({
    queryKey: ['items-summary'],
    queryFn: getWardrobeSummary,
  })
}

export function useCreateItem() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateItemPayload) => createItem(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['items'] })
      void queryClient.invalidateQueries({ queryKey: ['items-summary'] })
    },
  })
}

export interface UpdateItemVariables {
  id: string
  payload: UpdateItemPayload
}

export function useUpdateItem() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, payload }: UpdateItemVariables) =>
      updateItem(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['items'] })
      void queryClient.invalidateQueries({ queryKey: ['items-summary'] })
    },
  })
}

export function useDeleteItem() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deleteItem(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['items'] })
      void queryClient.invalidateQueries({ queryKey: ['items-summary'] })
    },
  })
}
