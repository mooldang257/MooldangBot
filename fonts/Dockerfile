FROM nginx:alpine

# Nginx 설정 복사
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 폰트 파일들을 정적 경로로 복사
COPY . /usr/share/nginx/html/fonts/

# 불필요한 설정 파일(Dockerfile 등)은 이미지에서 제외되도록 처리
RUN rm -f /usr/share/nginx/html/fonts/Dockerfile
