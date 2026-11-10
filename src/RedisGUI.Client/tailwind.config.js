/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ['class', '[data-theme]'],
  content: [
    "./src/**/*.{html,ts,scss}",
  ],
  theme: {
    extend: {
      colors: {
        canvas: {
          DEFAULT: 'var(--color-canvas)',
          subtle: 'var(--color-canvas-subtle)',
          card: 'var(--color-card)',
          hover: 'var(--color-card-hover)',
          border: 'var(--color-border)',
          borderLight: 'var(--color-border-light)',
          text: 'var(--color-text)',
          muted: 'var(--color-text-muted)',
        },
        brand: {
          DEFAULT: 'var(--color-accent)',
          hover: 'var(--color-accent-hover)',
          glow: 'var(--color-accent-glow)',
          cyan: '#38bdf8',
          emerald: '#10b981',
          amber: '#f59e0b',
          rose: '#ef4444',
          purple: '#a855f7',
        }
      },
      fontFamily: {
        mono: ['JetBrains Mono', 'Roboto Mono', 'ui-monospace', 'monospace'],
        sans: ['Inter', 'Roboto', 'system-ui', 'sans-serif']
      }
    },
  },
  plugins: [],
};
