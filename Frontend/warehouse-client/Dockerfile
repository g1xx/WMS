# syntax=docker/dockerfile:1

# Multi-stage build for warehouse-client (Vite + React 19, see package.json).
# Build context is expected to be Frontend/warehouse-client/ (see docker-compose.yml:
# services.frontend.build.context).

# ---------------------------------------------------------------------------
# Stage 1: build static assets
# ---------------------------------------------------------------------------
# glibc (-slim), not -alpine: Vite 8's bundler (Rolldown, @rolldown/binding-*)
# ships per-platform native binaries as optional deps, and its linux-x64-musl
# variant resolves to a different @emnapi/core/@emnapi/runtime version than
# what a non-Alpine-generated lockfile records, which makes `npm ci` fail with
# "Missing: @emnapi/core@... from lock file" on Alpine specifically. The final
# stage below stays on nginx:alpine — it never runs Node, so musl isn't a factor there.
FROM node:24-slim AS build
WORKDIR /app

# package*.json first so `npm ci` gets its own cached layer, same reasoning as the
# backend's restore-before-copy step.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# "npm run build" is `tsc -b && vite build` (see package.json) — type errors fail
# the image build here rather than surfacing at runtime in the browser.
RUN npm run build

# ---------------------------------------------------------------------------
# Stage 2: serve with nginx
# ---------------------------------------------------------------------------
FROM nginx:alpine AS final

# axiosClient.ts's baseURL is a compile-time constant (http://localhost:5124/api),
# so the browser calls the backend directly rather than through nginx — this stage
# only needs to serve the static SPA build, no reverse-proxy config to the backend.
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
