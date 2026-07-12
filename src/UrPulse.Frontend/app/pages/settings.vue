<template>
  <div class="mx-auto max-w-3xl space-y-6 animate-fade-in">
    <!-- Subtle vault-offline banner (global Failed state) -->
    <div
      v-if="vaultOffline"
      class="flex items-start gap-3 rounded-xl border border-amber-500/25 bg-amber-500/10 px-4 py-3 text-sm text-amber-100/90"
      role="status"
    >
      <span class="mt-0.5 shrink-0" aria-hidden="true">⚠️</span>
      <p class="leading-relaxed">
        Configuration Vault is offline. Changes cannot be saved until the Core Server is active.
      </p>
    </div>

    <!-- General System Settings — Core offline threshold + scan interval -->
    <section class="panel p-6">
      <div class="mb-5">
        <h2 class="text-base font-semibold text-white">General System Settings</h2>
        <p class="mt-0.5 text-xs text-pulse-muted">
          Live Core tuning · dead-line timeout and health-sweeper cadence
        </p>
      </div>

      <div class="grid gap-4 sm:grid-cols-2">
        <div>
          <label class="mb-1.5 block text-xs font-medium text-slate-400" for="offline-threshold">
            Global Offline Detection Threshold (seconds)
          </label>
          <input
            id="offline-threshold"
            v-model.number="offlineThresholdSeconds"
            type="number"
            min="5"
            max="300"
            step="1"
            class="input-field font-mono"
            :disabled="vaultOffline || savingSystemSettings"
          >
          <p class="mt-1.5 text-[11px] text-pulse-muted">
            Default 20 · range 5–300. Silence longer than this marks an app OFFLINE.
          </p>
        </div>

        <div>
          <label class="mb-1.5 block text-xs font-medium text-slate-400" for="scan-interval">
            Server Scan Interval (seconds)
          </label>
          <input
            id="scan-interval"
            v-model.number="scanIntervalSeconds"
            type="number"
            min="1"
            max="60"
            step="1"
            class="input-field font-mono"
            :disabled="vaultOffline || savingSystemSettings"
          >
          <p class="mt-1.5 text-[11px] text-pulse-muted">
            Default 5 · range 1–60. How often PulseEngine re-evaluates live heartbeats.
          </p>
        </div>

        <div class="sm:col-span-2">
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
            class="input-field font-mono max-w-xs"
          >
          <p class="mt-1.5 text-[11px] text-pulse-muted">
            Default {{ DEFAULT_DASHBOARD_LOG_LIMIT }} · client-side only (useState + localStorage). Caps the Dashboard timeline preview.
          </p>
        </div>
      </div>

      <p class="mt-3 font-mono text-[11px] text-pulse-muted">
        Threshold &amp; interval → <span class="text-cyan-400/80">GET/POST /api/settings/system</span>
        · Row limit → local browser state
      </p>

      <div class="mt-4 flex justify-end gap-2">
        <button
          type="button"
          class="rounded-lg border border-pulse-border/70 px-4 py-2 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
          :disabled="vaultOffline || loadingSystemSettings"
          @click="loadSystemSettings"
        >
          {{ loadingSystemSettings ? 'Loading…' : 'Load from Core' }}
        </button>
        <button
          type="button"
          class="btn-primary !px-4 !py-2 !text-xs"
          :disabled="savingSystemSettings"
          @click="saveSystemSettings"
        >
          {{ savingSystemSettings ? 'Saving…' : 'Save System Settings' }}
        </button>
      </div>
    </section>

    <!-- General / frontend preferences (local only — always instant) -->
    <section class="panel p-6">
      <div class="mb-5">
        <h2 class="text-base font-semibold text-white">Dashboard Client Settings</h2>
        <p class="mt-0.5 text-xs text-pulse-muted">
          Dashboard connection behavior stored locally in this browser
        </p>
      </div>

      <div class="max-w-xs">
        <label class="mb-1.5 flex items-center gap-1.5 text-xs font-medium text-slate-400" for="max-retries">
          Max Initial Connection Retries
          <span
            class="group relative inline-flex h-4 w-4 cursor-help items-center justify-center rounded-full border border-pulse-border text-[10px] text-pulse-muted"
            :title="retriesTooltip"
            :aria-label="retriesTooltip"
          >
            ?
            <span
              class="pointer-events-none absolute bottom-full left-1/2 z-20 mb-2 hidden w-56 -translate-x-1/2 rounded-lg border border-pulse-border bg-pulse-surface px-3 py-2 text-left text-[11px] font-normal leading-relaxed text-slate-300 shadow-lg group-hover:block"
            >
              {{ retriesTooltip }}
            </span>
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
          Default {{ DEFAULT_MAX_INITIAL_RETRIES }} · capped at {{ ABSOLUTE_MAX_INITIAL_RETRIES }}. Uses exponential backoff, then stops polling to avoid memory bloat.
        </p>
      </div>

      <div class="mt-4 flex justify-end">
        <button
          type="button"
          class="rounded-lg border border-pulse-border/70 px-4 py-2 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white"
          @click="saveGeneralSettings"
        >
          Save General Settings
        </button>
      </div>
    </section>

    <!-- App selector -->
    <section class="panel p-6">
      <div class="mb-5">
        <div class="flex items-start gap-2">
          <div>
            <h2 class="text-base font-semibold text-white">Application Selector</h2>
            <p class="mt-0.5 text-xs text-pulse-muted">
              Choose a monitored app or enter a custom App ID to load vault settings
            </p>
          </div>
          <span
            class="group relative mt-1 inline-flex h-4 w-4 shrink-0 cursor-help items-center justify-center rounded-full border border-pulse-border text-[10px] text-pulse-muted"
            :title="vaultLinkTooltip"
            :aria-label="vaultLinkTooltip"
          >
            ?
            <span
              class="pointer-events-none absolute bottom-full left-1/2 z-20 mb-2 hidden w-64 -translate-x-1/2 rounded-lg border border-pulse-border bg-pulse-surface px-3 py-2 text-left text-[11px] font-normal leading-relaxed text-slate-300 shadow-lg group-hover:block sm:left-auto sm:right-0 sm:translate-x-0"
            >
              {{ vaultLinkTooltip }}
            </span>
          </span>
        </div>
      </div>

      <div
        class="mb-5 rounded-lg border border-cyan-500/20 bg-cyan-500/5 px-4 py-3 text-xs leading-relaxed text-slate-300"
      >
        <p class="font-medium text-cyan-300/90">How apps appear in Ur Pulse</p>
        <p class="mt-1 text-pulse-muted">
          Saving alert settings here links <strong class="font-medium text-slate-300">out-of-band</strong> vault
          configuration to an App ID — it does not register the app by itself. New applications appear on the
          Dashboard automatically when they send their first telemetry pulse to Core.
        </p>
      </div>

      <div class="grid gap-4 sm:grid-cols-2">
        <div>
          <label class="mb-1.5 block text-xs font-medium text-slate-400" for="app-select">
            Known Applications
          </label>
          <select
            id="app-select"
            v-model="selectedKnownApp"
            class="input-field"
            @change="onKnownAppSelect"
          >
            <option value="">— Select an app —</option>
            <option
              v-for="id in knownAppIds"
              :key="id"
              :value="id"
            >
              {{ id }}
            </option>
          </select>
        </div>

        <div>
          <label class="mb-1.5 flex items-center gap-1.5 text-xs font-medium text-slate-400" for="app-id">
            App ID
            <span
              class="group relative inline-flex h-4 w-4 cursor-help items-center justify-center rounded-full border border-pulse-border text-[10px] text-pulse-muted"
              title="Use the same App ID your client sends in heartbeats. Vault rows are keyed by this value."
            >
              ?
            </span>
          </label>
          <input
            id="app-id"
            v-model="appId"
            type="text"
            class="input-field font-mono"
            placeholder="e.g. vector-kanban"
            @keydown.enter.prevent="loadSettings"
          >
        </div>
      </div>

      <div class="mt-4 flex justify-end">
        <button
          type="button"
          class="rounded-lg border border-pulse-border/70 px-4 py-2 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
          :disabled="!appId.trim() || loadingSettings || vaultOffline"
          :title="vaultOffline ? 'Vault is offline' : undefined"
          @click="loadSettings"
        >
          {{ loadingSettings ? 'Loading…' : 'Load from Vault' }}
        </button>
      </div>
    </section>

    <form class="panel space-y-8 p-6" @submit.prevent="saveSettings">
      <div>
        <h2 class="text-base font-semibold text-white">Multi-Channel Alerting</h2>
        <p class="mt-0.5 text-xs text-pulse-muted">
          Maps 1:1 to backend <span class="font-mono text-cyan-400/80">AlertSettings</span>
        </p>
      </div>

      <!-- Core toggles -->
      <div class="space-y-4">
        <div class="flex items-center justify-between gap-4 rounded-lg border border-pulse-border/50 bg-pulse-bg/40 px-4 py-3">
          <div>
            <p class="text-sm font-medium text-slate-100">Enable Alerts</p>
            <p class="text-xs text-pulse-muted">Master switch for escalation pipeline</p>
          </div>
          <ToggleSwitch v-model="settings.enableAlerts" />
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label class="mb-1.5 block text-xs font-medium text-slate-400" for="threshold">
              Escalation Threshold (seconds)
            </label>
            <input
              id="threshold"
              v-model.number="settings.escalationThresholdSeconds"
              type="number"
              min="1"
              step="1"
              class="input-field font-mono"
            >
          </div>

          <div class="flex items-end">
            <div class="flex w-full items-center justify-between gap-4 rounded-lg border border-pulse-border/50 bg-pulse-bg/40 px-4 py-3">
              <div>
                <p class="text-sm font-medium text-slate-100">Local Audio Alerts</p>
                <p class="text-xs text-pulse-muted">EnableLoudAudioAlert</p>
              </div>
              <ToggleSwitch v-model="settings.enableLoudAudioAlert" />
            </div>
          </div>
        </div>
      </div>

      <!-- Telegram -->
      <fieldset class="space-y-4 border-t border-pulse-border/50 pt-6">
        <div class="flex items-center justify-between gap-4">
          <legend class="text-sm font-semibold text-slate-100">Telegram Alerts</legend>
          <ToggleSwitch v-model="settings.enableTelegramAlert" />
        </div>

        <Transition name="reveal">
          <div v-if="settings.enableTelegramAlert" class="grid gap-4 sm:grid-cols-2">
            <div class="sm:col-span-2">
              <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tg-token">
                Bot Token
              </label>
              <input
                id="tg-token"
                v-model="settings.telegramBotToken"
                type="password"
                autocomplete="off"
                class="input-field font-mono"
                placeholder="123456:ABC-DEF…"
              >
            </div>
            <div class="sm:col-span-2">
              <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tg-chat">
                Chat ID
              </label>
              <input
                id="tg-chat"
                v-model="settings.telegramChatId"
                type="text"
                class="input-field font-mono"
                placeholder="-100…"
              >
            </div>
          </div>
        </Transition>
      </fieldset>

      <!-- Voice / Twilio -->
      <fieldset class="space-y-4 border-t border-pulse-border/50 pt-6">
        <div class="flex items-center justify-between gap-4">
          <legend class="text-sm font-semibold text-slate-100">Voice Call Alerts</legend>
          <ToggleSwitch v-model="settings.enableVoiceCallAlert" />
        </div>

        <Transition name="reveal">
          <div v-if="settings.enableVoiceCallAlert" class="grid gap-4 sm:grid-cols-2">
            <div>
              <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-sid">
                Twilio Account SID
              </label>
              <input
                id="tw-sid"
                v-model="settings.twilioAccountSid"
                type="text"
                class="input-field font-mono"
                autocomplete="off"
              >
            </div>
            <div>
              <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-auth">
                Auth Token
              </label>
              <input
                id="tw-auth"
                v-model="settings.twilioAuthToken"
                type="password"
                class="input-field font-mono"
                autocomplete="off"
              >
            </div>
            <div>
              <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-from">
                From Number
              </label>
              <input
                id="tw-from"
                v-model="settings.twilioFromNumber"
                type="tel"
                class="input-field font-mono"
                placeholder="+1…"
              >
            </div>
            <div>
              <label class="mb-1.5 block text-xs font-medium text-slate-400" for="tw-to">
                Target Phone Number
              </label>
              <input
                id="tw-to"
                v-model="settings.targetPhoneNumber"
                type="tel"
                class="input-field font-mono"
                placeholder="+1…"
              >
            </div>
            <div class="sm:col-span-2">
              <label class="mb-1.5 block text-xs font-medium text-slate-400" for="voice-msg">
                Custom Voice Message
              </label>
              <textarea
                id="voice-msg"
                v-model="settings.customVoiceMessage"
                rows="3"
                class="input-field resize-y"
                placeholder="Spoken message when the voice alert fires…"
              />
            </div>
          </div>
        </Transition>
      </fieldset>

      <div class="flex items-center justify-end gap-3 border-t border-pulse-border/50 pt-6">
        <button
          type="submit"
          class="btn-primary min-w-[160px]"
          :disabled="saving || !appId.trim() || vaultOffline"
          :title="vaultOffline ? 'Vault is offline' : undefined"
        >
          <span v-if="saving" class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />
          {{ saving ? 'Saving…' : 'Save to Vault' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import type { AlertSettings, SystemSettings } from '~/types/pulse'
import { createDefaultAlertSettings } from '~/types/pulse'

const config = useRuntimeConfig()
const apiBase = config.public.apiBase as string
const toast = useToast()

const { connectionStatus, pulses } = usePulseConnection()
const vaultOffline = computed(() => connectionStatus.value === 'failed')

const DEFAULT_OFFLINE_THRESHOLD = 20
const DEFAULT_SCAN_INTERVAL = 5
const offlineThresholdSeconds = ref(DEFAULT_OFFLINE_THRESHOLD)
const scanIntervalSeconds = ref(DEFAULT_SCAN_INTERVAL)
const loadingSystemSettings = ref(false)
const savingSystemSettings = ref(false)

async function loadSystemSettings() {
  if (vaultOffline.value) {
    toast.info('Core offline', 'Connect to Ur Pulse Core before loading system settings.')
    return
  }

  loadingSystemSettings.value = true
  try {
    const data = await $fetch<SystemSettings>(`${apiBase}/api/settings/system`)
    offlineThresholdSeconds.value = data.thresholdSeconds ?? DEFAULT_OFFLINE_THRESHOLD
    scanIntervalSeconds.value = data.intervalSeconds ?? DEFAULT_SCAN_INTERVAL
    toast.success('System settings loaded', `Threshold ${offlineThresholdSeconds.value}s · Scan ${scanIntervalSeconds.value}s`)
  }
  catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Unable to read system settings from Core.'
    toast.error('Load failed', message)
  }
  finally {
    loadingSystemSettings.value = false
  }
}

async function saveSystemSettings() {
  // Persist client row limit even when Core is offline.
  setDashboardLogLimit(logLimitInput.value)

  if (vaultOffline.value) {
    toast.info(
      'Row limit saved locally',
      'Core is offline — threshold/interval were not pushed. Connect and Save again to sync server settings.',
    )
    return
  }

  const threshold = Math.min(300, Math.max(5, Math.floor(Number(offlineThresholdSeconds.value) || DEFAULT_OFFLINE_THRESHOLD)))
  const interval = Math.min(60, Math.max(1, Math.floor(Number(scanIntervalSeconds.value) || DEFAULT_SCAN_INTERVAL)))
  offlineThresholdSeconds.value = threshold
  scanIntervalSeconds.value = interval
  savingSystemSettings.value = true

  try {
    await $fetch(`${apiBase}/api/settings/system`, {
      method: 'POST',
      body: {
        thresholdSeconds: threshold,
        intervalSeconds: interval,
      },
    })
    toast.success(
      'System settings saved',
      `Threshold ${threshold}s · Scan ${interval}s · Dashboard rows ${dashboardLogLimit.value}`,
    )
  }
  catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Core rejected the system settings update.'
    toast.error('Save failed', message)
  }
  finally {
    savingSystemSettings.value = false
  }
}

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

