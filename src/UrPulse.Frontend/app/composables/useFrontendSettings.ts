const STORAGE_KEY = 'urpulse.frontend.settings'
const DEFAULT_MAX_INITIAL_RETRIES = 3
const ABSOLUTE_MAX_INITIAL_RETRIES = 10
const DEFAULT_DASHBOARD_LOG_LIMIT = 10
const ABSOLUTE_MAX_DASHBOARD_LOG_LIMIT = 100

export interface FrontendSettings {
  /** Max failed attempts during the initial connection phase before giving up. */
  maxInitialConnectionRetries: number
  /** Max rows shown on the Dashboard Historical Telemetry Timeline. */
  dashboardLogLimit: number
}

function clampRetries(value: unknown): number {
  const n = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(n)) return DEFAULT_MAX_INITIAL_RETRIES
  return Math.min(ABSOLUTE_MAX_INITIAL_RETRIES, Math.max(1, Math.floor(n)))
}

function clampLogLimit(value: unknown): number {
  const n = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(n)) return DEFAULT_DASHBOARD_LOG_LIMIT
  return Math.min(ABSOLUTE_MAX_DASHBOARD_LOG_LIMIT, Math.max(1, Math.floor(n)))
}

function readFromStorage(): FrontendSettings {
  const defaults: FrontendSettings = {
    maxInitialConnectionRetries: DEFAULT_MAX_INITIAL_RETRIES,
    dashboardLogLimit: DEFAULT_DASHBOARD_LOG_LIMIT,
  }

  if (!import.meta.client) return defaults

  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return defaults
    const parsed = JSON.parse(raw) as Partial<FrontendSettings>
    return {
      maxInitialConnectionRetries: clampRetries(parsed.maxInitialConnectionRetries),
      dashboardLogLimit: clampLogLimit(
        parsed.dashboardLogLimit ?? DEFAULT_DASHBOARD_LOG_LIMIT,
      ),
    }
  }
  catch {
    return defaults
  }
}

function writeToStorage(settings: FrontendSettings) {
  if (!import.meta.client) return
  localStorage.setItem(STORAGE_KEY, JSON.stringify(settings))
}

/**
 * Shared frontend preferences via Nuxt `useState` + localStorage persistence.
 */
export function useFrontendSettings() {
  const maxInitialConnectionRetries = useState<number>(
    'pulse.maxInitialConnectionRetries',
    () => DEFAULT_MAX_INITIAL_RETRIES,
  )
  const dashboardLogLimit = useState<number>(
    'pulse.dashboardLogLimit',
    () => DEFAULT_DASHBOARD_LOG_LIMIT,
  )
  const hydrated = useState<boolean>('pulse.settingsHydrated', () => false)

  if (import.meta.client && !hydrated.value) {
    const stored = readFromStorage()
    maxInitialConnectionRetries.value = stored.maxInitialConnectionRetries
    dashboardLogLimit.value = stored.dashboardLogLimit
    hydrated.value = true
  }

  function persist() {
    writeToStorage({
      maxInitialConnectionRetries: maxInitialConnectionRetries.value,
      dashboardLogLimit: dashboardLogLimit.value,
    })
  }

  function setMaxInitialConnectionRetries(value: number) {
    maxInitialConnectionRetries.value = clampRetries(value)
    persist()
  }

  function setDashboardLogLimit(value: number) {
    dashboardLogLimit.value = clampLogLimit(value)
    persist()
  }

  function save(partial: Partial<FrontendSettings>) {
    if (partial.maxInitialConnectionRetries != null) {
      maxInitialConnectionRetries.value = clampRetries(partial.maxInitialConnectionRetries)
    }
    if (partial.dashboardLogLimit != null) {
      dashboardLogLimit.value = clampLogLimit(partial.dashboardLogLimit)
    }
    persist()
  }

  return {
    maxInitialConnectionRetries,
    dashboardLogLimit,
    setMaxInitialConnectionRetries,
    setDashboardLogLimit,
    save,
    DEFAULT_MAX_INITIAL_RETRIES,
    ABSOLUTE_MAX_INITIAL_RETRIES,
    DEFAULT_DASHBOARD_LOG_LIMIT,
    ABSOLUTE_MAX_DASHBOARD_LOG_LIMIT,
  }
}
