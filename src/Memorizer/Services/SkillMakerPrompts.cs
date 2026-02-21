namespace Memorizer.Services;

/// <summary>
/// Prompts for Claude Code Skill Maker
/// </summary>
public static class SkillMakerPrompts
{
    /// <summary>
    /// Generate sub-skill suggestions based on job category
    /// </summary>
    public static string GetSubSkillSuggestionPrompt(string category)
    {
        return $@"당신은 Claude Code 스킬 전문가입니다.
다음 직군에서 유용한 Claude Code 스킬 5개를 추천해주세요.

## 직군: {category}

## 규칙
- 각 스킬은 해당 직군에서 실제로 자주 사용되는 작업을 자동화하는 것이어야 합니다.
- 스킬명은 간결하고 명확해야 합니다 (한국어, 20자 이내).
- 각 스킬에 대해 한 줄 설명을 포함해주세요.

## 출력 형식 (JSON만 출력, 마크다운 없이)
[
  {{""name"": ""스킬명1"", ""description"": ""설명1""}},
  {{""name"": ""스킬명2"", ""description"": ""설명2""}},
  {{""name"": ""스킬명3"", ""description"": ""설명3""}},
  {{""name"": ""스킬명4"", ""description"": ""설명4""}},
  {{""name"": ""스킬명5"", ""description"": ""설명5""}}
]

JSON:";
    }

    /// <summary>
    /// Generate follow-up question to gather more information
    /// </summary>
    public static string GetFollowUpQuestionPrompt(string category, string skillName, string conversationHistory)
    {
        return $@"당신은 Claude Code 스킬을 만들기 위해 사용자에게 필요한 정보를 수집하는 전문가입니다.

## 직군: {category}
## 스킬명: {skillName}

## 지금까지의 대화
{conversationHistory}

## 목표
이 스킬을 작성하기 위해 아직 부족한 정보가 있는지 판단하세요.
스킬 작성에 필요한 정보:
1. 스킬이 수행해야 할 구체적인 작업 (무엇을 하는가)
2. 스킬이 사용되어야 하는 상황 (언제 사용하는가)
3. 입력/출력 형태 (어떤 파일이나 데이터를 다루는가)
4. 사용할 도구나 명령어 (allowed-tools에 포함될 것들)
5. 주의사항이나 제약조건

## 규칙
- 이미 충분한 정보가 수집되었다면 정확히 ""READY"" 라고만 출력하세요.
- 아직 정보가 부족하면 사용자에게 할 질문을 하나만 작성하세요.
- 질문은 구체적이고 답변하기 쉬워야 합니다.
- 최대 5번의 질의까지만 진행합니다. 대화가 이미 5회 이상이면 반드시 ""READY""를 출력하세요.

## 중요: 중복 질문 금지
- 위 대화 이력을 반드시 확인하고, 이미 질문했거나 사용자가 답변한 내용과 유사한 질문을 절대 하지 마세요.
- 이미 수집된 정보 항목은 다시 묻지 마세요. 아직 수집되지 않은 새로운 정보만 질문하세요.
- 대화에서 이미 다뤄진 주제: 확인 후, 다른 관점의 새로운 질문만 하세요.

출력:";
    }

    /// <summary>
    /// Generate the complete SKILL.md content
    /// </summary>
    public static string GetSkillGenerationPrompt(string category, string skillName, string collectedInfo)
    {
        return $@"당신은 Claude Code 스킬 전문가입니다.
수집된 정보를 바탕으로 완전한 SKILL.md 파일을 생성해주세요.

## 직군: {category}
## 스킬명: {skillName}

## 수집된 정보
{collectedInfo}

## SKILL.md 작성 규칙

### YAML 프론트매터 (필수)
- name: 소문자, 숫자, 하이픈만 사용 (최대 64자). 스킬의 고유 식별자
- description: 최대 1024자. 스킬이 수행하는 작업 + 사용 시기 + 트리거 용어 포함
- allowed-tools: 선택사항. 스킬이 사용할 도구 목록 (Read, Write, Bash, Grep, Glob 등)

### Markdown 본문 (필수)
- # 스킬 제목
- ## Instructions: 명확한 단계별 가이드
- ## Examples: 구체적인 사용 예시 (코드 블록 포함)
- ## Best Practices: 모범 사례 (선택)

## 출력
아래 형식으로 SKILL.md 전체 내용을 출력하세요. 마크다운 코드블록(```)으로 감싸지 마세요.

---
name: skill-name-here
description: Description here. Use when...
allowed-tools: Read, Write, Bash
---

# Skill Title

## Instructions
...

## Examples
...";
    }

    /// <summary>
    /// Generate practical usage guide from completed skill content
    /// </summary>
    public static string GetUsageGuidePrompt(string skillContent)
    {
        return $@"다음은 완성된 Claude Code SKILL.md 파일입니다. 이 스킬을 실제로 활용하는 방법을 안내해주세요.

## SKILL.md 내용
{skillContent}

## 규칙
1. YAML 프론트매터에서 name 필드를 읽어 스킬 호출 명령어를 만드세요.
2. 이 스킬이 할 수 있는 주요 작업 3~5개를 나열하세요.
3. 각 작업에 대해 실제 사용할 수 있는 Claude Code 명령어 예시를 제공하세요.
4. 형식: /skill-[name] [구체적인 요청]

## 출력 형식 (마크다운, 간결하게)
### 이 스킬이 할 수 있는 것

1. **작업1 제목**
   ```
   /skill-[name] 작업1에 대한 구체적 요청 예시
   ```

2. **작업2 제목**
   ```
   /skill-[name] 작업2에 대한 구체적 요청 예시
   ```

(3~5개)

출력:";
    }

    /// <summary>
    /// Get the initial welcome message with category options
    /// </summary>
    public static string GetWelcomeMessage()
    {
        return "만들고 싶은 역할이 무엇인가요? 업무 직군을 선택해주세요.";
    }

    /// <summary>
    /// Get category options
    /// </summary>
    public static List<string> GetCategoryOptions()
    {
        return new List<string>
        {
            "개발자",
            "기획자",
            "데브옵스",
            "QA",
            "직접입력"
        };
    }
}
