<template>
  <div class="mx-auto max-w-3xl space-y-6 animate-fade-in">
    <div
      v-if="coreOffline"
      class="flex items-start gap-3 rounded-xl border border-amber-500/25 bg-amber-500/10 px-4 py-3 text-sm text-amber-100/90"
      role="status"
    >
      <span class="mt-0.5 shrink-0" aria-hidden="true">⚠️</span>
      <p class="leading-relaxed">
        Core Server is offline. Global configuration cannot be saved until the connection is restored.
      </p>
    </div>

    <!-- Single unified global configuration panel -->
    <form class="panel space-y-8 overflow-hidden p-6" @submit.prevent="saveGlobalSettings">
      <div>
        <h2 class="text-base font-semibold text-white">Global System &amp; Alerting Configuration</h2>
        <p class="mt-0.5 text-xs text-pulse-muted">
          One master policy for every monitored app ·
          <span class="font-mono text-cyan-400/80">GET/POST /api/settings/system</span>
        </p>
      </div>

      <!-- Runtime tuning -->
      <fieldset class="space-y-4">
        <legend class="text-sm font-semibold text-slate-100">System Timing</legend>
        <p class="text-[11px] text-pulse-muted">
          Core is the single source of truth. Monitored apps read these via
          <span class="font-mono text-cyan-400/80">GET /api/pulse/client-config</span>
          — do not hardcode intervals in each project.
        </p>
        <div class="grid gap-4 sm:grid-cols-2">
          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="heartbeat-interval">
              Heartbeat Interval (seconds)
            </label>
            <input
              id="heartbeat-interval"
              v-model.number="settings.globalHeartbeatIntervalSeconds"
              type="number"
              min="5"
              max="60"
              step="1"
              class="input-field font-mono"
              :disabled="coreOffline || saving"
            >
            <p class="mt-1.5 text-[11px] text-pulse-muted">
              Default 10 · range 5–60. How often apps should POST heartbeats (must be &lt; offline threshold).
            </p>
          </div>

          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="offline-threshold">
              Offline Threshold (seconds)
            </label>
            <input
              id="offline-threshold"
              v-model.number="settings.globalOfflineThresholdSeconds"
              type="number"
              min="5"
              max="300"
              step="1"
              class="input-field font-mono"
              :disabled="coreOffline || saving"
            >
            <p class="mt-1.5 text-[11px] text-pulse-muted">
              Default 20 · range 5–300. Silence longer than this marks an app Offline.
            </p>
          </div>

          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="escalation-delay">
              Escalation Delay (seconds)
            </label>
            <input
              id="escalation-delay"
              v-model.number="settings.globalEscalationDelaySeconds"
              type="number"
              min="0"
              max="600"
              step="1"
              class="input-field font-mono"
              :disabled="coreOffline || saving"
            >
            <p class="mt-1.5 text-[11px] text-pulse-muted">
              Default 30 · range 0–600. Time spent Offline before Telegram / Twilio / beep fire.
            </p>
          </div>

          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="scan-interval">
              Scan Interval (seconds)
            </label>
            <input
              id="scan-interval"
              v-model.number="settings.globalScanIntervalSeconds"
              type="number"
              min="1"
              max="60"
              step="1"
              class="input-field font-mono"
              :disabled="coreOffline || saving"
            >
            <p class="mt-1.5 text-[11px] text-pulse-muted">
              Default 5 · range 1–60. How often PulseEngine re-evaluates live heartbeats.
            </p>
          </div>
        </div>
      </fieldset>

      <!-- Alert master switches -->
      <fieldset class="space-y-4 border-t border-pulse-border/50 pt-6">
        <legend class="text-sm font-semibold text-slate-100">Alerting</legend>

        <div class="flex items-center justify-between gap-4 rounded-lg border border-pulse-border/50 bg-pulse-bg/40 px-4 py-3">
          <div class="min-w-0">
            <p class="text-sm font-medium text-slate-100">Enable Alerts</p>
            <p class="text-xs text-pulse-muted">Master switch for beep, Telegram, and Twilio</p>
          </div>
          <ToggleSwitch v-model="settings.enableAlerts" />
        </div>

        <div class="flex items-center justify-between gap-4 rounded-lg border border-pulse-border/50 bg-pulse-bg/40 px-4 py-3">
          <div class="min-w-0">
            <p class="text-sm font-medium text-slate-100">Local Audio Alerts</p>
            <p class="text-xs text-pulse-muted">Console beep on the Core host when a service goes Offline</p>
          </div>
          <ToggleSwitch v-model="settings.localAudioAlerts" />
        </div>
      </fieldset>

      <!-- Telegram -->
      <fieldset class="space-y-4 border-t border-pulse-border/50 pt-6">
        <legend class="text-sm font-semibold text-slate-100">Telegram</legend>
        <div class="grid gap-4 sm:grid-cols-2">
          <div class="sm:col-span-2 min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tg-token">
              Bot Token
            </label>
            <input
              id="tg-token"
              v-model="settings.telegram.botToken"
              type="password"
              autocomplete="off"
              class="input-field font-mono"
              placeholder="123456:ABC-DEF…"
              :disabled="coreOffline || saving"
            >
          </div>
          <div class="sm:col-span-2 min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tg-chat">
              Chat ID
            </label>
            <input
              id="tg-chat"
              v-model="settings.telegram.chatId"
              type="text"
              class="input-field font-mono"
              placeholder="-100…"
              :disabled="coreOffline || saving"
            >
          </div>
        </div>
      </fieldset>

      <!-- Twilio -->
      <fieldset class="space-y-4 border-t border-pulse-border/50 pt-6">
        <legend class="text-sm font-semibold text-slate-100">Twilio Voice</legend>
        <div class="grid gap-4 sm:grid-cols-2">
          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-sid">
              Account SID
            </label>
            <input
              id="tw-sid"
              v-model="settings.twilio.accountSid"
              type="text"
              class="input-field font-mono"
              autocomplete="off"
              :disabled="coreOffline || saving"
            >
          </div>
          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-auth">
              Auth Token
            </label>
            <input
              id="tw-auth"
              v-model="settings.twilio.authToken"
              type="password"
              class="input-field font-mono"
              autocomplete="off"
              :disabled="coreOffline || saving"
            >
          </div>
          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-from">
              From Number
            </label>
            <input
              id="tw-from"
              v-model="settings.twilio.fromNumber"
              type="tel"
              class="input-field font-mono"
              placeholder="+1…"
              :disabled="coreOffline || saving"
            >
          </div>
          <div class="min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-to">
              To Number
            </label>
            <input
              id="tw-to"
              v-model="settings.twilio.toNumber"
              type="tel"
              class="input-field font-mono"
              placeholder="+1…"
              :disabled="coreOffline || saving"
            >
          </div>
          <div class="sm:col-span-2 min-w-0">
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="voice-msg">
              Voice Message
            </label>
            <textarea
              id="voice-msg"
              v-model="settings.twilio.voiceMessage"
              rows="3"
              class="input-field max-h-40 resize-y"
              placeholder="Spoken message when the voice alert fires…"
              :disabled="coreOffline || saving"
            />
          </div>
        </div>
      </fieldset>

      <div class="flex flex-wrap items-center justify-end gap-3 border-t border-pulse-border/50 pt-6">
        <button
          type="button"
          class="rounded-lg border border-pulse-border/70 px-4 py-2 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
          :disabled="coreOffline || loading"
          @click="loadGlobalSettings"
        >
          {{ loading ? 'Loading…' : 'Load from Core' }}
        </button>
        <button
          type="submit"
          class="btn-primary min-w-[160px]"
          :disabled="saving || coreOffline"
          :title="coreOffline ? 'Core is offline' : undefined"
        >
          <span v-if="saving" class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />
          {{ saving ? 'Saving…' : 'Save Global Configuration' }}
        </button>
      </div>
    </form>

    <!-- Browser-only client prefs (unchanged local storage) -->
    <section class="panel overflow-hidden p-6">
      <div class="mb-5">
        <h2 class="text-base font-semibold text-white">Dashboard Client Settings</h2>
        <p class="mt-0.5 text-xs text-pulse-muted">
          Local browser preferences — not stored on Core
        </p>
      </div>

      <div class="grid gap-4 sm:grid-cols-2">
        <div class="min-w-0">
          <label class="mb-1.5 block text-xs font-medium text-slate-400" for="dashboard-log-limit">
            Dashboard Log Row Limit
          </label>
          <input
            id="dashboard-log-limit"
            v-model.number="logLimitInput"
            type="number"
            min="1"
            :max="ABSOLUTE_MAX_DASHBOARD_LOG_LIMIT"
            step="1"
            class="input-field font-mono"
          >
          <p class="mt-1.5 text-[11px] text-pulse-muted">
            Default {{ DEFAULT_DASHBOARD_LOG_LIMIT }} · caps the Dashboard timeline preview.
          </p>
        </div>

        <div class="min-w-0">
          <label class="mb-1.5 flex items-center gap-1.5 text-xs font-medium text-slate-400" for="max-retries">
            Max Initial Connection Retries
            <span
              class="group relative inline-flex h-4 w-4 cursor-help items-center justify-center rounded-full border border-pulse-border text-[10px] text-pulse-muted"
              :title="retriesTooltip"
              :aria-label="retriesTooltip"
            >
              ?
            </span>
          </label>
          <input
            id="max-retries"
            v-model.number="retriesInput"
            type="number"
            min="1"
            :max="ABSOLUTE_MAX_INITIAL_RETRIES"
            step="1"
            class="input-field font-mono"
          >
          <p class="mt-1.5 text-[11px] text-pulse-muted">
            Default {{ DEFAULT_MAX_INITIAL_RETRIES }} · capped at {{ ABSOLUTE_MAX_INITIAL_RETRIES }}.
          </p>
        </div>
      </div>

      <div class="mt-4 flex justify-end">
        <button
          type="button"
          class="rounded-lg border border-pulse-border/70 px-4 py-2 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white"
          @click="saveClientSettings"
        >
          Save Client Settings
        </button>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import type { UrPulseSettings } from '~/types/pulse'
