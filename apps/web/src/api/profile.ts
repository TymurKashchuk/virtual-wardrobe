import { apiFetch, clearAccessToken } from './client'
import type { UserProfileDto } from './types'

export interface UpdateProfilePayload {
  displayName: string
}

export interface ChangePasswordPayload {
  currentPassword?: string
  newPassword: string
}

export function getProfile(): Promise<UserProfileDto> {
  return apiFetch<UserProfileDto>('/api/v1/me')
}

export function updateProfile(payload: UpdateProfilePayload): Promise<UserProfileDto> {
  return apiFetch<UserProfileDto>('/api/v1/me', {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function uploadAvatar(file: File): Promise<UserProfileDto> {
  const formData = new FormData()
  formData.append('avatar', file)

  return apiFetch<UserProfileDto>('/api/v1/me/avatar', {
    method: 'PUT',
    body: formData,
  })
}

export function deleteAvatar(): Promise<void> {
  return apiFetch<void>('/api/v1/me/avatar', {
    method: 'DELETE',
  })
}

export function getAvatarPath(version?: number): string {
  return version ? `/api/v1/me/avatar?v=${version}` : '/api/v1/me/avatar'
}

export async function changePassword(
  payload: ChangePasswordPayload,
): Promise<{ message: string }> {
  const result = await apiFetch<{ message: string }>('/api/v1/me/password', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
  clearAccessToken()
  return result
}

export async function deleteAccount(): Promise<void> {
  await apiFetch<void>('/api/v1/me', {
    method: 'DELETE',
  })
  clearAccessToken()
}