watch(dashboardLogLimit, (v) => {
  logLimitInput.value = v
})

const retriesInput = ref(maxInitialConnectionRetries.value)

const retriesTooltip
  = 'How many times the Dashboard retries reaching Core on startup before stopping all timers. Prevents endless polling when the backend is down.'

const vaultLinkTooltip
  = 'Vault alert settings are out-of-band: they attach escalation config to an App ID. Apps themselves register automatically when they send their first telemetry pulse.'

watch(maxInitialConnectionRetries, (v) => {
  retriesInput.value = v
})

function saveGeneralSettings() {
  const clamped = Math.min(
    ABSOLUTE_MAX_INITIAL_RETRIES,
    Math.max(1, Math.floor(Number(retriesInput.value) || DEFAULT_MAX_INITIAL_RETRIES)),
  )
  retriesInput.value = clamped
  setMaxInitialConnectionRetries(clamped)
  toast.success('General settings saved', `Max initial retries set to ${clamped}.`)
}

const appId = ref('vector-kanban')
const selectedKnownApp = ref('')
const settings = reactive<AlertSettings>(createDefaultAlertSettings())
const loadingSettings = ref(false)
const saving = ref(false)

/** Known apps from global pulse cache only — no network on page enter. */
const knownAppIds = computed(() => {
  const ids = new Set(pulses.value.map(p => p.appId).filter(Boolean))
  ids.add('vector-kanban')
  return Array.from(ids).sort()
})

