# syntax=docker/dockerfile:1

# Multi-stage build for both frontend apps, served by one nginx container under one
# public port: warehouse-client at / and TestOrderGenerator (the simulated upstream
# ERP/marketplace order feed) at /inbound/. Build context is expected to be Frontend/
# (see docker-compose.yml: services.frontend.build.context) — a parent of both, so a
# single Dockerfile can COPY each subproject independently.

# ---------------------------------------------------------------------------
# Stage 1: build warehouse-client
# ---------------------------------------------------------------------------
# glibc (-slim), not -alpine: Vite 8's bundler (Rolldown, @rolldown/binding-*) ships
# per-platform native binaries as optional deps, and its linux-x64-musl variant
# resolves to a different @emnapi/core/@emnapi/runtime version than what a
# non-Alpine-generated lockfile records, which makes `npm ci` fail with "Missing:
# @emnapi/core@... from lock file" on Alpine specifically. The final stage below
# stays on nginx:alpine — it never runs Node, so musl isn't a factor there.
FROM node:24-slim AS build-client
WORKDIR /app

# package*.json first so `npm ci` gets its own cached layer, same reasoning as the
# backend's restore-before-copy step.
COPY warehouse-client/package.json warehouse-client/package-lock.json ./
RUN npm ci

COPY warehouse-client/ .

# "npm run build" is `tsc -b && vite build` (see package.json) — type errors fail
# the image build here rather than surfacing at runtime in the browser.
RUN npm run build

# ---------------------------------------------------------------------------
# Stage 2: build TestOrderGenerator (the simulated inbound order feed)
# ---------------------------------------------------------------------------
FROM node:24-slim AS build-feed
WORKDIR /app

COPY TestOrderGenerator/package.json TestOrderGenerator/package-lock.json ./
RUN npm ci

COPY TestOrderGenerator/ .

# vite.config.ts sets base: '/inbound/' for this exact build command (`vite build`),
# so its asset URLs resolve correctly once served from that subpath below.
RUN npm run build

# ---------------------------------------------------------------------------
# Stage 3: serve both with nginx
# ---------------------------------------------------------------------------
FROM nginx:alpine AS final

# Both apps call the backend via a relative /api path, same-origin — nginx proxies
# that to the backend container (see nginx.conf). Neither app talks to the backend
# directly, so this stage only needs to serve two static SPA builds.
COPY --from=build-client /app/dist /usr/share/nginx/html
COPY --from=build-feed /app/dist /usr/share/nginx/html/inbound
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
