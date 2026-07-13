<template>
  <section class="panel overflow-hidden">
    <div class="flex items-center justify-between border-b border-pulse-border/60 px-5 py-4">
      <div>
        <h2 class="text-base font-semibold text-white">Historical Telemetry Timeline</h2>
        <p class="mt-0.5 text-xs text-pulse-muted">
          Latest {{ displayLogs.length }} of up to {{ maxRows }} recent transitions
        </p>
      </div>
      <span class="font-mono text-[11px] text-pulse-muted">
        {{ displayLogs.length }} events
      </span>
    </div>

    <div class="overflow-x-auto">
      <table class="w-full min-w-[640px] text-left text-sm">
        <thead>
          <tr class="border-b border-pulse-border/40 text-[11px] uppercase tracking-wider text-pulse-muted">
            <th class="px-5 py-3 font-medium">Timestamp</th>
            <th class="px-5 py-3 font-medium">App ID</th>
            <th class="px-5 py-3 font-medium">Service</th>
            <th class="px-5 py-3 font-medium">Status</th>
            <th class="px-5 py-3 font-medium">Relative</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="pending" class="text-pulse-muted">
            <td colspan="5" class="px-5 py-10 text-center text-sm">
              Loading telemetry…
            </td>
          </tr>
          <tr v-else-if="!displayLogs.length" class="text-pulse-muted">
            <td colspan="5" class="px-5 py-10 text-center text-sm">
              No historical transitions recorded yet.
            </td>
          </tr>
          <tr
            v-for="log in displayLogs"
            :key="log.id"
            class="border-b border-pulse-border/30 transition-colors duration-150 hover:bg-white/[0.03]"
          >
            <td class="max-w-[12rem] truncate px-5 py-3 font-mono text-xs text-slate-300" :title="formatAbsoluteTimestamp(log.timestamp)">
              {{ formatAbsoluteTimestamp(log.timestamp) }}
            </td>
            <td class="max-w-[10rem] truncate px-5 py-3 font-mono text-xs text-cyan-300/90" :title="log.appId">
              {{ log.appId }}
            </td>
            <td class="max-w-[10rem] truncate px-5 py-3 text-slate-300" :title="log.serviceName">
              {{ log.serviceName }}
            </td>
            <td class="px-5 py-3">
              <span
                class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium"
                :class="statusBadge(log.status)"
              >
                <span
                  class="h-1.5 w-1.5 rounded-full"
                  :class="log.status?.toLowerCase() === 'offline' ? 'bg-rose-400' : 'bg-emerald-400'"
                />
                {{ log.status }}
              </span>
            </td>
            <td class="px-5 py-3 font-mono text-xs text-pulse-muted">
              {{ humanizeTimestamp(log.timestamp, nowMs) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="flex items-center justify-end border-t border-pulse-border/50 px-5 py-4">
      <NuxtLink
        to="/logs"
        class="group inline-flex items-center gap-2 text-sm font-medium text-cyan-300/90 transition hover:text-cyan-200"
      >
        View Full Log History
        <span class="transition-transform group-hover:translate-x-0.5" aria-hidden="true">→</span>
      </NuxtLink>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { HealthLog } from '~/types/pulse'
import { formatAbsoluteTimestamp, humanizeTimestamp } from '~/utils/format'

const props = withDefaults(defineProps<{
  logs: HealthLog[]
  pending?: boolean
  nowMs: number
  maxRows?: number
}>(), {
  maxRows: 10,
})

const displayLogs = computed(() => props.logs.slice(0, props.maxRows))

function statusBadge(status: string) {
  const offline = status?.toLowerCase() === 'offline'
  return offline
    ? 'bg-rose-500/15 text-rose-300 ring-1 ring-rose-500/25'
    : 'bg-emerald-500/15 text-emerald-300 ring-1 ring-emerald-500/25'
}
</script>
