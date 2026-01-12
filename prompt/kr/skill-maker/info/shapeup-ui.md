# ShapeUp Whiteboard UI 구조 및 컨셉

## 개요

ShapeUp Whiteboard는 Fabric.js 기반의 인터랙티브 화이트보드 애플리케이션입니다.
Shape Up 방법론(Basecamp)의 5단계 프로세스를 시각화하고, AI 기반 보드 생성 기능을 제공합니다.

## UI/UX 컨셉

### 레이아웃 구조

```
┌─────────────────────────────────────────────────────────────────┐
│  Header (보라색 그라데이션)                                        │
│  [Title]                              [Clear][PNG][SVG][Share]  │
├────────────┬────────────────────────────────────────────────────┤
│            │                                                    │
│  Tool      │              Canvas Area                           │
│  Panel     │              (Fabric.js)                           │
│  (200px)   │                                                    │
│            │                                                    │
│  - Drawing │                                                    │
│  - Templates│                                                   │
│  - Properties│                                                  │
│            │                                    ┌──────────────┐│
│            │                                    │Canvas Controls│
│            │                                    │[Pan][+][-][AI]│
│            │                                    └──────────────┘│
└────────────┴────────────────────────────────────────────────────┘
```

### 색상 테마

| 요소 | 색상 | 용도 |
|------|------|------|
| Primary | `#667eea` ~ `#764ba2` | 헤더 그라데이션, 강조 버튼 |
| Problem | `#667eea` | Problem 보드 테마 |
| Breadboard | `#00c6ff` | Breadboard 보드 테마 |
| Fat Marker | `#11998e` | Fat Marker 보드 테마 |
| Risk | `#f093fb` | Risk 보드 테마 |
| Pitch | `#ff6b6b` | Pitch 보드 테마 |
| Background | `#f0f2f5` | 페이지 배경 |
| Canvas BG | `#f8f9fa` | 캔버스 배경 |

### 인터랙션 패턴

1. **도구 선택**: 좌측 패널에서 도구 클릭 → 활성 상태 표시 (보라색 배경)
2. **도형 그리기**: 드래그 앤 드롭으로 도형 생성
3. **화살표 연결**: 도형 위에서 드래그 시작 → 자석처럼 앵커 포인트에 스냅
4. **텍스트 편집**: 더블클릭으로 편집 모드 진입
5. **컨텍스트 메뉴**: 우클릭으로 그룹핑, 속성, 삭제 등 조작
6. **줌/팬**: 마우스 휠 줌, Alt+드래그 또는 Pan 툴로 이동

---

## 파일 구조

### 리팩토링 후 구조

```
src/Memorizer/
├── Views/ShapeUpView/
│   ├── Index.cshtml          # 메인 뷰 (61줄) - 레이아웃만 담당
│   ├── _ToolPanel.cshtml     # 드로잉 도구 패널
│   ├── _Canvas.cshtml        # 캔버스 영역 + AI 프롬프트 패널
│   ├── _Modals.cshtml        # 토스트, 프로그레스, 공유 모달
│   ├── _ContextMenu.cshtml   # 우클릭 컨텍스트 메뉴
│   ├── _PropertyPopup.cshtml # 속성 팝업
│   ├── Share.cshtml          # 공유 페이지
│   ├── ShareList.cshtml      # 공유 목록 페이지
│   └── Index.cshtml.bak      # 원본 백업 (4297줄)
│
└── wwwroot/
    ├── css/
    │   └── shapeup.css       # ShapeUp 전용 스타일
    │
    └── js/
        ├── shapeup.js        # 핵심 캔버스 로직
        └── shapeup-templates.js  # 보드 템플릿 + AI 생성
```

### 파일별 역할

#### CSS 파일

**`wwwroot/css/shapeup.css`**
- 페이지 레이아웃 (`.shapeup-page`, `.shapeup-container`)
- 헤더 스타일 (`.shapeup-header`, `.btn-header`)
- 도구 패널 (`.tool-panel`, `.tool-btn`, `.tool-section`)
- 캔버스 영역 (`.canvas-area`, `.canvas-controls`)
- AI 프롬프트 패널 (`.ai-prompt-panel`)
- 토스트 알림 (`.toast-notification`)
- 컨텍스트 메뉴 (`.context-menu`)
- 속성 팝업 (`.property-popup`)
- 애니메이션 (`@keyframes spin`, `slideIn`, `slideOut`)

#### JavaScript 파일

**`wwwroot/js/shapeup.js`** (핵심 로직)
```javascript
// 주요 섹션
- Canvas State Variables     // 캔버스 상태 변수
- Toast Notification         // 토스트 알림 함수
- Canvas Initialization      // 캔버스 초기화
- Tool Selection            // 도구 선택
- Drawing Functions         // 그리기 함수 (rect, circle, line, arrow, text)
- Arrow Functions           // 화살표 연결, 자석 스냅, 연결 업데이트
- Context Menu Functions    // 컨텍스트 메뉴
- Property Popup Functions  // 속성 팝업
- Group Functions           // 그룹핑/언그룹
- Text Editing              // 그룹 내 텍스트 편집
- Zoom Functions            // 줌 인/아웃/리셋
- Export Functions          // PNG/SVG 내보내기
```

