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

export const UserRole = {
  User: 1,
  Admin: 2,
} as const

export type UserRole = (typeof UserRole)[keyof typeof UserRole]

export interface UserProfileDto {
  id: string
  email: string
  displayName: string
  hasAvatar: boolean
  hasPassword: boolean
  role: UserRole
  createdAt: string
}

export interface ApiErrorPayload {
  message?: string
  error?: string
  errors?: Record<string, string[]>
}

export const OutfitSource = {
  Manual: 1,
  Recommendation: 2,
} as const

export type OutfitSource = (typeof OutfitSource)[keyof typeof OutfitSource]

export const OUTFIT_SOURCE_LABELS: Record<OutfitSource, string> = {
  [OutfitSource.Manual]: 'Створено вручну',
  [OutfitSource.Recommendation]: 'Рекомендація',
}

export interface OutfitItemDto {
  itemId: string
  name: string
  categoryId: number
  categoryCode: string
  categoryNameUk: string
  thumbKey: string | null
  position: number
  primaryColorHex: string | null
}

export interface OutfitSummaryDto {
  id: string
  name: string
  style: ClothingStyle | null
  isFavorite: boolean
  source: OutfitSource
  itemsCount: number
  itemThumbKeys: string[]
  createdAt: string
  updatedAt: string
}

export interface OutfitDto {
  id: string
  name: string
  style: ClothingStyle | null
  notes: string | null
  isFavorite: boolean
  source: OutfitSource
  items: OutfitItemDto[]
  createdAt: string
  updatedAt: string
}
