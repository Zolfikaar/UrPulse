/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './app/components/**/*.{js,vue,ts}',
    './app/layouts/**/*.vue',
    './app/pages/**/*.vue',
    './app/plugins/**/*.{js,ts}',
    './app/app.vue',
    './app/error.vue',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Outfit', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'monospace'],
      },
      colors: {
        pulse: {
          bg: '#0a0e17',
          surface: '#111827',
          border: '#1e293b',
          muted: '#64748b',
          cyan: '#22d3ee',
          emerald: '#34d399',
          rose: '#f43f5e',
          amber: '#fbbf24',
        },
      },
      boxShadow: {
        glow: '0 0 24px rgba(34, 211, 238, 0.15)',
        'glow-green': '0 0 20px rgba(52, 211, 153, 0.35)',
        'glow-red': '0 0 20px rgba(244, 63, 94, 0.35)',
      },
      keyframes: {
        'pulse-dot': {
          '0%, 100%': { opacity: '1', transform: 'scale(1)' },
          '50%': { opacity: '0.45', transform: 'scale(1.35)' },
        },
        'fade-in': {
          '0%': { opacity: '0', transform: 'translateY(8px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        'slide-in': {
          '0%': { opacity: '0', transform: 'translateX(16px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
      },
      animation: {
        'pulse-dot': 'pulse-dot 1.6s ease-in-out infinite',
        'fade-in': 'fade-in 0.4s ease-out',
        'slide-in': 'slide-in 0.35s ease-out',
      },
    },
  },
  plugins: [],
}