**`wwwroot/js/shapeup-templates.js`** (템플릿 + AI)
```javascript
// 주요 섹션
- Board Templates           // 5가지 보드 템플릿
  - addProblemBoard()
  - addBreadboard()
  - addFatMarkerSketch()
  - addRiskBoard()
  - addPitchBoard()

- AI Generation             // AI 보드 생성
  - generateWithAI()
  - generateFreeBoard()     // 자유 보드 (메모리 검색 포함)
  - generateSingleBoard()   // 단일 보드 생성
  - generateIntegratedBoards()  // 5단계 통합 생성

- Rendering Functions       // 렌더링 함수
  - renderFreeBoard()       // Free Board JSON 렌더링
  - renderBoardElements()   // 보드 요소 렌더링
  - 각종 Helper 함수 (createFrame, createSection, etc.)

- Share Functionality       // 공유 기능
```

#### 부분뷰 파일

| 파일 | 역할 |
|------|------|
| `_ToolPanel.cshtml` | Drawing Tools, Board Templates, Properties, Text Properties 섹션 |
| `_Canvas.cshtml` | 캔버스 영역, 하단 컨트롤 바, AI 프롬프트 패널 |
| `_Modals.cshtml` | 토스트 컨테이너, 프로그레스 인디케이터, 공유 모달 |
| `_ContextMenu.cshtml` | Properties, Group/Ungroup, Bring to Front/Send to Back, Delete |
| `_PropertyPopup.cshtml` | Shape 속성 (Fill, Stroke), Text 속성 (Color, Size, Align, Style, List) |

---

## 주요 기능

### 1. 드로잉 도구

| 도구 | 설명 |
|------|------|
| Select | 객체 선택, 이동, 크기 조절 |
| Rectangle | 둥근 모서리 사각형 |
| Circle | 원형 |
| Line | 직선 |
| Arrow | 화살표 (자석 스냅 지원) |
| Text | 텍스트 (리스트 연속 입력 지원) |
| Delete | 선택 객체 삭제 |

### 2. 보드 템플릿

5가지 Shape Up 방법론 템플릿:
1. **Problem** - 문제 정의 (Raw Idea, Narrowed Problem, Baseline, Appetite)
2. **Breadboard** - 화면 흐름도 (Places + Affordances)
3. **Fat Marker** - 대략적인 UI 스케치
4. **Risk** - Rabbit Holes + No-Gos
5. **Pitch** - 최종 제안서 (5개 섹션)

### 3. 화살표 자석 기능

- 도형 위에서 드래그 시작 → 가장 가까운 앵커 포인트에 스냅
- 앵커 포인트: top, bottom, left, right (각 변의 중앙)
- 연결된 도형 이동 시 화살표 자동 업데이트
- 드래그 중 미리보기 표시 (점선)

### 4. 그룹핑

- 2개 이상 선택 → 우클릭 → Group
- 그룹 내 텍스트는 더블클릭으로 편집 가능
- Ungroup으로 해제

### 5. 텍스트 속성

- 크기: H1 (32px), H2 (24px), H3 (18px), Normal (14px)
- 정렬: 왼쪽, 가운데, 오른쪽
- 스타일: Bold, Italic, Underline
- 리스트: 번호 매기기, 글머리 기호 (Enter 시 자동 연속)

### 6. AI 보드 생성

- **Free Board**: 자유로운 시각화 보드 (메모리 검색 포함)
- **단일 보드**: 특정 타입의 보드만 생성
- SSE 스트리밍으로 진행 상황 실시간 표시
- 메모리 조각 채택 시 토스트 알림

### 7. 공유 기능

- 보드 데이터 JSON 저장
- 짧은 URL 생성 (`/ui/shapeup/share/{shortCode}`)
- 클립보드 복사

---

## 의존성

### 외부 라이브러리

```html
<!-- Fabric.js 5.3.1 - 캔버스 라이브러리 -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/fabric.js/5.3.1/fabric.min.js"></script>

<!-- FileSaver.js - 파일 다운로드 -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/FileSaver.js/2.0.5/FileSaver.min.js"></script>
```

### 내부 의존성

- Bootstrap 5 (모달, 버튼 그룹)
- Font Awesome (아이콘)
- `_Layout.cshtml` (공통 레이아웃)

---

## API 엔드포인트

| 엔드포인트 | 메서드 | 설명 |
|------------|--------|------|
| `/api/shapeup/generate` | POST | AI 보드 생성 (SSE 스트리밍) |
| `/api/shapeup/generate-title` | POST | 프롬프트 기반 제목 생성 |
| `/api/shapeup/share` | POST | 보드 공유 링크 생성 |

---

## 리팩토링 히스토리

| 날짜 | 내용 |
|------|------|
| 2026-01-12 | 4297줄 단일 파일 → 9개 파일로 분리 |
| | CSS 분리: `shapeup.css` |
| | JS 분리: `shapeup.js`, `shapeup-templates.js` |
| | 부분뷰 분리: `_ToolPanel`, `_Canvas`, `_Modals`, `_ContextMenu`, `_PropertyPopup` |
