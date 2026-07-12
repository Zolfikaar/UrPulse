<template>
  <div class="animate-fade-in space-y-6">
    <div
      v-if="connectionStatus === 'failed'"
      class="flex items-start gap-3 rounded-xl border border-amber-500/25 bg-amber-500/10 px-4 py-3 text-sm text-amber-100/90"
      role="status"
    >
      <span class="mt-0.5 shrink-0" aria-hidden="true">⚠️</span>
      <p class="leading-relaxed">
        Core Server is unreachable. Log history cannot be loaded until the backend is active.
      </p>
    </div>

    <section class="panel overflow-hidden">
      <div class="flex flex-wrap items-end justify-between gap-4 border-b border-pulse-border/60 px-5 py-4">
        <div>
          <h2 class="text-base font-semibold text-white">Full Health Log History</h2>
          <p class="mt-0.5 text-xs text-pulse-muted">
            Server-side pagination · {{ pageSize }} rows per page
          </p>
        </div>
        <div class="flex items-center gap-3">
          <span class="font-mono text-[11px] text-pulse-muted">
            {{ totalCount }} total events
          </span>
          <NuxtLink
            to="/"
            class="rounded-lg border border-pulse-border/70 px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white"
          >
            ← Dashboard
          </NuxtLink>
        </div>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full min-w-[720px] text-left text-sm">
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
              <td colspan="5" class="px-5 py-16 text-center text-sm">
                Loading page {{ currentPage }}…
              </td>
            </tr>
            <tr v-else-if="!logs.length" class="text-pulse-muted">
              <td colspan="5" class="px-5 py-16 text-center text-sm">
                No health log records found.
              </td>
            </tr>
            <tr
              v-for="log in logs"
              :key="log.id"
              class="border-b border-pulse-border/30 transition-colors duration-150 hover:bg-white/[0.03]"
            >
              <td class="px-5 py-3 font-mono text-xs text-slate-300">
                {{ formatAbsoluteTimestamp(log.timestamp) }}
              </td>
              <td class="px-5 py-3 font-mono text-xs text-cyan-300/90">
                {{ log.appId }}
              </td>
              <td class="px-5 py-3 text-slate-300">
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

      <!-- Pagination controls -->
      <div class="flex flex-wrap items-center justify-between gap-4 border-t border-pulse-border/50 px-5 py-4">
        <p class="font-mono text-[11px] text-pulse-muted">
          Page {{ currentPage }} of {{ Math.max(totalPages, 1) }}
        </p>

        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="rounded-lg border border-pulse-border/70 px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="pending || currentPage <= 1 || connectionStatus === 'failed'"
            @click="goToPage(currentPage - 1)"
          >
            ← Previous
          </button>

          <div class="flex items-center gap-1">
            <button
              v-for="pageNum in visiblePages"
              :key="pageNum"
              type="button"
              class="min-w-[2rem] rounded-lg px-2.5 py-1.5 font-mono text-xs font-medium transition"
              :class="pageNum === currentPage
                ? 'bg-gradient-to-r from-cyan-600 to-teal-600 text-white shadow-glow'
                : 'border border-pulse-border/70 text-slate-400 hover:border-cyan-500/40 hover:text-white'"
              :disabled="pending || connectionStatus === 'failed'"
              @click="goToPage(pageNum)"
            >
              {{ pageNum }}
            </button>
          </div>

          <button
            type="button"
            class="rounded-lg border border-pulse-border/70 px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-cyan-500/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="pending || currentPage >= totalPages || totalPages === 0 || connectionStatus === 'failed'"
            @click="goToPage(currentPage + 1)"
          >
            Next →
          </button>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import type { PaginatedHealthLogs } from '~/types/pulse'
import { formatAbsoluteTimestamp, humanizeTimestamp } from '~/utils/format'

const config = useRuntimeConfig()
const apiBase = config.public.apiBase as string
const { connectionStatus } = usePulseConnection()

const pageSize = 20
const currentPage = ref(1)
const logs = ref<PaginatedHealthLogs['logs']>([])
const totalCount = ref(0)
const totalPages = ref(0)
const pending = ref(false)

const nowMs = ref(Date.now())
let clockTimer: ReturnType<typeof setInterval> | null = null

const visiblePages = computed(() => {
  const total = Math.max(totalPages.value, 1)
  const current = currentPage.value
  const window = 5
  let start = Math.max(1, current - Math.floor(window / 2))
  let end = Math.min(total, start + window - 1)
  start = Math.max(1, end - window + 1)

  const pages: number[] = []
  for (let i = start; i <= end; i++) pages.push(i)
  return pages
})

function statusBadge(status: string) {
  const offline = status?.toLowerCase() === 'offline'
  return offline
    ? 'bg-rose-500/15 text-rose-300 ring-1 ring-rose-500/25'
    : 'bg-emerald-500/15 text-emerald-300 ring-1 ring-emerald-500/25'
}

async function fetchPage(page: number) {
  if (connectionStatus.value === 'failed') {
    logs.value = []
    totalCount.value = 0
    totalPages.value = 0
    return
  }

  pending.value = true
  try {
    const data = await $fetch<PaginatedHealthLogs>(
      `${apiBase}/api/pulse/logs/paginated`,
      {
        query: { page, pageSize },
      },
    )

    logs.value = data.logs ?? []
    currentPage.value = data.page ?? page
    totalCount.value = data.totalCount ?? 0
    totalPages.value = data.totalPages ?? (
      data.totalCount
        ? Math.ceil(data.totalCount / (data.pageSize || pageSize))
        : 0
    )
  }
  catch {
    logs.value = []
  }
  finally {
    pending.value = false
  }
}

function goToPage(page: number) {
  if (page < 1 || (totalPages.value > 0 && page > totalPages.value)) return
  void fetchPage(page)
}

onMounted(() => {
  clockTimer = setInterval(() => {
    nowMs.value = Date.now()
  }, 1000)

  if (connectionStatus.value === 'connected') {
    void fetchPage(1)
  }
})

watch(connectionStatus, (status) => {
  if (status === 'connected') {
    void fetchPage(currentPage.value || 1)
  }
})

onUnmounted(() => {
  if (clockTimer) clearInterval(clockTimer)
})
</script>
