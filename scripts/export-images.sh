#!/bin/bash
IMAGE_DIR="../mooldang-images"
IMAGES=(
    "mooldang-app:latest"
    "mooldang-chzzk-bot:latest"
    "mooldang-studio:latest"
    "mooldang-overlay:latest"
    "mooldang-admin:latest"
)

mkdir -p "$IMAGE_DIR"

for img in "${IMAGES[@]}"; do
    FILENAME="$(echo $img | sed 's/:/-/g').tar"
    echo "Exporting $img to $IMAGE_DIR/$FILENAME..."
    docker save -o "$IMAGE_DIR/$FILENAME" "$img"
done

echo "All images exported successfully to $IMAGE_DIR"
