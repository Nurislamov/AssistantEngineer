FROM node:22-alpine AS build
WORKDIR /app

COPY src/Frontend/package.json src/Frontend/package-lock.json ./
RUN npm ci
COPY src/Frontend/ ./
ARG VITE_API_BASE_URL=http://localhost/
ARG VITE_API_VERSION=1
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_API_VERSION=$VITE_API_VERSION
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY deploy/docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
