/**
 * Formats a duration in milliseconds into a compact HH:MM:SS / MM:SS / Ns string.
 */
export function formatDowntime(ms: number): string {
  if (!Number.isFinite(ms) || ms < 0) return '00:00'

  const totalSeconds = Math.floor(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  const pad = (n: number) => n.toString().padStart(2, '0')

  if (hours > 0) {
    return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`
  }

  return `${pad(minutes)}:${pad(seconds)}`
}

/**
 * Human-readable relative time (e.g. "3 minutes ago").
 */
export function humanizeTimestamp(value: string | Date | null | undefined, now = Date.now()): string {
  if (!value) return '—'

  const date = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(date.getTime())) return '—'

  const diffMs = now - date.getTime()
  const abs = Math.abs(diffMs)
  const future = diffMs < 0

  const seconds = Math.floor(abs / 1000)
  if (seconds < 5) return 'just now'
  if (seconds < 60) return future ? `in ${seconds}s` : `${seconds}s ago`

  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return future ? `in ${minutes}m` : `${minutes}m ago`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return future ? `in ${hours}h` : `${hours}h ago`

  const days = Math.floor(hours / 24)
  if (days < 7) return future ? `in ${days}d` : `${days}d ago`

  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function formatAbsoluteTimestamp(value: string | Date | null | undefined): string {
  if (!value) return '—'
  const date = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(date.getTime())) return '—'

  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  })
}

export function parseMemoryMb(metadata: Record<string, string> | undefined): number | null {
  if (!metadata) return null
  const raw = metadata.MemoryUsage_MB ?? metadata.memoryUsage_MB ?? metadata.memory_mb
  if (!raw) return null
  const parsed = Number.parseFloat(raw)
  return Number.isFinite(parsed) ? parsed : null
}
