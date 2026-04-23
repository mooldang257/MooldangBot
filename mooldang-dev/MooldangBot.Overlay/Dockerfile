# [오시리스의 무대]: Svelte + Vite Multi-stage Build
# 1. 빌드 단계
FROM node:20-alpine AS builder
WORKDIR /app/MooldangBot.Overlay
COPY MooldangBot.Overlay/package*.json ./
RUN npm install --legacy-peer-deps
COPY MooldangBot.Overlay/ .
RUN npm run build

# 2. 실행 단계 (리버스 프록시 노드로 동작)
FROM nginx:alpine
# 빌드된 정적 결과물 복사
COPY --from=builder /app/MooldangBot.Overlay/dist /usr/share/nginx/html/overlay
# Nginx 단일 경로 서빙을 위한 간단한 설정 (Overwrite default)
RUN echo 'server { listen 80; location /overlay/ { alias /usr/share/nginx/html/overlay/; index index.html; try_files $uri $uri/ /overlay/index.html; } }' > /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
