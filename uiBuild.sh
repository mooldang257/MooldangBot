git pull
docker compose build studio overlay
docker compose up -d --no-deps app studio migration overlay nginx
