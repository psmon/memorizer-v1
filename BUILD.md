# Build and Run Memorizer with Docker

This document provides instructions for building and running the Memorizer application using Docker.

---

## 🐳 Build the Docker Image

cd src

docker build -f Memorizer/Dockerfile  -t registry.webnori.com/memorizer:latest .

## Run the Docker Container

docker run -e ASP -p 5000:5000 registry.webnori.com/memorizer:latest