<template>
  <!-- Viewport-locking gate: opaque cover hides any orphaned/layout-leaked cards -->
  <div
    class="fixed inset-0 z-50 flex flex-col bg-[#0B0F19]"
    role="status"
    aria-live="polite"
  >
    <!-- Loading: full-viewport blocking spinner -->
    <div
      v-if="connectionStatus === 'loading'"
      class="flex flex-1 items-center justify-center animate-fade-in"
    >
      <div class="flex flex-col items-center gap-5">
        <div class="relative flex h-28 w-28 items-center justify-center">
          <!-- Outer pulse rings -->
          <span
            class="absolute inset-0 rounded-full border border-cyan-400/20
              bg-gradient-to-br from-cyan-500/10 to-teal-500/5 shadow-glow"
          />
          <span
            class="absolute inset-2 rounded-full border-2 border-transparent
              border-t-cyan-400 border-r-teal-400/60 animate-spin"
            style="animation-duration: 1.1s"
          />
          <span
            class="absolute inset-5 rounded-full border border-cyan-500/30
              animate-pulse-dot"
          />
          <!-- Center attempt counter -->
          <span
            class="relative z-10 font-mono text-lg font-semibold tracking-wide text-cyan-200"
          >
            {{ attemptLabel }}
          </span>
        </div>
        <div class="text-center">
          <p class="text-sm font-medium text-slate-200">Connecting to Ur Pulse Core</p>
          <p class="mt-1 font-mono text-[11px] text-pulse-muted">
            {{ apiBase }}
          </p>
        </div>
      </div>
    </div>

    <!-- Failed: isolated full-width warning -->
    <div
      v-else-if="connectionStatus === 'failed'"
      class="flex flex-1 items-center justify-center animate-fade-in"
    >
      <div
        class="panel w-full max-w-xl px-8 py-16 text-center
          border-rose-500/25 bg-gradient-to-b from-rose-950/30 to-pulse-surface/60 shadow-glow-red"
      >
        <div
          class="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-full
            bg-rose-500/10 ring-1 ring-rose-500/35"
        >
          <span class="relative flex h-3 w-3">
            <span class="absolute inline-flex h-full w-full rounded-full bg-rose-400 animate-pulse-dot" />
            <span class="relative inline-flex h-3 w-3 rounded-full bg-rose-500" />
          </span>
        </div>

        <h2 class="text-xl font-semibold tracking-tight text-white">
          Ur Pulse Core Server is unreachable
        </h2>
        <p class="mt-2 text-sm text-slate-400">
          Please verify that the backend is running.
        </p>
        <p class="mt-4 break-all font-mono text-xs text-cyan-300/80">
          {{ apiBase }}
        </p>
        <p class="mt-2 font-mono text-[11px] text-pulse-muted">
          Exhausted {{ maxRetries }} initial {{ maxRetries === 1 ? 'retry' : 'retries' }}
        </p>

        <button
          type="button"
          class="btn-primary mt-8"
          :disabled="pending"
          @click="emit('retry')"
        >
          {{ pending ? 'Connecting…' : 'Retry Connection Manually' }}
        </button>
      </div>
    </div>
  </div>
</template>


<script setup lang="ts">
import type { ConnectionStatus } from '~/composables/usePulseConnection'

defineProps<{
  connectionStatus: ConnectionStatus
  attemptLabel: string
  apiBase: string
  maxRetries: number
  pending?: boolean
}>()

const emit = defineEmits<{
  retry: []
}>()
</script>
