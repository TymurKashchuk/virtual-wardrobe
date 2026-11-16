import type { ApiErrorPayload, AuthResponse } from './types'

let accessToken: string | null = null
let refreshPromise: Promise<AuthResponse | null> | null = null

export class ApiError extends Error {
  status: number
  code?: string
  errors?: Record<string, string[]>

  constructor(
    status: number,
    message: string,
    code?: string,
    errors?: Record<string, string[]>,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.errors = errors
  }
}

export function getAccessToken(): string | null {
  return accessToken
}

export function setAccessToken(token: string | null): void {
  accessToken = token
}

export function clearAccessToken(): void {
  accessToken = null
}

export async function refreshSessionSingleFlight(): Promise<AuthResponse | null> {
  if (refreshPromise) {
    return refreshPromise
  }

  refreshPromise = (async () => {
    try {
      const response = await fetch('/api/v1/auth/refresh', {
        method: 'POST',
        credentials: 'include',
      })

      if (!response.ok) {
        clearAccessToken()
        return null
      }

      const data = (await response.json()) as AuthResponse
      setAccessToken(data.accessToken)
      return data
    } catch {
      clearAccessToken()
      return null
    } finally {
      refreshPromise = null
    }
  })()

  return refreshPromise
}

export interface ApiRequestOptions extends RequestInit {
  skipAuthRefresh?: boolean
}

async function performAuthorizedFetch(
  path: string,
  options: ApiRequestOptions = {},
): Promise<Response> {
  const { skipAuthRefresh = false, headers: customHeaders, body, ...rest } = options

  const buildHeaders = (): Headers => {
    const headers = new Headers(customHeaders)
    if (accessToken) {
      headers.set('Authorization', `Bearer ${accessToken}`)
    }
    if (body && !(body instanceof FormData) && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }
    return headers
  }

  let response = await fetch(path, {
    ...rest,
    body,
    headers: buildHeaders(),
    credentials: 'include',
  })

  if (response.status === 401 && !skipAuthRefresh) {
    const refreshed = await refreshSessionSingleFlight()
    if (refreshed) {
      response = await fetch(path, {
        ...rest,
        body,
        headers: buildHeaders(),
        credentials: 'include',
      })
    }
  }

  return response
}

async function parseError(response: Response): Promise<ApiError> {
  let payload: ApiErrorPayload | null = null
  try {
    payload = (await response.json()) as ApiErrorPayload
  } catch {
    // Response may have an empty or non-JSON body.
  }

  const firstValidationError = payload?.errors
    ? Object.values(payload.errors).flat()[0]
    : undefined

  const message =
    payload?.message ??
    firstValidationError ??
    `Помилка запиту (${response.status})`

  return new ApiError(response.status, message, payload?.error, payload?.errors)
}

export async function apiFetch<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const response = await performAuthorizedFetch(path, options)

  if (!response.ok) {
    throw await parseError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  if (!text) {
    return undefined as T
  }

  return JSON.parse(text) as T
}

export async function apiFetchBlob(
  path: string,
  signal?: AbortSignal,
): Promise<Blob> {
  const response = await performAuthorizedFetch(path, { signal })

  if (!response.ok) {
    throw await parseError(response)
  }

  return response.blob()
}
