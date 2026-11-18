import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  changePassword,
  deleteAccount,
  deleteAvatar,
  getProfile,
  updateProfile,
  uploadAvatar,
  type ChangePasswordPayload,
  type UpdateProfilePayload,
} from '../api/profile'
import { useAuth } from '../context/useAuth'

export const PROFILE_QUERY_KEY = ['profile'] as const

export function useProfile() {
  return useQuery({
    queryKey: PROFILE_QUERY_KEY,
    queryFn: getProfile,
  })
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()
  const { updateDisplayName } = useAuth()

  return useMutation({
    mutationFn: (payload: UpdateProfilePayload) => updateProfile(payload),
    onSuccess: (updated) => {
      queryClient.setQueryData(PROFILE_QUERY_KEY, updated)
      updateDisplayName(updated.displayName)
    },
  })
}

export function useUploadAvatar() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => uploadAvatar(file),
    onSuccess: (updated) => {
      queryClient.setQueryData(PROFILE_QUERY_KEY, updated)
    },
  })
}

export function useDeleteAvatar() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => deleteAvatar(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PROFILE_QUERY_KEY })
    },
  })
}

export function useChangePassword() {
  const { resetSession } = useAuth()

  return useMutation({
    mutationFn: (payload: ChangePasswordPayload) => changePassword(payload),
    onSuccess: () => {
      resetSession()
    },
  })
}

export function useDeleteAccount() {
  const { resetSession } = useAuth()

  return useMutation({
    mutationFn: () => deleteAccount(),
    onSuccess: () => {
      resetSession()
    },
  })
}
