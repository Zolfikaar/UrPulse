<template>
  <button
    type="button"
    role="switch"
    :aria-checked="modelValue"
    class="relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full transition-colors duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-pulse-cyan/40"
    :class="modelValue ? 'bg-gradient-to-r from-cyan-600 to-teal-500' : 'bg-slate-700'"
    @click="emit('update:modelValue', !modelValue)"
  >
    <span
      class="inline-block h-5 w-5 transform rounded-full bg-white shadow transition-transform duration-200"
      :class="modelValue ? 'translate-x-5' : 'translate-x-0.5'"
    />
  </button>
</template>

<script setup lang="ts">
defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()
</script>
