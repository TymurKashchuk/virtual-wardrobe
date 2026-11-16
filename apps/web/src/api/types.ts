export const ClothingStyle = {
  Casual: 1,
  Sport: 2,
  Business: 3,
  Evening: 4,
} as const

export type ClothingStyle = (typeof ClothingStyle)[keyof typeof ClothingStyle]

export const CLOTHING_STYLE_LABELS: Record<ClothingStyle, string> = {
  [ClothingStyle.Casual]: 'Повсякденний',
  [ClothingStyle.Sport]: 'Спортивний',
  [ClothingStyle.Business]: 'Діловий',
  [ClothingStyle.Evening]: 'Вечірній',
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  userId: string
  email: string
  displayName: string
}

export interface RegisterResponse {
  userId: string
  email: string
  displayName: string
  message: string
}

export interface CategoryDto {
  id: number
  code: string
  nameUk: string
  maxPerOutfit: number
  sortOrder: number
}

export interface ColorDto {
  id: number
  code: string
  nameUk: string
  hex: string
  isNeutral: boolean
  sortOrder: number
}

export interface ItemColorDto {
  colorId: number
  code: string
  nameUk: string
  hex: string
  isNeutral: boolean
  isPrimary: boolean
}

export interface ItemDto {
  id: string
  name: string
  categoryId: number
  categoryCode: string
  categoryNameUk: string
  style: ClothingStyle
  notes: string | null
  photoKey: string
  thumbKey: string
  colors: ItemColorDto[]
  createdAt: string
  updatedAt: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface ItemFilterParams {
  categoryIds?: number[]
  colorIds?: number[]
  search?: string
  sortBy?: 'newest' | 'name'
  page?: number
  pageSize?: number
}

export interface ApiErrorPayload {
  message?: string
  error?: string
  errors?: Record<string, string[]>
}
