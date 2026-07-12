<template>
  <div class="pointer-events-none fixed right-4 top-4 z-[100] flex w-full max-w-sm flex-col gap-2">
    <TransitionGroup name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="pointer-events-auto animate-slide-in rounded-xl border px-4 py-3 shadow-lg backdrop-blur-md"
        :class="toastClass(toast.type)"
      >
        <div class="flex items-start gap-3">
          <span class="mt-0.5 text-lg leading-none" aria-hidden="true">
            {{ toast.type === 'success' ? '✓' : toast.type === 'error' ? '✕' : 'ℹ' }}
          </span>
          <div class="min-w-0 flex-1">
            <p class="text-sm font-semibold text-white">{{ toast.title }}</p>
            <p class="mt-0.5 text-xs text-slate-300">{{ toast.message }}</p>
          </div>
          <button
            type="button"
            class="rounded p-1 text-slate-400 transition hover:bg-white/10 hover:text-white"
            aria-label="Dismiss"
            @click="dismiss(toast.id)"
          >
            ✕
          </button>
        </div>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup lang="ts">
import type { ToastType } from '~/types/pulse'

const { toasts, dismiss } = useToast()

function toastClass(type: ToastType) {
  switch (type) {
    case 'success':
      return 'border-emerald-500/40 bg-emerald-950/90'
    case 'error':
      return 'border-rose-500/40 bg-rose-950/90'
    default:
      return 'border-cyan-500/40 bg-slate-900/90'
  }
}
</script>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}
.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateX(1rem);
}
</style>