/** Selection only fills App ID — vault fetch stays on-demand via the Load button. */
function onKnownAppSelect() {
  if (selectedKnownApp.value) {
    appId.value = selectedKnownApp.value
  }
}

function applySettings(payload: AlertSettings) {
  Object.assign(settings, {
    ...createDefaultAlertSettings(),
    ...payload,
    escalationThresholdSeconds: payload.escalationThresholdSeconds ?? 60,
  })
}

async function loadSettings() {
  const id = appId.value.trim()
  if (!id) {
    toast.info('Missing App ID', 'Enter an application ID before loading vault settings.')
    return
  }

  if (vaultOffline.value) {
    toast.info('Vault offline', 'Connect to Ur Pulse Core before loading settings.')
    return
  }

  loadingSettings.value = true
  try {
    const data = await $fetch<AlertSettings>(
      `${apiBase}/api/vault/settings/${encodeURIComponent(id)}`,
    )
    applySettings(data)
    toast.success('Vault loaded', `Settings retrieved for "${id}".`)
  }
  catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Unable to reach the vault API.'
    toast.error('Load failed', message)
  }
  finally {
    loadingSettings.value = false
  }
}

async function saveSettings() {
  const id = appId.value.trim()
  if (!id) {
    toast.info('Missing App ID', 'Provide an App ID before saving to the vault.')
    return
  }

  if (vaultOffline.value) {
    toast.info('Vault offline', 'Changes cannot be saved until the Core Server is active.')
    return
  }

  saving.value = true
  try {
    const payload: AlertSettings = {
      enableAlerts: settings.enableAlerts,
      escalationThresholdSeconds: Number(settings.escalationThresholdSeconds) || 60,
      enableLoudAudioAlert: settings.enableLoudAudioAlert,
      enableTelegramAlert: settings.enableTelegramAlert,
      telegramBotToken: settings.telegramBotToken,
      telegramChatId: settings.telegramChatId,
      enableVoiceCallAlert: settings.enableVoiceCallAlert,
      twilioAccountSid: settings.twilioAccountSid,
      twilioAuthToken: settings.twilioAuthToken,
      twilioFromNumber: settings.twilioFromNumber,
      targetPhoneNumber: settings.targetPhoneNumber,
      customVoiceMessage: settings.customVoiceMessage,
    }

    await $fetch(`${apiBase}/api/vault/settings/${encodeURIComponent(id)}`, {
      method: 'POST',
      body: payload,
    })

    toast.success('Saved to Vault', `Alert settings updated for "${id}".`)
  }
  catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'The vault rejected the write request.'
    toast.error('Save failed', message)
  }
  finally {
    saving.value = false
  }
}
</script>

<style scoped>
.reveal-enter-active,
.reveal-leave-active {
  transition: all 0.25s ease;
  overflow: hidden;
}
.reveal-enter-from,
.reveal-leave-to {
  opacity: 0;
  max-height: 0;
  transform: translateY(-6px);
}
.reveal-enter-to,
.reveal-leave-from {
  opacity: 1;
  max-height: 480px;
}
</style>
