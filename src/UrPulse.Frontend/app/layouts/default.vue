<template>
  <div class="flex min-h-screen">
    <aside
      class="fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-pulse-border/70
        bg-gradient-to-b from-slate-950/95 via-pulse-surface/90 to-pulse-bg/95 backdrop-blur-xl"
    >
      <div class="border-b border-pulse-border/60 px-5 py-6">
        <NuxtLink to="/" class="group flex items-center gap-3">
          <div
            class="flex h-10 w-10 items-center justify-center rounded-xl
              bg-gradient-to-br from-cyan-500/30 to-teal-600/20
              ring-1 ring-cyan-400/30 shadow-glow transition group-hover:shadow-glow-green"
          >
            <svg class="h-5 w-5 text-pulse-cyan" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path
                d="M3 12h3l2-6 4 12 2-6h7"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </div>
          <div>
            <p class="text-[10px] font-semibold uppercase tracking-[0.25em] text-pulse-cyan/80">
              UrLabs
            </p>
            <p class="text-lg font-semibold tracking-tight text-white">
              Ur Pulse
            </p>
          </div>
        </NuxtLink>
      </div>

      <div class="px-4 pt-4">
        <HealthPingIndicator :is-online="isOnline" :latency-ms="latencyMs" />
      </div>

      <nav class="mt-6 flex flex-1 flex-col gap-1 px-3" aria-label="Main">
        <NuxtLink
          to="/"
          class="nav-link"
          :class="{ 'nav-link-active': route.path === '/' }"
        >
          <svg class="h-4 w-4 shrink-0 opacity-80" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <rect x="3" y="3" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.75" />
            <rect x="14" y="3" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.75" />
            <rect x="3" y="14" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.75" />
            <rect x="14" y="14" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.75" />
          </svg>
          Dashboard
        </NuxtLink>

        <NuxtLink
          to="/logs"
          class="nav-link"
          :class="{ 'nav-link-active': route.path.startsWith('/logs') }"
        >
          <svg class="h-4 w-4 shrink-0 opacity-80" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path
              d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"
              stroke="currentColor"
              stroke-width="1.75"
              stroke-linecap="round"
            />
          </svg>
          Log History
        </NuxtLink>

        <NuxtLink
          to="/settings"
          class="nav-link"
          :class="{ 'nav-link-active': route.path.startsWith('/settings') }"
        >
          <svg class="h-4 w-4 shrink-0 opacity-80" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path
              d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z"
              stroke="currentColor"
              stroke-width="1.75"
            />
            <path
              d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1Z"
              stroke="currentColor"
              stroke-width="1.5"
              stroke-linejoin="round"
            />
          </svg>
          Settings Vault
        </NuxtLink>
      </nav>

      <div class="border-t border-pulse-border/60 px-5 py-4">
        <p class="font-mono text-[10px] text-pulse-muted">
          Studio Ecosystem · v1.0
        </p>
      </div>
    </aside>

    <div class="flex min-h-screen flex-1 flex-col pl-64">
      <header class="sticky top-0 z-30 border-b border-pulse-border/50 bg-pulse-bg/70 backdrop-blur-md">
        <div class="flex items-center justify-between px-8 py-4">
          <div>
            <h1 class="text-lg font-semibold tracking-tight text-white">
              {{ pageTitle }}
            </h1>
            <p class="text-xs text-pulse-muted">
              {{ pageSubtitle }}
            </p>
          </div>
          <div class="hidden items-center gap-2 sm:flex">
            <span class="rounded-md border border-pulse-border/60 bg-pulse-surface/50 px-2.5 py-1 font-mono text-[10px] text-pulse-muted">
              API {{ apiBase }}
            </span>
          </div>
        </div>
      </header>

      <main class="flex-1 px-8 py-6">
        <slot />
      </main>
    </div>

    <ToastHost />
  </div>
</template>

<script setup lang="ts">
const route = useRoute()
const config = useRuntimeConfig()
const { isOnline, latencyMs } = useApiHealth(5000)

const apiBase = computed(() => config.public.apiBase as string)

const pageTitle = computed(() => {
  if (route.path.startsWith('/settings')) return 'System Settings'
  if (route.path.startsWith('/logs')) return 'Log History'
  return 'Live Dashboard'
})

const pageSubtitle = computed(() => {
  if (route.path.startsWith('/settings')) {
    return 'Global system tuning and unified alerting configuration'
  }
  if (route.path.startsWith('/logs')) {
    return 'Paginated historical health transitions across all monitored apps'
  }
  return 'Real-time health monitoring across UrLabs services'
})
</script>
