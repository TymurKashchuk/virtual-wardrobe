import { useEffect, useState } from 'react'
import { apiFetchBlob } from '../api/client'

interface AuthImageProps {
  src: string
  alt: string
  className?: string
}

export function AuthImage({ src, alt, className = '' }: AuthImageProps) {
  const [objectUrl, setObjectUrl] = useState<string | null>(null)
  const [hasError, setHasError] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    let createdUrl: string | null = null

    setHasError(false)
    setObjectUrl(null)

    apiFetchBlob(src, controller.signal)
      .then((blob) => {
        if (controller.signal.aborted) {
          return
        }
        createdUrl = URL.createObjectURL(blob)
        setObjectUrl(createdUrl)
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setHasError(true)
        }
      })

    return () => {
      controller.abort()
      if (createdUrl) {
        URL.revokeObjectURL(createdUrl)
      }
    }
  }, [src])

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
