import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Mirrors nginx.conf's /api proxy so axiosClient's relative baseURL ('/api')
      // reaches the real backend during `npm run dev` too, not just in Docker.
      '/api': {
        target: 'http://localhost:5124',
        changeOrigin: true,
      },
      // SignalR (see usePickTaskRealtime.ts) negotiates over HTTP first, then
      // upgrades to a WebSocket — ws: true is what makes Vite's proxy forward
      // that upgrade instead of dropping the connection after negotiate.
      '/hubs': {
        target: 'http://localhost:5124',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
