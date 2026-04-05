# [물멍]: Studio 8.0 차세대 빌드 엔진 (Vite 8 + Script 6 전용)
FROM node:22-alpine AS builder

WORKDIR /app

# [오시리스의 열쇠]: 의존성 레이어 최적화
# COPY root level lock if exists
COPY package.json ./
# COPY MooldangBot.Studio/package.json ./MooldangBot.Studio/

# 강제 정화 및 설치 (Vite 8/Svelte 5 환경)
RUN npm config set fetch-retry-mintimeout 20000 && \
    npm config set fetch-retry-maxtimeout 120000

WORKDIR /app/MooldangBot.Studio
COPY MooldangBot.Studio/package.json ./
RUN npm install --legacy-peer-deps

# 소스 복사 및 빌드
COPY MooldangBot.Studio/ ./
RUN npm run build

# 실행 레이어
FROM node:22-alpine

WORKDIR /app
COPY --from=builder /app/MooldangBot.Studio/build ./build
COPY --from=builder /app/MooldangBot.Studio/package.json ./
RUN npm install --production --legacy-peer-deps

EXPOSE 3000
ENV NODE_ENV=production
ENV PORT=3000

CMD ["node", "build"]
