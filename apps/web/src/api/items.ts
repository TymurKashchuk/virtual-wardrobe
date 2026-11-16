import { apiFetch } from './client'
import type {
  CategoryDto,
  ClothingStyle,
  ColorDto,
  ItemDto,
  ItemFilterParams,
  PagedResult,
} from './types'

export interface CreateItemPayload {
  name: string
  categoryId: number
  primaryColorId: number
  secondaryColorIds?: number[]
  style: ClothingStyle
  notes?: string
  photo: File
}

export function getCategories(): Promise<CategoryDto[]> {
  return apiFetch<CategoryDto[]>('/api/v1/categories', { skipAuthRefresh: true })
}

export function getColors(): Promise<ColorDto[]> {
  return apiFetch<ColorDto[]>('/api/v1/colors', { skipAuthRefresh: true })
}

export function getItems(params: ItemFilterParams = {}): Promise<PagedResult<ItemDto>> {
  const query = new URLSearchParams()

  params.categoryIds?.forEach((id) => query.append('categoryIds', String(id)))
  params.colorIds?.forEach((id) => query.append('colorIds', String(id)))

  if (params.search?.trim()) {
    query.set('search', params.search.trim())
  }
  if (params.sortBy) {
    query.set('sortBy', params.sortBy)
  }
  if (params.page) {
    query.set('page', String(params.page))
  }
  if (params.pageSize) {
    query.set('pageSize', String(params.pageSize))
  }

  const queryString = query.toString()
  return apiFetch<PagedResult<ItemDto>>(
    queryString ? `/api/v1/items?${queryString}` : '/api/v1/items',
  )
}

export function createItem(payload: CreateItemPayload): Promise<ItemDto> {
  const formData = new FormData()
  formData.append('name', payload.name)
  formData.append('categoryId', String(payload.categoryId))
  formData.append('primaryColorId', String(payload.primaryColorId))
  payload.secondaryColorIds?.forEach((id) =>
    formData.append('secondaryColorIds', String(id)),
  )
  formData.append('style', String(payload.style))
  if (payload.notes?.trim()) {
    formData.append('notes', payload.notes.trim())
  }
  formData.append('photo', payload.photo)

  return apiFetch<ItemDto>('/api/v1/items', {
    method: 'POST',
    body: formData,
  })
}

export function deleteItem(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/items/${id}`, { method: 'DELETE' })
}

export function getItemThumbPath(id: string): string {
  return `/api/v1/items/${id}/thumb`
}

export function getItemPhotoPath(id: string): string {
  return `/api/v1/items/${id}/photo`
}
