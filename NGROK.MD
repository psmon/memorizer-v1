# Ngrok을 이용한 HTTPS 터널링 가이드 (Windows)

로컬 개발 서버를 HTTPS로 외부에 노출하여 ChatGPT MCP 연동 등을 테스트할 수 있습니다.

## 사전 준비

### 1. Ngrok 설치

**방법 1: Chocolatey 사용**
```powershell
choco install ngrok
```

**방법 2: 직접 다운로드**
1. https://ngrok.com/download 에서 Windows 버전 다운로드
2. 압축 해제 후 원하는 경로에 배치 (예: `C:\Tools\ngrok`)
3. 시스템 PATH에 추가하거나 해당 경로에서 실행

### 2. Ngrok 계정 설정

1. https://ngrok.com 에서 무료 계정 생성
2. Dashboard에서 Authtoken 복사
3. 터미널에서 인증 설정:
```powershell
ngrok config add-authtoken YOUR_AUTH_TOKEN
```

## 실행 방법

### 기본 실행

```powershell
# HTTP 터널링 (localhost:5013 -> HTTPS)
ngrok http 5013

ngrok http 192.168.0.13:5013
```

### 고정 도메인 사용 (유료 플랜)

```powershell
ngrok http 5013 --domain=your-custom-domain.ngrok.io
```

### 실행 결과 예시

```
Session Status                online
Account                       your-email@example.com
Version                       3.x.x
Region                        Japan (jp)
Web Interface                 http://127.0.0.1:4040
Forwarding                    https://abc123.ngrok-free.app -> http://localhost:5013
```

## 배치 스크립트

### start-ngrok.bat

```batch
@echo off
echo Starting ngrok tunnel for localhost:5013...
echo.
echo Press Ctrl+C to stop
echo.
ngrok http 5013
```

### start-ngrok.ps1 (PowerShell)

```powershell
Write-Host "Starting ngrok tunnel for localhost:5013..." -ForegroundColor Green
Write-Host ""
Write-Host "Ngrok Web Interface: http://127.0.0.1:4040" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

ngrok http 5013
```

## ChatGPT MCP 연동 시 설정

Ngrok 실행 후 표시되는 HTTPS URL을 사용합니다:

### OAuth 엔드포인트
- Token: `https://abc123.ngrok-free.app/oauth/token`
- Authorize: `https://abc123.ngrok-free.app/oauth/authorize`
- Well-known: `https://abc123.ngrok-free.app/.well-known/oauth-authorization-server`

### MCP 엔드포인트
- SSE (레거시): `https://abc123.ngrok-free.app/sse`
- Streamable HTTP: `https://abc123.ngrok-free.app/` (POST)
- Message: `https://abc123.ngrok-free.app/message` (POST)

## 주의사항

1. **무료 플랜 제한**
   - URL이 매번 변경됨
   - 동시 연결 수 제한
   - 대역폭 제한

2. **appsettings 설정**
   - `Server.CanonicalUrl`을 ngrok URL로 변경하면 OAuth 메타데이터가 올바른 URL 반환
   ```json
   "Server": {
     "CanonicalUrl": "https://abc123.ngrok-free.app"
   }
   ```

3. **보안**
   - 테스트 후 ngrok 종료
   - 민감한 데이터 노출 주의
   - API Key/Secret 공유 금지

## 트러블슈팅

### ERR_NGROK_108: Tunnel session failed
```powershell
# Authtoken 재설정
ngrok config add-authtoken YOUR_AUTH_TOKEN
```

### 연결이 느린 경우
```powershell
# 가까운 리전 선택
ngrok http 5013 --region=ap  # Asia Pacific
ngrok http 5013 --region=jp  # Japan
```

### 로컬 서버 연결 확인
```powershell
# 먼저 로컬 서버가 동작하는지 확인
curl http://localhost:5013/healthz
```
