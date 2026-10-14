# 로그 템플릿 (Log Template)

하네스의 모든 로그는 이 템플릿을 따른다. **필드를 즉흥적으로 추가하지 않는다** —
새 필드가 필요하면 먼저 이 템플릿을 갱신하고(스키마 변경 = Minor 버전), 그 다음 로그에 쓴다.

## 경로

`harness/logs/{part}/{yyyy-MM-dd-HH-mm-title}.md`

- `{part}`: **에이전트명 또는 엔진명** (예: `tamer`, `security-guard`, `full-review`)
  - 에이전트도 엔진도 아닌 메타 작업은 `meta`를 쓴다. 새 디렉토리를 즉흥 신설하지 않는다.
- `{title}`: 영문 kebab-case 활동 요약

## frontmatter 스키마

```yaml
---
date: {ISO 8601}                  # 필수
agent: {agent-name}               # 필수. 엔진 실행이면 "engine:{name}", 메타 작업이면 "meta:{name}"
type: {evaluation | improvement | explanation | review | creation | copy | recruit | release | migration}
mode: {log-eval | suggestion-tip | skill-copy | recruit | execution | full-review | targeted-review | build-verify | version | init | migrate}
trigger: "{매칭된 트리거 문구}"     # 트리거로 시작된 작업이면 필수
command: "{실행한 명령}"           # 트리거 없이 명령으로 시작된 메타 작업이면 trigger 대신 이 필드
---
```

- `trigger`와 `command` 중 **정확히 하나**는 반드시 있어야 한다.
- `mode` 값은 위 enum에서만 고른다. 두 값을 조합하지 않는다 (`A + B` 금지) —
  두 모드가 섞인 작업이면 주된 모드 하나를 고르고 본문에 부연한다.
- 구 로그의 `mode: kakashi-copy`(2.0.x 이전)는 `skill-copy`와 동일하게 읽는다. 기존 로그는 고치지 않는다.

## 본문 필수 섹션

```markdown
# {활동 제목}

## 실행 요약
{수행한 작업 내용}

## 결과
{산출물, 변경사항}

## 평가
{하네스 기본 평가축에 따른 평가 — references/evaluation.md 참조}

### Plan
### Do
### Study
### Act
{PDSA 상시 평가를 운용하는 하네스만. 반드시 본문 H3 섹션으로 쓴다 —
frontmatter YAML 안에 넣으면 자동 집계 도구가 읽지 못한다.}

## 다음 단계 제안
- {제안 1 — 실행할 것이면 harness/todo/ 로 승격을 함께 판단한다}
```

## 규칙

1. **`## 평가` 생략 금지** — 평가 없는 로그는 불완전한 실행이다.
   예외를 두려면 이 템플릿에 면제 조건을 명시적으로 추가한 뒤에만 허용된다.
2. **합성 등급 금지** — 여러 축/에이전트의 결과를 "종합 B+" 같은 단일 등급으로
   합치지 않는다. 합성이 꼭 필요하면 규칙을 evaluation.md에 먼저 정의한다.
3. **다음 단계 제안은 쌓아두지 않는다** — 실행할 제안은 `harness/todo/`로 승격한다.
   로그에만 남은 제안은 실행되지 않는다.
