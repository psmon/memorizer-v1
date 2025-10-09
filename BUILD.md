# Build and Run Memorizer with Docker

This document provides instructions for building and running the Memorizer application using Docker.

---

## 🐳 Build the Docker Image


```
docker build -f src/Memorizer/Dockerfile  -t registry.webnori.com/memorizer:latest .

docker push registry.webnori.com/memorizer:latest
```


## 🐳 Build the DockerHub Image

```
docker build --no-cache -f src/Memorizer/Dockerfile -t psmon/mcp-memorizer:build .

docker tag psmon/mcp-memorizer:build psmon/mcp-memorizer:latest
docker push psmon/mcp-memorizer:latest

// Common ver
docker tag psmon/mcp-memorizer:build psmon/mcp-memorizer:v1.1.7
c

```


## Proxy Configuration
```
# location 컨텍스트에서 유효
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;

# SSE는 스트림을 즉시 흘려보내야 함
proxy_buffering off;
proxy_request_buffering off;
proxy_cache off;

# 압축/청크가 스트림을 묶지 않도록
gzip off;
proxy_set_header Accept-Encoding "";

# 커넥션/타임아웃
proxy_http_version 1.1;
proxy_read_timeout 1h;
proxy_send_timeout 1h;

# Nginx가 헤더를 재작성하지 않도록(일부 환경에서 유용)
proxy_set_header Connection "";
```