import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Forwards /api/* to the PizzaShop.Api dev server (see
      // src/PizzaShop.Api/Properties/launchSettings.json, "http" profile) so the frontend
      // can call same-origin paths in dev without needing CORS (ADR-0035). The "frontend"
      // CORS policy in Program.cs still exists for non-proxied scenarios (e.g. a production
      // build served from a different origin than the Api).
      '/api': {
        target: 'http://localhost:5105',
        changeOrigin: true,
      },
    },
  },
})
