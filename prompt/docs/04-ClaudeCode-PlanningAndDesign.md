Claude Skills Guide - Part 2: Planning and Design (Ch.2)
1. Use Case 정의부터 시작
   코드 작성 전 2-3개의 구체적 유즈케이스 식별 필요

좋은 Use Case 정의 예시
Vbnet

Copy
Use Case: Project Sprint Planning
Trigger: "help me plan this sprint" / "create sprint tasks"
Steps:
1. Linear에서 현재 프로젝트 상태 조회 (via MCP)
2. 팀 velocity, capacity 분석
3. 작업 우선순위 제안
4. Linear에 라벨/추정치 포함 작업 생성
   Result: 작업이 생성된 완전한 스프린트 계획
   자문 체크리스트
   사용자가 달성하려는 것?
   필요한 멀티스텝 워크플로우?
   필요한 도구 (빌트인 or MCP)?
   내장해야 할 도메인 지식/베스트 프랙티스?
2. 스킬 유즈케이스 3가지 카테고리
   Category 1: Document & Asset Creation
   목적: 일관된 고품질 문서, 프레젠테이션, 앱, 디자인, 코드 생성
   예시: frontend-design, docx, pptx, xlsx 스킬
   기법: 스타일 가이드 내장, 템플릿 구조, 품질 체크리스트, 외부 도구 불필요
   Category 2: Workflow Automation
   목적: 일관된 방법론의 멀티스텝 프로세스 (여러 MCP 서버 조율 포함)
   예시: skill-creator 스킬
   기법: 단계별 워크플로우+검증 게이트, 템플릿, 개선 제안, 반복 정제 루프
   Category 3: MCP Enhancement
   목적: MCP 서버의 도구 접근에 워크플로우 가이드 추가
   예시: sentry-code-review 스킬 (Sentry)
   기법: 순차적 MCP 호출 조율, 도메인 전문성 내장, 컨텍스트 제공, 에러 처리
3. 성공 기준 정의
   정량적 지표
   관련 쿼리 90%에서 스킬 트리거 (10-20개 테스트 쿼리)
   X번의 도구 호출로 워크플로우 완료 (스킬 유/무 비교)
   워크플로우당 0 실패 API 호출 (MCP 서버 로그 모니터링)
   정성적 지표
   사용자가 다음 단계를 프롬프트할 필요 없음
   사용자 수정 없이 워크플로우 완료
   세션 간 일관된 결과
4. 기술 요구사항
   파일 구조 규칙
   SKILL.md: 반드시 정확히 "SKILL.md" (대소문자 구분)
   폴더 네이밍: kebab-case만 허용 (notion-project-setup ✅, 공백/밑줄/대문자 ❌)
   README.md: 스킬 폴더 내부에 포함 금지 (모든 문서는 SKILL.md 또는 references/)
   YAML Frontmatter (가장 중요한 부분)
   YAML

Copy
---
name: your-skill-name          # Required, kebab-case
description: What it does. Use when user asks to [specific phrases].  # Required
---
name 필드
kebab-case only, 공백/대문자 금지, 폴더명과 일치
description 필드 (핵심!)
반드시 포함: ① 무엇을 하는지 + ② 언제 사용하는지 (트리거 조건)
1024자 미만
XML 태그 (< >) 금지
구체적 작업/파일 타입 언급
좋은 description 예시
YAML

Copy
# 구체적이고 실행 가능
description: Analyzes Figma design files and generates developer handoff documentation.
Use when user uploads .fig files, asks for "design specs", "component documentation",
or "design-to-code handoff".

# 트리거 문구 포함
description: Manages Linear project workflows including sprint planning, task creation,
and status tracking. Use when user mentions "sprint", "Linear tasks", "project planning",
or asks to "create tickets".
나쁜 description 예시
YAML

Copy
description: Helps with projects.          # 너무 모호
description: Creates sophisticated multi-page documentation systems.  # 트리거 누락
description: Implements the Project entity model with hierarchical relationships.  # 너무 기술적
Optional 필드
license: MIT, Apache-2.0 등
compatibility: 환경 요구사항 (1-500자)
metadata: author, version, mcp-server 등 커스텀 key-value
보안 제한
YAML에 XML 꺽쇠 (< >) 금지 (시스템 프롬프트 인젝션 방지)
"claude" 또는 "anthropic" 이름 사용 금지 (예약어)
5. 효과적인 Instructions 작성
   추천 구조
   Markdown

Copy
---
name: your-skill
description: [...]
---
# Your Skill Name
# Instructions
### Step 1: [First Major Step]
Clear explanation...
Example: ...

# Examples
Example 1: [common scenario]
User says: "..."
Actions: 1. ... 2. ...
Result: ...

# Troubleshooting
Error: [Common error]
Cause: [Why]
Solution: [How to fix]
Best Practices
구체적이고 실행 가능하게: python scripts/validate.py --input {filename} 처럼 명확히
에러 처리 포함: MCP 연결 실패 시 단계별 해결법
번들 리소스 명확히 참조: references/api-patterns.md 참고하도록 안내
Progressive Disclosure 활용: SKILL.md는 핵심만, 상세 문서는 references/로
Source: "The Complete Guide to Building Skills for Claude" PDF (Pages 7-13)