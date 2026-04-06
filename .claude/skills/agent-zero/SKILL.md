---
name: agent-zero
description: |
  윈도우 데스크톱을 제어하는 자동화 스킬. AgentZero CLI(AgentZero.ps1)를 통해 마우스 이동/클릭, 키보드 입력, 윈도우 정보 조회, 텍스트 캡처, 스크롤 캡처, 스크린샷, 윈도우 활성화, 내장 터미널 제어, AgentBot 채팅 등을 수행한다.
  다음 상황에서 반드시 이 스킬을 사용할 것:
  - "에이전트제로(AgentZero)를 이용해 OOO 작업을 수행해줘"
  - "윈도우 제어해줘", "마우스 클릭해줘", "키보드 입력해줘"
  - "화면 캡처해줘", "스크롤 캡처해줘", "윈도우 텍스트 추출해줘"
  - "특정 윈도우 찾아줘", "윈도우 목록 보여줘"
  - "스크린샷 찍어줘", "화면 캡처해서 좌표 알려줘", "DPI 확인해줘"
  - "윈도우 활성화해줘", "최소화된 창 복원해줘"
  - "팀즈/슬랙/카카오톡 채팅 내용 가져와", "최근 대화 캡처해줘"
  - "OOO 버튼 찾아줘", "OOO 요소 위치 알려줘", "UI 트리 보여줘", "엘리먼트 검색해줘"
  - "터미널에 명령 보내줘", "터미널 목록 보여줘", "터미널에 입력해줘", "터미널에 엔터키 보내줘", "특수키 보내줘"
  - "AgentBot에 메시지 보내줘", "봇에 채팅 보내줘"
  - 데스크톱 자동화, GUI 조작, 윈도우 정보 수집, 스크린샷 기반 좌표 추정이 필요한 모든 상황
  - UI 요소/컴포넌트를 이름으로 찾아야 하는 상황 (스크린샷 없이 element-tree로 직접 탐색 가능)
---

# AgentZero - Windows Desktop Automation Skill

AgentZero는 Windows 데스크톱을 CLI로 제어하는 도구다. 모든 기능이 CLI에서 직접 동작하며 GUI 없이 사용한다.

PATH에 등록되어 있으므로 `AgentZero.ps1 <command>` 형태로 바로 실행한다.

각 명령어는 `scripts/` 디렉토리에 개별 PowerShell 스크립트로도 제공된다. 스크립트는 파라미터 검증과 타입 지정이 포함되어 있어 직접 호출하기 편리하다.

## 스크립트 실행 방법

```powershell
# 직접 호출 (AgentZero.ps1 래핑)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/get-window-info.ps1

# 또는 AgentZero.ps1 직접 호출 (동일 결과)
AgentZero.ps1 get-window-info
```

## 명령어 레퍼런스

### 윈도우 정보 조회

```powershell
# 모든 보이는 윈도우 목록 (Handle, Class, Title, Rect, Process, PID 등)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/get-window-info.ps1

# 2패널 레이아웃(3:7) 분석 - 좌측/우측 패널 좌표 및 중심점 반환
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/wininfo-layout.ps1 -Hwnd <hwnd>

# 최소화/백그라운드 윈도우를 복원하고 포그라운드로 가져오기
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/activate.ps1 -Hwnd <hwnd>

# AgentZero 상태 조회
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/status.ps1
```

### 마우스 제어

```powershell
# 마우스 이동
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/mousemove.ps1 -X 500 -Y 300

# 좌클릭
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/mouseclick.ps1 -X 500 -Y 300

# 우클릭
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/mouseclick.ps1 -X 500 -Y 300 -Right

# 스크롤 (양수=위, 음수=아래)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/mousewheel.ps1 -X 500 -Y 300 -Delta -5
```

### 키보드 제어

```powershell
# 텍스트 입력 (유니코드/한글)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/keypress.ps1 Hello World

# 특수키
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/keypress.ps1 -Key tab

# 키 조합
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/keypress.ps1 -Key ctrl+a
```
지원 키: tab, enter, esc, space, backspace, delete, insert, home, end, pageup, pagedown, up, down, left, right, f1-f12, capslock, pause, win

