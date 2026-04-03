# [오시리스의 대시보드]: Multi-stage Build
# 1. 빌드 단계 (호스트 시스템 Node.js 의존성 제로)
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# 2. 실행 단계 (경량화 오퍼레이션)
FROM node:20-alpine
WORKDIR /app
COPY --from=builder /app/build ./build
COPY --from=builder /app/package*.json ./
# 런타임에 필수적인 의존성만 설치
RUN npm install --only=production
EXPOSE 3000
# [Aegis of Resonance Dashboard Server] 가동
CMD ["node", "build"]
