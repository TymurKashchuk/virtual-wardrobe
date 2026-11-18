import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { useQueryClient } from '@tanstack/react-query'
import * as authApi from '../api/auth'
import type { LoginPayload } from '../api/auth'
import { AuthContext, type AuthUser } from './authContextDef'

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

  const resetSession = useCallback(() => {
    setUser(null)
    queryClient.clear()
  }, [queryClient])

  const logout = useCallback(async () => {
    await authApi.logout()
    resetSession()
  }, [resetSession])

  const updateDisplayName = useCallback((displayName: string) => {
    setUser((prev) => (prev ? { ...prev, displayName } : null))
  }, [])

  const value = useMemo(
    () => ({
      user,
      isInitializing,
      login,
      logout,
      updateDisplayName,
      resetSession,
    }),
    [user, isInitializing, login, logout, updateDisplayName, resetSession],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
