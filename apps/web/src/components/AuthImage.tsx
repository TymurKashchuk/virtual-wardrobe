import { useEffect, useState } from 'react'
import { apiFetchBlob } from '../api/client'

interface AuthImageProps {
  src: string
  alt: string
  className?: string
}

interface ImageState {
  src: string
  url: string | null
  error: boolean
}

export function AuthImage({ src, alt, className = '' }: AuthImageProps) {
  const [state, setState] = useState<ImageState>({
    src,
    url: null,
    error: false,
  })

  useEffect(() => {
    const controller = new AbortController()
    let createdUrl: string | null = null

    apiFetchBlob(src, controller.signal)
      .then((blob) => {
        if (controller.signal.aborted) {
          return
        }
        createdUrl = URL.createObjectURL(blob)
        setState({ src, url: createdUrl, error: false })
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setState({ src, url: null, error: true })
        }
      })

    return () => {
      controller.abort()
      if (createdUrl) {
        URL.revokeObjectURL(createdUrl)
      }
    }
  }, [src])

  const isCurrent = state.src === src
  const objectUrl = isCurrent ? state.url : null
  const hasError = isCurrent ? state.error : false

  if (hasError) {
    return (
      <div
        className={`flex items-center justify-center bg-slate-100 text-xs text-slate-400 ${className}`}
        role="img"
        aria-label={alt}
      >
        Фото недоступне
      </div>
    )
  }

  if (!objectUrl) {
    return <div className={`animate-pulse bg-slate-200 ${className}`} />
  }

  return <img src={objectUrl} alt={alt} className={className} loading="lazy" />
}