### 텍스트 캡처 (CLI 전용, GUI 불필요)

```powershell
# 빠른 텍스트 캡처 — 윈도우의 현재 보이는 텍스트를 stdout으로 출력
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/text-capture.ps1 -Hwnd <hwnd>
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/text-capture.ps1 -Hwnd <hwnd> -PickX 500 -PickY 400

# 스크롤 캡처 — 자동 스크롤하며 텍스트 수집, 5초간 새 텍스트 없으면 자동 종료
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/scroll-capture.ps1 -Hwnd <hwnd>
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/scroll-capture.ps1 -Hwnd <hwnd> -Start 2026-03-31 -End 2026-04-02
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/scroll-capture.ps1 -Hwnd <hwnd> -Max 9999
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/scroll-capture.ps1 -Hwnd <hwnd> -PickX 500 -PickY 400 -Delta 5
```

**scroll-capture 옵션:**
- `-Start DATE` : 시작 날짜 필터 (이보다 오래된 콘텐츠에서 정지)
- `-End DATE` : 종료 날짜 필터 (이보다 새로운 콘텐츠 제외)
- `-Delay N` : 스크롤 간격 ms (기본 200)
- `-Max N` : 최대 스크롤 횟수 (기본 500)
- `-Delta N` : 휠 배수 (기본 3)
- `-PickX X -PickY Y` : 스크롤 타겟 좌표 (기본: 윈도우 중심)

캡처된 텍스트는 stdout으로 출력되며, `logs/scrap/`에도 자동 저장된다.

### UI 요소 트리 탐색 (element-tree)

스크린샷 없이 윈도우 내부의 UI 요소(버튼, 텍스트, 입력란 등)를 이름과 타입으로 직접 탐색한다.

```powershell
# 전체 요소 트리 출력
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/element-tree.ps1 -Hwnd <hwnd>

# 키워드로 요소 검색
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/element-tree.ps1 -Hwnd <hwnd> -Search "검색어"

# 트리 깊이 제한
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/element-tree.ps1 -Hwnd <hwnd> -Depth 10
```

**출력 형식:**
```
[Window] "앱 제목" @(0,0) [1280x720]
  [Pane] @(0,40) [1280x680]
    [Button] "뒤로" @(10,50) [48x47]
    [ComboBox] "검색(Ctrl+E)" @(200,50) [506x49]
```

- **`@(x,y)`는 물리 픽셀 절대 좌표** — mouseclick에 직접 사용 가능
- 클릭 좌표 계산: `click_x = x + width/2`, `click_y = y + height/2` (요소 중심)
- 프레임워크 자동 감지: Flutter, Electron/Chromium, Native

### 터미널 제어 (AgentZero 내장 터미널)

AgentZero WPF 앱 내부에 포함된 멀티 터미널(그룹 x N개 탭)을 CLI에서 원격 제어한다.

```powershell
# 활성 터미널 세션 목록 조회 (group_index, tab_index, title, hwnd, session_id)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-list.ps1

# 특정 터미널에 명령 전송 (텍스트 + Enter)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-send.ps1 -GroupIndex 0 -TabIndex 0 ls -la
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-send.ps1 -GroupIndex 0 -TabIndex 1 git status

# 터미널에 특수키 전송 (Enter, ESC, Tab, 화살표 등)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-key.ps1 -GroupIndex 0 -TabIndex 0 -Key cr
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-key.ps1 -GroupIndex 0 -TabIndex 0 -Key esc
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-key.ps1 -GroupIndex 0 -TabIndex 0 -Key tab
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-key.ps1 -GroupIndex 0 -TabIndex 0 -Key ctrlc

# 터미널 콘솔 출력 텍스트 읽기 (ANSI 코드 제거된 클린 텍스트)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-read.ps1 -GroupIndex 0 -TabIndex 0
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-read.ps1 -GroupIndex 0 -TabIndex 0 -Last 2000
```