import { createDefaultUrPulseSettings } from '~/types/pulse'

const config = useRuntimeConfig()
const apiBase = config.public.apiBase as string
const toast = useToast()

const { connectionStatus } = usePulseConnection()
const coreOffline = computed(() => connectionStatus.value === 'failed')

const settings = reactive<UrPulseSettings>(createDefaultUrPulseSettings())
const loading = ref(false)
const saving = ref(false)

const {
  maxInitialConnectionRetries,
  setMaxInitialConnectionRetries,
  dashboardLogLimit,
  setDashboardLogLimit,
  DEFAULT_MAX_INITIAL_RETRIES,
  ABSOLUTE_MAX_INITIAL_RETRIES,
  DEFAULT_DASHBOARD_LOG_LIMIT,
  ABSOLUTE_MAX_DASHBOARD_LOG_LIMIT,
} = useFrontendSettings()

const logLimitInput = ref(dashboardLogLimit.value)
const retriesInput = ref(maxInitialConnectionRetries.value)

const retriesTooltip
  = 'How many times the Dashboard retries reaching Core on startup before stopping all timers.'

watch(dashboardLogLimit, (v) => { logLimitInput.value = v })
watch(maxInitialConnectionRetries, (v) => { retriesInput.value = v })

function applySettings(payload: UrPulseSettings) {
  const defaults = createDefaultUrPulseSettings()
  Object.assign(settings, {
    ...defaults,
    ...payload,
    telegram: { ...defaults.telegram, ...(payload.telegram ?? {}) },
    twilio: { ...defaults.twilio, ...(payload.twilio ?? {}) },
  })
}

