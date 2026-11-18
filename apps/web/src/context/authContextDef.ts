import { createContext } from 'react'
import type { LoginPayload } from '../api/auth'

export interface AuthUser {
  userId: string
  email: string
  displayName: string
}

export interface AuthContextValue {
  user: AuthUser | null
  isInitializing: boolean
  login: (payload: LoginPayload) => Promise<void>
  logout: () => Promise<void>
  updateDisplayName: (displayName: string) => void
  resetSession: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