**terminal-key 지원 키 목록:**
| Key | 설명 | 용도 |
|-----|------|------|
| `cr` | Carriage Return (\r) | Enter 키 — 명령 제출 |
| `lf` | Line Feed (\n) | 줄바꿈 |
| `crlf` | CR+LF (\r\n) | Windows식 Enter |
| `esc` | Escape | 취소, TUI 메뉴 닫기 |
| `tab` | Tab | 자동완성, 포커스 이동 |
| `backspace` | Backspace | 문자 삭제 |
| `del` | Delete | 앞쪽 문자 삭제 |
| `ctrlc` | Ctrl+C | 프로세스 중단 |
| `ctrld` | Ctrl+D | EOF / 세션 종료 |
| `up/down/left/right` | 화살표 키 | 커서 이동, 히스토리 탐색 |
| `hex:XX` | Raw hex byte | 임의 바이트 전송 (예: hex:0D) |

**terminal-key가 필요한 상황:**
- `terminal-send`로 텍스트를 보냈는데 TUI에서 Enter가 인식되지 않을 때 (특히 OpenAI Codex TUI). Claude Code TUI는 `terminal-send`의 기본 `\r`로 잘 동작하지만, Codex 같은 일부 TUI는 텍스트와 Enter를 별도로 보내야 한다.
- 실행 중인 프로세스를 중단(Ctrl+C)하거나, TUI 메뉴에서 ESC로 나가거나, Tab으로 자동완성을 트리거할 때.

**터미널 제어 워크플로우:**
1. `terminal-list`로 활성 세션 목록 확인 → group_index, tab_index, hwnd 획득
2. `terminal-send <group> <tab> <command>`로 명령 전송
3. 만약 TUI에서 Enter가 인식되지 않으면 `terminal-key <group> <tab> cr`로 별도 Enter 전송
4. `terminal-read <group> <tab>`로 터미널 출력 텍스트 읽기 (`--last N`으로 최근 N자만)
5. 터미널이 미시작 상태이면 AgentZero에서 탭을 활성화한 후 재시도

**TUI별 Enter 키 동작 차이:**
- **Claude Code**: `terminal-send`만으로 텍스트 + Enter 한 번에 동작 (기본 `\r` 사용)
- **OpenAI Codex**: `terminal-send`(텍스트만) + `terminal-key cr`(별도 Enter) 2단계 필요. 긴 한글 텍스트에서 `\r`이 개행으로만 처리되고 제출되지 않는 현상이 있음
- **일반 셸 (bash, PowerShell, cmd)**: `terminal-send`만으로 동작

### AgentBot 채팅

AgentBot 창에 외부에서 채팅 메시지를 보낸다. 자동화 스크립트의 진행 상황이나 알림을 사용자에게 전달할 때 유용하다.

```powershell
# 메시지 전송 (기본 발신자: CLI)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/bot-chat.ps1 Hello from automation

# 발신자 이름 지정
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/bot-chat.ps1 -From CI Build finished successfully
```

AgentBot 창이 열려 있어야 한다. 닫혀 있으면 AgentZero에서 Bot 버튼을 클릭하여 열 것.

### 행동 로그

```powershell
# 최근 행동 이력 조회
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/log.ps1

# 최근 10건
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/log.ps1 -Last 10

# 오래된 이력 정리
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/log.ps1 -Clear
```

### DPI 및 좌표 매핑

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/dpi.ps1
```

### 스크린샷

```powershell
# 흑백 1920x1080 (AI용)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/screenshot.ps1

# 컬러
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/screenshot.ps1 -Color

# 원본 해상도
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/screenshot.ps1 -Original

# 영역 캡처
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/screenshot.ps1 -AtX 200 -AtY 300 -Size 500
```
- 전체: `screenshot/YYYYMMDDHHmmss-desktop.png`
- 영역: `screenshot/YYYYMMDDHHmmss-region.png`

## 텍스트 캡처 워크플로우

```powershell
# 1. 대상 윈도우 핸들 찾기
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/get-window-info.ps1

# 2. 빠른 텍스트 캡처
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/text-capture.ps1 -Hwnd <hwnd>

# 3. 스크롤 캡처 (날짜 필터)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/scroll-capture.ps1 -Hwnd <hwnd> -Start 2026-03-31