async function loadGlobalSettings() {
  if (coreOffline.value) {
    toast.info('Core offline', 'Connect to Ur Pulse Core before loading global settings.')
    return
  }

  loading.value = true
  try {
    const data = await $fetch<UrPulseSettings>(`${apiBase}/api/settings/system`)
    applySettings(data)
    toast.success(
      'Configuration loaded',
      `HB ${settings.globalHeartbeatIntervalSeconds}s · Offline ${settings.globalOfflineThresholdSeconds}s · Escalation ${settings.globalEscalationDelaySeconds}s`,
    )
  }
  catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Unable to read global settings from Core.'
    toast.error('Load failed', message)
  }
  finally {
    loading.value = false
  }
}

async function saveGlobalSettings() {
  if (coreOffline.value) {
    toast.info('Core offline', 'Global configuration cannot be saved until Core is active.')
    return
  }

  const heartbeat = Math.min(60, Math.max(5, Math.floor(Number(settings.globalHeartbeatIntervalSeconds) || 10)))
  const threshold = Math.min(300, Math.max(5, Math.floor(Number(settings.globalOfflineThresholdSeconds) || 20)))
  const interval = Math.min(60, Math.max(1, Math.floor(Number(settings.globalScanIntervalSeconds) || 5)))
  const escalation = Math.min(600, Math.max(0, Math.floor(Number(settings.globalEscalationDelaySeconds) || 30)))

  if (heartbeat >= threshold) {
    toast.error('Invalid timing', 'Heartbeat interval must be less than the offline threshold.')
    return
  }

  settings.globalHeartbeatIntervalSeconds = heartbeat
  settings.globalOfflineThresholdSeconds = threshold
  settings.globalScanIntervalSeconds = interval
  settings.globalEscalationDelaySeconds = escalation

  const payload: UrPulseSettings = {
    globalHeartbeatIntervalSeconds: heartbeat,
    globalOfflineThresholdSeconds: threshold,
    globalScanIntervalSeconds: interval,
    globalEscalationDelaySeconds: escalation,
    enableAlerts: settings.enableAlerts,
    localAudioAlerts: settings.localAudioAlerts,
    telegram: {
      botToken: settings.telegram.botToken?.trim() ?? '',
      chatId: settings.telegram.chatId?.trim() ?? '',
    },
    twilio: {
      accountSid: settings.twilio.accountSid?.trim() ?? '',
      authToken: settings.twilio.authToken?.trim() ?? '',
      fromNumber: settings.twilio.fromNumber?.trim() ?? '',
      toNumber: settings.twilio.toNumber?.trim() ?? '',
      voiceMessage: settings.twilio.voiceMessage?.trim() ?? '',
    },
  }

  saving.value = true
  try {
    await $fetch(`${apiBase}/api/settings/system`, {
      method: 'POST',
      body: payload,
    })
    toast.success(
      'Global configuration saved',
      `HB ${heartbeat}s · Offline ${threshold}s · Escalation ${escalation}s · Alerts ${payload.enableAlerts ? 'on' : 'off'}`,
    )
  }
  catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Core rejected the configuration update.'
    toast.error('Save failed', message)
  }
  finally {
    saving.value = false
  }
}

function saveClientSettings() {
  setDashboardLogLimit(logLimitInput.value)
  const clamped = Math.min(
    ABSOLUTE_MAX_INITIAL_RETRIES,
    Math.max(1, Math.floor(Number(retriesInput.value) || DEFAULT_MAX_INITIAL_RETRIES)),
  )
  retriesInput.value = clamped
  setMaxInitialConnectionRetries(clamped)
  toast.success(
    'Client settings saved',
    `Log rows ${dashboardLogLimit.value} · Max retries ${clamped}.`,
  )
}

onMounted(() => {
  if (!coreOffline.value) {
    loadGlobalSettings()
  }
})
</script>
