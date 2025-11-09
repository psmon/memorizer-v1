# Memorizer MCP Proxy Server

Memorizer MCP 서비스를 외부에 노출하기 위한 프록시 서버입니다. Python Flask 기반으로 구축되었으며, ngrok을 통해 공개 URL로 접근할 수 있습니다.

## 목차
- [필수 요구사항](#필수-요구사항)
- [설치](#설치)
- [설정](#설정)
- [사용 방법](#사용-방법)
- [API 엔드포인트](#api-엔드포인트)
- [문제 해결](#문제-해결)

## 필수 요구사항

시작하기 전에 다음 항목들이 설치되어 있어야 합니다:

- **Python 3.7 이상**
- **pip3** (Python 패키지 관리자)
- **ngrok** (외부 노출용, 선택사항)
- **WSL (Windows Subsystem for Linux)** 또는 Linux 환경

## 설치

### 1. Python 패키지 설치

프록시 서버 실행에 필요한 Python 패키지들을 설치합니다:

```bash
pip3 install flask requests python-dotenv
```

> 참고: 시작 스크립트가 자동으로 누락된 패키지를 설치하므로 이 단계는 선택사항입니다.

### 2. ngrok 설치 (선택사항)

외부에서 접근 가능한 URL이 필요한 경우에만 설치합니다.

#### WSL/Ubuntu에서 ngrok 설치:

```bash
curl -s https://ngrok-agent.s3.amazonaws.com/ngrok.asc | sudo tee /etc/apt/trusted.gpg.d/ngrok.asc >/dev/null
echo "deb https://ngrok-agent.s3.amazonaws.com buster main" | sudo tee /etc/apt/sources.list.d/ngrok.list
sudo apt update && sudo apt install ngrok
```

#### ngrok 설정:

1. [ngrok 웹사이트](https://ngrok.com/)에서 계정 생성
2. [ngrok 대시보드](https://dashboard.ngrok.com/)에서 Auth Token 발급
3. `.env` 파일에 Auth Token 설정 (아래 참조)

## 설정

### 환경 변수 설정

`src/MemorizerProxy/.env` 파일에 다음 내용이 포함되어 있어야 합니다:

```env
ngrok_key=YOUR_NGROK_AUTH_TOKEN
mcp_api_key=YOUR_MCP_API_KEY
```

- `ngrok_key`: ngrok 대시보드에서 발급받은 Auth Token (ngrok 사용 시에만 필요)
- `mcp_api_key`: Memorizer MCP 서비스 API 키

## 사용 방법

### 기본 실행

bash 쉘에서 다음 명령을 실행하여 프록시 서버와 ngrok 터널을 시작합니다:

```bash
cd D:\Code\AI\memorizer-v1\src\MemorizerProxy


cd src/MemorizerProxy
./start-proxy.sh
```

### 포트 지정하여 실행

기본 포트(5000) 대신 다른 포트를 사용하려면:

```bash
./start-proxy.sh --port 8090
```

또는 짧은 옵션:

```bash
./start-proxy.sh -p 8080
```

### ngrok 없이 로컬에서만 실행

외부 노출 없이 로컬에서만 서버를 실행하려면:

```bash
./start-proxy.sh --skip-ngrok
```

### 도움말

사용 가능한 모든 옵션을 보려면:

```bash
./start-proxy.sh --help
```

### 실행 결과

스크립트가 성공적으로 실행되면 다음과 같은 정보가 표시됩니다:

```
================================
Server is running!
================================
Local URL:  http://localhost:5000
Public URL: https://xxxx-xxx-xxx-xxx.ngrok-free.app

SSE Endpoint: https://xxxx-xxx-xxx-xxx.ngrok-free.app/sse
Health Check: https://xxxx-xxx-xxx-xxx.ngrok-free.app/health

Press Ctrl+C to stop the server and ngrok tunnel
================================
```

### 서버 중지

실행 중인 서버를 중지하려면:
- 터미널에서 `Ctrl+C` 키를 누릅니다

## API 엔드포인트

### 1. SSE 엔드포인트
- **URL**: `/sse`
- **메서드**: GET, POST
- **설명**: MCP SSE (Server-Sent Events) 스트림을 프록시합니다
- **예제**:
  ```bash
  curl https://your-ngrok-url.ngrok-free.app/sse
  ```

### 2. Health Check
- **URL**: `/health`
- **메서드**: GET
- **설명**: 서버 상태를 확인합니다
- **응답**:
  ```json
  {
    "status": "healthy",
    "service": "memorizer-proxy"
  }
  ```
- **예제**:
  ```bash
  curl http://localhost:5000/health
  ```

### 3. 루트 엔드포인트
- **URL**: `/`
- **메서드**: GET
- **설명**: 서비스 정보와 사용 가능한 엔드포인트를 반환합니다
- **응답**:
  ```json
  {
    "service": "Memorizer MCP Proxy Server",
    "version": "1.0.0",
    "endpoints": {
      "/sse": "MCP SSE endpoint",
      "/health": "Health check endpoint"
    }
  }
  ```

## MCP 클라이언트 연결

생성된 ngrok URL을 사용하여 MCP 클라이언트를 연결할 수 있습니다:

```python
from agents.mcp import MCPServerStreamableHttp

async def main():
    server = MCPServerStreamableHttp(
        name="MemorizerProxy",
        params={
            "url": "https://your-ngrok-url.ngrok-free.app/sse",
            "headers": {}  # API 키는 프록시에서 자동으로 추가됩니다
        },
    )
    await server.run()
```

## 문제 해결

### 스크립트 실행 권한 오류

```
Permission denied: ./start-proxy.sh
```

**해결 방법**:
```bash
chmod +x start-proxy.sh
./start-proxy.sh
```

### 포트가 이미 사용 중인 경우

```
Error: Port 5000 is already in use
```

**해결 방법**:
- 스크립트가 자동으로 기존 프로세스를 종료합니다
- 또는 다른 포트를 지정하여 실행: `./start-proxy.sh -p 8080`

### Python 패키지가 설치되지 않은 경우

```
Error: Module 'flask' not found
```

**해결 방법**:
- 스크립트가 자동으로 누락된 패키지를 설치합니다
- 또는 수동 설치: `pip3 install flask requests python-dotenv`

### ngrok이 설치되지 않은 경우

```
Error: ngrok is not installed or not in PATH!
```

**해결 방법**:
- ngrok 없이 실행: `./start-proxy.sh --skip-ngrok`
- 또는 ngrok 설치 (위의 설치 섹션 참조)

### ngrok Auth Token이 유효하지 않은 경우

```
Error: Invalid ngrok auth token
```

**해결 방법**:
1. [ngrok 대시보드](https://dashboard.ngrok.com/)에서 새 Auth Token 발급
2. `.env` 파일의 `ngrok_key` 값 업데이트

### 서버가 시작되지 않는 경우

```
Error: Server failed to start
```

**해결 방법**:
1. `.env` 파일이 올바른 위치에 있는지 확인
2. `mcp_api_key`가 올바르게 설정되었는지 확인
3. Python3이 올바르게 설치되었는지 확인: `python3 --version`
4. 로그를 확인하여 상세한 오류 메시지 확인

### ngrok URL을 가져올 수 없는 경우

```
Warning: Could not get ngrok public URL
```

**해결 방법**:
- ngrok이 시작되는 데 시간이 걸릴 수 있습니다
- 브라우저에서 `http://localhost:4040`을 열어 ngrok 대시보드에서 공개 URL 확인

### lsof 명령어를 찾을 수 없는 경우

```
lsof: command not found
```

**해결 방법**:
```bash
sudo apt-get update
sudo apt-get install lsof
```

## 파일 구조

```
src/MemorizerProxy/
├── .env                    # 환경 변수 설정 파일
├── mcp_server.py          # Python Flask 프록시 서버
├── start-proxy.sh         # Bash 시작 스크립트 (WSL/Linux)
├── MCPProxy.md            # 프로젝트 요구사항 문서
└── README.md              # 이 파일
```

## 주의사항

- `.env` 파일에는 민감한 정보(API 키, Auth Token)가 포함되어 있으므로 Git에 커밋하지 마세요
- ngrok 무료 플랜은 동시 연결 제한이 있을 수 있습니다
- 프로덕션 환경에서는 적절한 보안 설정을 추가하는 것을 권장합니다
- WSL 환경에서 실행하며, Windows 네이티브 환경에서는 작동하지 않습니다

## 라이선스

이 프로젝트는 Memorizer v1 프로젝트의 일부입니다.
