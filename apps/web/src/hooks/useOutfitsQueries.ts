import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  createOutfit,
  deleteOutfit,
  duplicateOutfit,
  getOutfit,
  getOutfits,
  toggleOutfitFavorite,
  updateOutfit,
} from '../api/outfits'
import type {
  CreateOutfitRequest,
  OutfitDto,
  OutfitFilterParams,
  UpdateOutfitRequest,
} from '../api/types'

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

export function useCreateOutfit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateOutfitRequest) => createOutfit(payload),
    onSuccess: (created) => {
      queryClient.setQueryData(outfitKeys.detail(created.id), created)
      void queryClient.invalidateQueries({ queryKey: outfitKeys.lists() })
    },
  })
}

export interface UpdateOutfitVariables {
  id: string
  payload: UpdateOutfitRequest
}

export function useUpdateOutfit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, payload }: UpdateOutfitVariables) =>
      updateOutfit(id, payload),
    onSuccess: (updated) => {
      queryClient.setQueryData(outfitKeys.detail(updated.id), updated)
      void queryClient.invalidateQueries({ queryKey: outfitKeys.lists() })
    },
  })
}

export function useDeleteOutfit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deleteOutfit(id),
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: outfitKeys.detail(id) })
      void queryClient.invalidateQueries({ queryKey: outfitKeys.lists() })
    },
  })
}

export function useDuplicateOutfit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => duplicateOutfit(id),
    onSuccess: (duplicated) => {
      queryClient.setQueryData(outfitKeys.detail(duplicated.id), duplicated)
      void queryClient.invalidateQueries({ queryKey: outfitKeys.lists() })
    },
  })
}

export function useToggleOutfitFavorite() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => toggleOutfitFavorite(id),
    onSuccess: (result, id) => {
      queryClient.setQueryData<OutfitDto>(
        outfitKeys.detail(id),
        (prev) => (prev ? { ...prev, isFavorite: result.isFavorite } : prev),
      )
      void queryClient.invalidateQueries({ queryKey: outfitKeys.lists() })
    },
  })
}
