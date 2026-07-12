import type { HealthLog, HeartbeatPulse } from '~/types/pulse'

export type ConnectionStatus = 'loading' | 'failed' | 'connected'

const STABLE_POLL_MS = 5000
const INITIAL_BACKOFF_MS = 1000
const MAX_BACKOFF_MS = 8000

/** Module-level timer handles — survive page navigations (singleton). */
let retryTimeout: ReturnType<typeof setTimeout> | null = null
let stableInterval: ReturnType<typeof setInterval> | null = null
let inFlight = false
let engineBootstrapped = false

function clearRetryTimeout() {
  if (retryTimeout) {
    clearTimeout(retryTimeout)
    retryTimeout = null
  }
}

function clearStableInterval() {
  if (stableInterval) {
    clearInterval(stableInterval)
    stableInterval = null
  }
}

function clearAllTimers() {
  clearRetryTimeout()
  clearStableInterval()
}

function backoffDelay(attemptAfterFailure: number): number {
  const delay = INITIAL_BACKOFF_MS * 2 ** Math.max(0, attemptAfterFailure - 1)
  return Math.min(delay, MAX_BACKOFF_MS)
}

/**
 * Global Pulse Core connection state + singleton polling engine.
 * State lives in Nuxt `useState` so Dashboard ↔ Settings navigation
 * never resets retries or spawns duplicate intervals.
 */
export function usePulseConnection() {
  const config = useRuntimeConfig()
  const apiBase = computed(() => config.public.apiBase as string)
  const { maxInitialConnectionRetries } = useFrontendSettings()

  const connectionStatus = useState<ConnectionStatus>(
    'pulse.connectionStatus',
    () => 'loading',
  )
  const retryCount = useState<number>('pulse.retryCount', () => 0)
  const isPollingActive = useState<boolean>('pulse.isPollingActive', () => false)
  const pending = useState<boolean>('pulse.pending', () => false)
  const lastError = useState<string | null>('pulse.lastError', () => null)
  const pulses = useState<HeartbeatPulse[]>('pulse.pulses', () => [])
  const logs = useState<HealthLog[]>('pulse.logs', () => [])

  const maxRetries = computed(() => maxInitialConnectionRetries.value)

  /** 1-based attempt label for the spinner, e.g. "2/3". */
  const attemptLabel = computed(() => {
    const max = maxRetries.value
    const current = Math.min(Math.max(retryCount.value + 1, 1), max)
    return `${current}/${max}`
  })

  async function fetchPulseData(): Promise<boolean> {
    if (inFlight) return false
    inFlight = true
    pending.value = true

    try {
      const [statusResult, logsResult] = await Promise.all([
        $fetch<HeartbeatPulse[]>(`${apiBase.value}/api/pulse/status`),
        $fetch<HealthLog[]>(`${apiBase.value}/api/pulse/logs`),
      ])

      pulses.value = statusResult ?? []
      logs.value = logsResult ?? []
      lastError.value = null
      return true
    }
    catch (err: unknown) {
      lastError.value = err instanceof Error ? err.message : 'Request failed'
      return false
    }
    finally {
      pending.value = false
      inFlight = false
    }
  }

  function enterConnectedMode() {
    retryCount.value = 0
    lastError.value = null
    connectionStatus.value = 'connected'
    clearRetryTimeout()

    if (!stableInterval) {
      isPollingActive.value = true
      stableInterval = setInterval(() => {
        void runStableTick()
      }, STABLE_POLL_MS)
    }
    else {
      isPollingActive.value = true
    }
  }

  async function runStableTick() {
    const ok = await fetchPulseData()
    if (ok) {
      retryCount.value = 0
    }
  }

  function enterFailedMode() {
    connectionStatus.value = 'failed'
    isPollingActive.value = false
    clearAllTimers()
  }

  function scheduleConnectingRetry() {
    clearRetryTimeout()
    const delay = backoffDelay(retryCount.value)
    retryTimeout = setTimeout(() => {
      retryTimeout = null
      void runConnectingAttempt()
    }, delay)
  }

  async function runConnectingAttempt() {
    if (connectionStatus.value === 'failed') return

    connectionStatus.value = 'loading'
    const ok = await fetchPulseData()

    if (ok) {
      enterConnectedMode()
      return
    }

    retryCount.value += 1

    if (retryCount.value >= maxRetries.value) {
      enterFailedMode()
      return
    }

    scheduleConnectingRetry()
  }

  function startConnecting() {
    clearAllTimers()
    isPollingActive.value = false
    retryCount.value = 0
    lastError.value = null
    connectionStatus.value = 'loading'
    void runConnectingAttempt()
  }

  /** Manual retry from the failed overlay — resets budget and reconnects. */
  function retryManually() {
    startConnecting()
  }

  async function refreshNow() {
    if (connectionStatus.value === 'failed') {
      retryManually()
      return
    }

    const ok = await fetchPulseData()
    if (ok) {
      retryCount.value = 0
      lastError.value = null
      if (connectionStatus.value !== 'connected') {
        enterConnectedMode()
      }
    }
  }

  /**
   * Idempotent bootstrap. Safe to call from plugin + any page.
   * Never tears down on route leave — timers are module-scoped.
   */
  function ensureEngineStarted() {
    if (!import.meta.client) return

    // Re-attach stable interval after HMR without resetting state.
    if (engineBootstrapped) {
      if (connectionStatus.value === 'connected' && !stableInterval) {
        enterConnectedMode()
      }
      return
    }

    engineBootstrapped = true

    if (connectionStatus.value === 'connected') {
      enterConnectedMode()
      return
    }

    if (connectionStatus.value === 'failed') {
      return
    }

    startConnecting()
  }

  return {
    apiBase,
    pulses,
    logs,
    connectionStatus,
    retryCount,
    maxRetries,
    isPollingActive,
    pending,
    lastError,
    attemptLabel,
    retryManually,
    refreshNow,
    ensureEngineStarted,
  }
}

/** @deprecated Prefer usePulseConnection — kept as alias for older imports. */
export function usePulsePolling() {
  return usePulseConnection()
}
