import type { ToastMessage, ToastType } from '~/types/pulse'

const toasts = ref<ToastMessage[]>([])
let toastSeq = 0

export function useToast() {
  function push(type: ToastType, title: string, message: string, durationMs = 3500) {
    const id = ++toastSeq
    toasts.value.push({ id, type, title, message })

    if (import.meta.client) {
      window.setTimeout(() => dismiss(id), durationMs)
    }
  }

  function dismiss(id: number) {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  function success(title: string, message: string) {
    push('success', title, message)
  }

  function error(title: string, message: string) {
    push('error', title, message)
  }

  function info(title: string, message: string) {
    push('info', title, message)
  }

  return {
    toasts: readonly(toasts),
    push,
    dismiss,
    success,
    error,
    info,
  }
}
