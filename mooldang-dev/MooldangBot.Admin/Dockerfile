# [물멍]: Admin 함대 관제 센터 빌드 엔진
FROM node:22-alpine AS builder

WORKDIR /app

# [오시리스의 열쇠]: 의존성 레이어 최적화
COPY package.json ./

# 강제 정화 및 설치 (Svelte 5 환경)
RUN npm config set fetch-retry-mintimeout 20000 && \
    npm config set fetch-retry-maxtimeout 120000

WORKDIR /app/MooldangBot.Admin
COPY MooldangBot.Admin/package.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm \
    npm install --legacy-peer-deps

# 소스 복사 및 빌드
COPY MooldangBot.Admin/ ./
RUN npm run build

# 실행 레이어
FROM node:22-alpine

WORKDIR /app
COPY --from=builder /app/MooldangBot.Admin/build ./build
COPY --from=builder /app/MooldangBot.Admin/package.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm \
    npm install --production --legacy-peer-deps

EXPOSE 3000
ENV NODE_ENV=production
ENV PORT=3000
# 👇 [핵심 추가]: Nginx가 도커 네트워크를 타고 들어올 수 있도록 모든 IP 대역 개방
ENV HOST=0.0.0.0

CMD ["node", "build"]
