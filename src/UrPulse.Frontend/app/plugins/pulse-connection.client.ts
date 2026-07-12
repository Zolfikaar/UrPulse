/**
 * Boots the Pulse Core connection engine once on the client.
 * Survives Dashboard ↔ Settings navigation without resetting retries.
 */
export default defineNuxtPlugin(() => {
  const { ensureEngineStarted } = usePulseConnection()
  ensureEngineStarted()
})
