<template>
  <article
    class="panel group relative overflow-hidden p-5 transition-all duration-300 animate-fade-in"
    :class="cardRingClass"
  >
    <div
      class="pointer-events-none absolute -right-8 -top-8 h-28 w-28 rounded-full blur-3xl transition-opacity duration-500"
      :class="isHealthy ? 'bg-emerald-500/20 opacity-80' : 'bg-rose-500/25 opacity-90'"
    />

    <div class="relative flex items-start justify-between gap-3">
      <div class="min-w-0">
        <p class="panel-header">App ID</p>
        <h3 class="mt-1 truncate font-mono text-lg font-semibold text-white">
          {{ pulse.appId }}
        </h3>
        <p class="mt-0.5 truncate text-sm text-slate-400">
          {{ pulse.serviceName }}
        </p>
      </div>

      <div
        class="inline-flex shrink-0 items-center gap-2 rounded-full px-3 py-1.5 text-xs font-semibold uppercase tracking-wide transition-all duration-300"
        :class="badgeClass"
      >
        <span class="relative flex h-2 w-2">
          <span
            class="absolute inline-flex h-full w-full rounded-full animate-pulse-dot"
            :class="isHealthy ? 'bg-emerald-300' : 'bg-rose-300'"
          />
          <span
            class="relative inline-flex h-2 w-2 rounded-full"
            :class="isHealthy ? 'bg-emerald-400' : 'bg-rose-400'"
          />
        </span>
        {{ isHealthy ? 'Healthy' : 'Offline' }}
      </div>
    </div>

    <dl class="relative mt-5 grid grid-cols-2 gap-4">
      <div>
        <dt class="panel-header">Memory</dt>
        <dd class="mt-1 font-mono text-base text-slate-100">
          <template v-if="memoryMb != null">{{ memoryMb.toFixed(1) }} <span class="text-xs text-pulse-muted">MB</span></template>
          <template v-else>—</template>
        </dd>
      </div>
      <div>
        <dt class="panel-header">Last Heartbeat</dt>
        <dd class="mt-1 font-mono text-sm text-slate-100" :title="absoluteHeartbeat">
          {{ relativeHeartbeat }}
        </dd>
      </div>
    </dl>

    <div
      v-if="!isHealthy"
      class="relative mt-4 flex items-center justify-between rounded-lg border border-rose-500/30 bg-rose-500/10 px-3 py-2"
    >
      <span class="text-xs font-medium uppercase tracking-wider text-rose-300">Downtime</span>
      <span class="font-mono text-sm font-semibold tabular-nums text-rose-200">
        {{ downtimeLabel }}
      </span>
    </div>
  </article>
</template>

<script setup lang="ts">
import type { HeartbeatPulse } from '~/types/pulse'
import { formatAbsoluteTimestamp, formatDowntime, humanizeTimestamp, parseMemoryMb } from '~/utils/format'

const props = defineProps<{
  pulse: HeartbeatPulse
  /** Shared clock tick so downtime counters stay in sync */
  nowMs: number
}>()

const isHealthy = computed(() => props.pulse.status?.toLowerCase() !== 'offline')

const memoryMb = computed(() => parseMemoryMb(props.pulse.metadata))

const relativeHeartbeat = computed(() =>
  humanizeTimestamp(props.pulse.timestamp, props.nowMs),
)

const absoluteHeartbeat = computed(() =>
  formatAbsoluteTimestamp(props.pulse.timestamp),
)

const downtimeLabel = computed(() => {
  const since = props.pulse.offlineSince ?? props.pulse.timestamp
  if (!since) return '00:00'
  const start = new Date(since).getTime()
  if (Number.isNaN(start)) return '00:00'
  return formatDowntime(props.nowMs - start)
})

const badgeClass = computed(() =>
  isHealthy.value
    ? 'bg-emerald-500/15 text-emerald-300 ring-1 ring-emerald-500/30 shadow-glow-green'
    : 'bg-rose-500/15 text-rose-300 ring-1 ring-rose-500/30 shadow-glow-red',
)

const cardRingClass = computed(() =>
  isHealthy.value
    ? 'hover:border-emerald-500/30 hover:shadow-glow-green'
    : 'border-rose-500/25 hover:border-rose-500/40 hover:shadow-glow-red',
)
</script>
