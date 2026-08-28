FROM node:24.19.0-bookworm-slim AS dependencies

WORKDIR /app

COPY package.json package-lock.json .npmrc ./
RUN test "$(npm --version)" = "11.17.0" && npm ci

FROM dependencies AS test

COPY angular.json eslint.config.js vitest.config.ts tsconfig.json tsconfig.app.json tsconfig.spec.json ./
COPY public ./public
COPY src ./src

CMD ["sh", "-ec", "npm run lint && npm test && npm run build"]

FROM dependencies AS build

COPY angular.json tsconfig.json tsconfig.app.json ./
COPY public ./public
COPY src ./src

RUN npm run build

FROM nginx:1.30.4-alpine3.24-slim

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/user-profile-web/browser /usr/share/nginx/html

EXPOSE 8080