# 4. 특정 영역 대상 스크롤 캡처
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/wininfo-layout.ps1 -Hwnd <hwnd>
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/scroll-capture.ps1 -Hwnd <hwnd> -PickX <x> -PickY <y>
```

## UI 요소 탐색 워크플로우 (스크린샷 불필요)

```powershell
# 1. 대상 윈도우 핸들 찾기
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/get-window-info.ps1

# 2. 요소 검색 → 절대 좌표 확인 → 클릭
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/element-tree.ps1 -Hwnd <hwnd> -Search "운영타워"
# → [TreeItem] "채널 운영타워" @(128,978) [372x43]
#   클릭 좌표 = (128 + 372/2, 978 + 43/2) = (314, 999)

powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/mouseclick.ps1 -X 314 -Y 999
```

**element-tree가 유용한 경우:**
- 버튼/메뉴/텍스트 등의 **이름을 알고 있을 때** → `-Search`로 즉시 위치+좌표 확인
- **스크린샷 불필요** — `@(x,y)` 절대 좌표로 바로 클릭 가능, DPI 보정도 불필요
- 앱의 **UI 구조를 파악**하고 싶을 때 (전체 트리 덤프)

**element-tree의 한계:**
- 일부 커스텀 렌더링 앱(게임 등)은 요소가 안 보일 수 있음
- 좌표가 `@(0,0)`이거나 없는 요소는 스크린샷 워크플로우로 폴백

## 터미널 원격 제어 워크플로우

AgentZero 내장 터미널에 CLI에서 명령을 보내는 워크플로우. AgentZero WPF가 실행 중이어야 한다.

```powershell
# 1. 터미널 세션 목록 확인
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-list.ps1
# → Group 0: MyProject  (D:\Code\MyProject)
#     Tab 0: PowerShell *
#       ID: MyProject/PowerShell
#     Tab 1: Claude Code
#       ID: MyProject/Claude Code

# 2. 특정 터미널에 명령 전송
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-send.ps1 -GroupIndex 0 -TabIndex 0 dotnet build

# 3. TUI에서 Enter 미인식 시 별도 Enter 전송 (Codex 등)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-key.ps1 -GroupIndex 0 -TabIndex 0 -Key cr

# 4. 터미널 출력 확인 (최근 2000자)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-read.ps1 -GroupIndex 0 -TabIndex 0 -Last 2000

# 5. 실행 중인 프로세스 중단
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-key.ps1 -GroupIndex 0 -TabIndex 0 -Key ctrlc

# 6. AgentBot에 진행 상황 알림
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/bot-chat.ps1 -From BuildScript Build completed successfully
```

### Multi-AI 대화 워크플로우 (Claude ↔ Codex 등)

다른 AI 에이전트(Claude Code, OpenAI Codex 등)가 실행된 터미널에 메시지를 보내고 응답을 읽는 워크플로우.

```powershell
# 1. 터미널 목록에서 대상 AI 터미널 확인
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-list.ps1

# 2a. Claude Code 터미널에 메시지 전송 (terminal-send만으로 OK)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-send.ps1 -GroupIndex 3 -TabIndex 0 "안녕하세요"

# 2b. Codex 터미널에 메시지 전송 (2단계: send → 1초 대기 → key cr)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-send.ps1 -GroupIndex 3 -TabIndex 1 "안녕하세요"
sleep 1
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-key.ps1 -GroupIndex 3 -TabIndex 1 -Key cr

