<template>
  <div class="flex items-center gap-2.5 rounded-lg border border-pulse-border/60 bg-pulse-bg/50 px-3 py-2">
    <span class="relative flex h-2.5 w-2.5">
      <span
        class="absolute inline-flex h-full w-full rounded-full opacity-75 animate-pulse-dot"
        :class="isOnline ? 'bg-pulse-emerald' : 'bg-pulse-rose'"
      />
      <span
        class="relative inline-flex h-2.5 w-2.5 rounded-full"
        :class="isOnline ? 'bg-pulse-emerald' : 'bg-pulse-rose'"
      />
    </span>
    <div class="min-w-0">
      <p class="text-[11px] font-medium uppercase tracking-wider" :class="isOnline ? 'text-pulse-emerald' : 'text-pulse-rose'">
        {{ isOnline ? 'Core Online' : 'Core Offline' }}
      </p>
      <p class="truncate font-mono text-[10px] text-pulse-muted">
        <template v-if="isOnline && latencyMs != null">{{ latencyMs }}ms ping</template>
        <template v-else>unreachable</template>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  isOnline: boolean
  latencyMs: number | null
}>()
</script>
