#!/bin/bash
IMAGE_DIR="../mooldang-images"

if [ ! -d "$IMAGE_DIR" ]; then
    echo "Error: $IMAGE_DIR not found."
    exit 1
fi

for tar in "$IMAGE_DIR"/*.tar; do
    echo "Importing $tar..."
    docker load -i "$tar"
done

echo "All images imported successfully."
