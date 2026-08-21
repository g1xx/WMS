import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ command }) => ({
  plugins: [react()],
  // Served under /inbound/ by nginx in production (see nginx.conf) so its asset URLs
  // resolve correctly from that subpath; the dev server itself still serves from root.
  base: command === 'build' ? '/inbound/' : '/',
  server: {
    port: 5175,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5124',
        changeOrigin: true,
      },
    },
  },
}))
