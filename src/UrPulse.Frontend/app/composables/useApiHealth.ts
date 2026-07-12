/**
 * Live ping against the Pulse Core API to drive the sidebar health indicator.
 */
export function useApiHealth(intervalMs = 5000) {
  const config = useRuntimeConfig()
  const isOnline = ref(false)
  const lastCheckedAt = ref<Date | null>(null)
  const latencyMs = ref<number | null>(null)

  let timer: ReturnType<typeof setInterval> | null = null

  async function check() {
    const started = performance.now()
    try {
      await $fetch(`${config.public.apiBase}/api/pulse/status`, {
        method: 'GET',
        timeout: 4000,
      })
      isOnline.value = true
      latencyMs.value = Math.round(performance.now() - started)
    }
    catch {
      isOnline.value = false
      latencyMs.value = null
    }
    finally {
      lastCheckedAt.value = new Date()
    }
  }

  function start() {
    if (!import.meta.client || timer) return
    void check()
    timer = setInterval(() => {
      void check()
    }, intervalMs)
  }

  function stop() {
    if (timer) {
      clearInterval(timer)
      timer = null
    }
  }

  onMounted(start)
  onUnmounted(stop)

  return {
    isOnline: readonly(isOnline),
    lastCheckedAt: readonly(lastCheckedAt),
    latencyMs: readonly(latencyMs),
    check,
  }
}