# 3. 응답 대기 후 읽기 (최적 대기시간: 아래 표 참고)
sleep 3
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/terminal-read.ps1 -GroupIndex 3 -TabIndex 0 -Last 3000
```

### Multi-AI 대화 응답 대기시간 지침

실측 테스트(2026-04-06, gpt-5.4 high fast 모드) 기반 최적 대기시간.

**Codex 전송 시 send와 cr 사이에 1초 간격을 둘 것** — 텍스트 입력 직후 즉시 Enter를 보내면 TUI가 입력을 놓칠 수 있다.

| 응답 유형 | 최적 대기시간 | 비고 |
|----------|-------------|------|
| 짧은 응답 (1~2문장) | **1~2초** | 단답형 질문에 적합 |
| 일반 응답 (3~5문장) | **3초** | 대부분의 대화에 안정적 |
| 긴 응답 (목록, 코드 등) | **5~6초** | 구조화된 응답, 코드 예시 포함 시 |

**권장 기본값: 3초** — 안정적으로 대부분의 응답을 커버한다.

**응답 미확인 시 대처:**
1. 추가 3초 대기 후 재읽기 (`terminal-read -Last 3000`)
2. 여전히 없으면 스크린샷으로 터미널 상태 확인
3. TUI가 입력 대기 상태인지, 아직 응답 생성 중인지 구분

**다턴 자유대화 루프 패턴:**
```powershell
# 매 턴마다 반복
terminal-send -GroupIndex G -TabIndex T "메시지"
sleep 1
terminal-key -GroupIndex G -TabIndex T -Key cr
sleep 3                                          # 기본 3초 대기
terminal-read -GroupIndex G -TabIndex T -Last 2000  # 응답 확인
```

## 스크린샷 기반 좌표 추정 워크플로우

앱 내부 UI 컴포넌트(버튼, 메뉴 등)를 클릭해야 하는데 좌표를 모를 때 사용한다.

```powershell
# Step 0. DPI 파악 (세션에서 최초 1회)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/dpi.ps1

# Step 1. 전체 스크린샷으로 대략적 위치 파악
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/screenshot.ps1

# Step 2. 넓은 영역 스크린샷으로 대상 탐색
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/screenshot.ps1 -AtX <x> -AtY <y> -Size 500

# Step 3. 좁은 영역으로 줌인 (정밀 좌표 확인)
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/screenshot.ps1 -AtX <x> -AtY <y> -Size 150

# Step 4. DPI 보정 후 좌표 계산
#   corrected_pixel = AI_estimated_pixel * dpi_scale
#   mouse_coord = origin + corrected_pixel

# Step 5. 보정된 좌표로 클릭
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/mouseclick.ps1 -X <mouse_x> -Y <mouse_y>

# Step 6. 클릭 결과 검증
powershell -ExecutionPolicy Bypass -File .claude/skills/agent-zero/scripts/log.ps1 -Last 1
```

**정밀 클릭 핵심 원칙:**
- **DPI 보정 필수**: `corrected_pixel = AI_estimated_pixel * dpi_scale`
- **줌인 필수**: 큰 영역(400~500)에서 대상을 찾은 뒤, 작은 영역(150~200)으로 줌인
- **행동 로그 참고**: `log` 명령으로 이전 클릭의 좌표와 결과를 확인

## 작업 수행 가이드라인

1. **항상 get-window-info부터 시작** - 대상 윈도우의 핸들(hwnd)을 먼저 파악한다.
2. **최소화된 윈도우는 activate로 복원** - 최소화 상태에서는 캡처/클릭이 불가하므로 먼저 activate로 복원한다.
3. **텍스트 캡처는 CLI로 직접** - text-capture(빠른 캡처) 또는 scroll-capture(스크롤 캡처)를 사용한다.
4. **앱 내부 UI 요소는 element-tree 먼저 시도 (필수)** - 이름을 알고 있는 버튼/메뉴/채널은 반드시 element-tree --search로 먼저 검색한다. element-tree가 실패할 때만 screenshot으로 폴백.
5. **DPI 보정을 반드시 적용한다** - 영역 스크린샷에서 좌표를 추정할 때 `corrected_pixel = AI_estimated_pixel * (DPI / 96)` 공식을 적용한다.
6. **클릭 후 반드시 검증한다** - log --last 1로 타이틀 변경 확인, 또는 screenshot --at으로 시각 검증.
7. **행동 로그를 활용한다** - log 명령으로 이전 행동과 결과를 검토하여 반복 작업 시 이전 성공 좌표를 참고한다.
8. **에러 발생 시** - "실행 파일을 찾을 수 없습니다" 오류가 나오면 `dotnet build -c Release` 빌드가 필요하다고 안내한다.
9. **AgentZero 미실행 시** - "AgentZero is not running" 오류가 나오면 AgentZero WPF를 먼저 실행하라고 안내한다. 자동 실행하지 말 것.
