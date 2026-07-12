<template>
  <!-- Strict gate: never mount cards/timeline until Connected -->
  <ConnectionOverlay
    v-if="connectionStatus !== 'connected'"
    :connection-status="connectionStatus"
    :attempt-label="attemptLabel"
    :api-base="apiBase"
    :max-retries="maxRetries"
    :pending="pending"
    @retry="retryManually"
  />

  <div
    v-else
    class="space-y-8 animate-fade-in"
  >
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="panel-header">Active Applications</p>
        <p class="mt-1 text-sm text-slate-400">
          Polling every 5s ·
          <span class="text-emerald-400/80">{{ pulses.length }} live</span>
        </p>
      </div>
      <button
        type="button"
        class="rounded-lg border border-pulse-border/70 bg-pulse-surface/50 px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white disabled:opacity-50"
        :disabled="pending"
        @click="refreshNow"
      >
        Refresh now
      </button>
    </div>

    <div
      v-if="!pulses.length"
      class="panel flex flex-col items-center justify-center px-6 py-16 text-center"
    >
      <div class="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-cyan-500/10 ring-1 ring-cyan-500/20">
        <span class="text-xl text-pulse-cyan">◯</span>
      </div>
      <p class="text-sm font-medium text-slate-200">No active pulses yet</p>
      <p class="mt-1 max-w-sm text-xs text-pulse-muted">
        Apps register automatically when they send their first telemetry pulse to {{ apiBase }}.
      </p>
    </div>

    <div
      v-else
      class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
    >
      <PulseStatusCard
        v-for="pulse in pulses"
        :key="`${pulse.appId}:${pulse.serviceName}`"
        :pulse="pulse"
        :now-ms="nowMs"
      />
    </div>

    <TelemetryTimeline
      :logs="logs"
      :pending="false"
      :now-ms="nowMs"
      :max-rows="dashboardLogLimit"
    />
  </div>
</template>

<script setup lang="ts">
const {
  apiBase,
  pulses,
  logs,
  connectionStatus,
  pending,
  maxRetries,
  attemptLabel,
  retryManually,
  refreshNow,
  ensureEngineStarted,
} = usePulseConnection()

const { dashboardLogLimit } = useFrontendSettings()

ensureEngineStarted()

const nowMs = ref(Date.now())
let clockTimer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  clockTimer = setInterval(() => {
    nowMs.value = Date.now()
  }, 1000)
})

onUnmounted(() => {
  if (clockTimer) clearInterval(clockTimer)
  // Intentionally do NOT stop the global connection engine here.
})
</script>
