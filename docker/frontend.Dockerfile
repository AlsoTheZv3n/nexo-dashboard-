# syntax=docker/dockerfile:1.7

# ---- build ----
FROM node:20-alpine AS build
WORKDIR /app

# pnpm via corepack keeps us aligned with the repo-declared version
RUN corepack enable && corepack prepare pnpm@9.15.0 --activate

COPY frontend/package.json frontend/pnpm-lock.yaml* ./frontend/
WORKDIR /app/frontend
RUN pnpm install --frozen-lockfile

COPY frontend/ ./
RUN pnpm build

# ---- runtime ----
FROM nginx:1.27-alpine AS runtime
COPY docker/nginx-spa.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/frontend/dist /usr/share/nginx/html

EXPOSE 8080
