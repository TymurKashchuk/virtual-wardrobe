import {
  apiFetch,
  clearAccessToken,
  refreshSessionSingleFlight,
  setAccessToken,
} from './client'
import type { AuthResponse, RegisterResponse } from './types'

export interface LoginPayload {
  email: string
  password: string
}

export interface RegisterPayload {
  email: string
  password: string
  displayName: string
}

export async function login(payload: LoginPayload): Promise<AuthResponse> {
  const data = await apiFetch<AuthResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify(payload),
    skipAuthRefresh: true,
  })
  setAccessToken(data.accessToken)
  return data
}

export async function register(payload: RegisterPayload): Promise<RegisterResponse> {
  return apiFetch<RegisterResponse>('/api/v1/auth/register', {
    method: 'POST',
    body: JSON.stringify(payload),
    skipAuthRefresh: true,
  })
}

export async function confirmEmail(token: string): Promise<{ message: string }> {
  return apiFetch<{ message: string }>('/api/v1/auth/confirm-email', {
    method: 'POST',
    body: JSON.stringify({ token }),
    skipAuthRefresh: true,
  })
}

export async function restoreSession(): Promise<AuthResponse | null> {
  return refreshSessionSingleFlight()
}

export async function logout(): Promise<void> {
  try {
    await apiFetch<{ message: string }>('/api/v1/auth/logout', {
      method: 'POST',
      skipAuthRefresh: true,
    })
  } finally {
    clearAccessToken()
  }
}
