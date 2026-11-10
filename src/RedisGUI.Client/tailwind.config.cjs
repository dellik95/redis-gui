/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    "./src/**/*.{html,ts,scss}",
  ],
  theme: {
    extend: {
      colors: {
        canvas: {
          DEFAULT: '#090d16',
          subtle: '#0c1322',
          card: '#0f172a',
          hover: '#1e293b',
          border: '#1e293b',
          borderLight: '#334155'
        },
        brand: {
          DEFAULT: '#6366f1',
          hover: '#4f46e5',
          glow: 'rgba(99, 102, 241, 0.25)',
          cyan: '#38bdf8',
          emerald: '#10b981',
          amber: '#f59e0b',
          rose: '#ef4444'
        }
      },
      fontFamily: {
        mono: ['JetBrains Mono', 'Roboto Mono', 'ui-monospace', 'monospace'],
        sans: ['Inter', 'system-ui', 'sans-serif']
      }
    },
  },
  plugins: [],
};
