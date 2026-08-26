FROM mcr.microsoft.com/playwright:v1.62.0-noble

WORKDIR /work

COPY package.json package-lock.json ./
RUN npm ci

COPY playwright.config.ts ./
COPY specs ./specs

ENTRYPOINT ["npm", "test"]
