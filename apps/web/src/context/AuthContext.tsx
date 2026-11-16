import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { useQueryClient } from '@tanstack/react-query'
import * as authApi from '../api/auth'
import type { LoginPayload } from '../api/auth'

export interface AuthUser {
  userId: string
  email: string
  displayName: string
}

interface AuthContextValue {
  user: AuthUser | null
  isInitializing: boolean
  login: (payload: LoginPayload) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)
  const queryClient = useQueryClient()

  useEffect(() => {
    let active = true

    authApi
      .restoreSession()
      .then((session) => {
        if (!active || !session) {
          return
        }
        setUser({
          userId: session.userId,
          email: session.email,
          displayName: session.displayName,
        })
      })
      .finally(() => {
        if (active) {
          setIsInitializing(false)
        }
      })

    return () => {
      active = false
    }
  }, [])

  const login = useCallback(async (payload: LoginPayload) => {
    const response = await authApi.login(payload)
    setUser({
      userId: response.userId,
      email: response.email,
      displayName: response.displayName,
    })
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    setUser(null)
    queryClient.clear()
  }, [queryClient])

  const value = useMemo(
    () => ({ user, isInitializing, login, logout }),
    [user, isInitializing, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return ctx
}
