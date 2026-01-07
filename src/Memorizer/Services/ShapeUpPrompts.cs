namespace Memorizer.Services;

/// <summary>
/// LLM prompts for Shape Up Whiteboard feature
/// Generates Shape Up boards (Problem Definition, Breadboard, Fat Marker Sketch, Risk Assessment, Pitch)
/// </summary>
public static class ShapeUpPrompts
{
    /// <summary>
    /// Get the appropriate board generation prompt based on board type
    /// </summary>
    public static string GetBoardGenerationPrompt(string prompt, string boardType, bool isIntegrated = false)
    {
        // For integrated generation, the prompt already contains context from previous steps
        if (isIntegrated)
        {
            return boardType.ToLower() switch
            {
                "problem" => GetProblemDefinitionPrompt(prompt),
                "breadboard" => GetIntegratedBreadboardPrompt(prompt),
                "fat-marker" => GetIntegratedFatMarkerPrompt(prompt),
                "risk" => GetIntegratedRiskPrompt(prompt),
                "pitch" => GetIntegratedPitchPrompt(prompt),
                _ => GetPitchBoardPrompt(prompt)
            };
        }

        return boardType.ToLower() switch
        {
            "problem" => GetProblemDefinitionPrompt(prompt),
            "breadboard" => GetBreadboardPrompt(prompt),
            "fat-marker" => GetFatMarkerSketchPrompt(prompt),
            "risk" => GetRiskAssessmentPrompt(prompt),
            "pitch" => GetPitchBoardPrompt(prompt),
            _ => GetPitchBoardPrompt(prompt)  // Default to full pitch board
        };
    }

    #region Integrated Generation Prompts (context-aware)

    /// <summary>
    /// Breadboard with Problem Definition context
    /// </summary>
    private static string GetIntegratedBreadboardPrompt(string contextPrompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
이전 단계에서 분석한 Problem Definition을 기반으로 Breadboard를 생성합니다.

{contextPrompt}

## Breadboard 생성 지침
이전 단계의 Problem Definition에서 도출된 문제와 해결 방향을 기반으로:
1. 핵심 사용자 흐름을 Places로 표현
2. 각 Place의 주요 인터랙션을 Affordances로 정의
3. 사용자 여정을 Connection으로 연결

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""breadboard"",
  ""title"": ""Breadboard"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 600,
      ""height"": 250,
      ""title"": ""BREADBOARD"",
      ""color"": ""#00c6ff""
    }},
    {{
      ""type"": ""place"",
      ""id"": ""place1"",
      ""x"": 80,
      ""y"": 100,
      ""width"": 150,
      ""height"": 120,
      ""name"": ""[화면명]"",
      ""affordances"": [""버튼1"", ""필드1""]
    }},
    {{
      ""type"": ""place"",
      ""id"": ""place2"",
      ""x"": 280,
      ""y"": 100,
      ""width"": 150,
      ""height"": 120,
      ""name"": ""[다음 화면]"",
      ""affordances"": [""버튼2""]
    }},
    {{
      ""type"": ""connection"",
      ""from"": ""place1"",
      ""to"": ""place2"",
      ""label"": ""액션""
    }}
  ]
}}

Problem Definition의 내용과 일관성 있게 3-4개의 핵심 Places를 생성하세요.";
    }

    /// <summary>
    /// Fat Marker with Breadboard context
    /// </summary>
    private static string GetIntegratedFatMarkerPrompt(string contextPrompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
이전 단계에서 분석한 Problem과 Breadboard를 기반으로 Fat Marker Sketch를 생성합니다.

{contextPrompt}

## Fat Marker Sketch 생성 지침
이전 단계의 Breadboard에서 정의된 화면 흐름을 기반으로:
1. 주요 화면의 대략적인 레이아웃을 굵은 박스로 표현
2. 세부 디테일 대신 영역과 구조에 집중
3. Squiggle로 텍스트 영역 표시
4. 중요 영역에 annotation 추가

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""fat-marker"",
  ""title"": ""Fat Marker Sketch"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 350,
      ""height"": 380,
      ""title"": ""FAT MARKER SKETCH"",
      ""color"": ""#11998e""
    }},
    {{
      ""type"": ""box"",
      ""x"": 70,
      ""y"": 90,
      ""width"": 310,
      ""height"": 40,
      ""label"": ""Header""
    }},
    {{
      ""type"": ""box"",
      ""x"": 70,
      ""y"": 150,
      ""width"": 150,
      ""height"": 100,
      ""label"": ""Main""
    }},
    {{
      ""type"": ""annotation"",
      ""x"": 430,
      ""y"": 150,
      ""text"": ""← 주요 영역"",
      ""color"": ""#764ba2""
    }}
  ]
}}

Breadboard의 Places를 시각적 레이아웃으로 변환하세요.";
    }

    /// <summary>
    /// Risk Assessment with all previous context
    /// </summary>
    private static string GetIntegratedRiskPrompt(string contextPrompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
이전 단계에서 분석한 Problem, Breadboard, Fat Marker를 기반으로 Risk Assessment Board를 생성합니다.

{contextPrompt}

## Risk Assessment 생성 지침
이전 단계들의 결과를 분석하여:
1. 기술적으로 불확실한 부분 식별 (Rabbit Holes)
2. 각 위험 요소에 대한 대응 방안 제시
3. 범위에서 명시적으로 제외할 항목 정의 (No-Gos)

## 리스크 식별 체크리스트
- Breadboard의 복잡한 흐름에서 기술적 리스크가 있나?
- Fat Marker에서 UI 구현의 불확실성이 있나?
- 팀이 경험하지 못한 영역이 있나?

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""risk"",
  ""title"": ""Risk Assessment"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 600,
      ""height"": 400,
      ""title"": ""RISK & RABBIT HOLES"",
      ""color"": ""#f093fb""
    }},
    {{
      ""type"": ""rabbitHoleList"",
      ""x"": 70,
      ""y"": 120,
      ""width"": 550,
      ""title"": ""Rabbit Holes"",
      ""items"": [
        {{
          ""description"": ""[위험 요소]"",
          ""status"": ""patched"",
          ""solution"": ""[해결책]""
        }}
      ]
    }},
    {{
      ""type"": ""noGoList"",
      ""x"": 70,
      ""y"": 280,
      ""width"": 550,
      ""title"": ""No-Gos"",
      ""items"": [""[제외 기능 1]"", ""[제외 기능 2]""]
    }}
  ]
}}

이전 단계들에서 식별된 구체적인 위험 요소와 제외 항목을 포함하세요.";
    }

    /// <summary>
    /// Final Pitch Summary integrating all previous steps
    /// </summary>
    private static string GetIntegratedPitchPrompt(string contextPrompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
이전 4단계(Problem, Breadboard, Fat Marker, Risk)의 결과를 종합하여 최종 Pitch Board를 생성합니다.

{contextPrompt}

## Pitch Board 생성 지침
이전 단계들의 결과를 요약하여 5가지 필수 요소를 완성합니다:
1. **PROBLEM**: Problem Definition에서 정의된 핵심 문제
2. **SOLUTION**: Fat Marker와 Breadboard에서 도출된 솔루션
3. **RABBIT HOLES**: Risk Assessment의 위험 요소
4. **NO-GOS**: Risk Assessment의 제외 항목
5. **FLOW**: Breadboard에서 정의된 사용자 흐름

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""pitch"",
  ""title"": ""Pitch Board"",
  ""elements"": [
    {{
      ""type"": ""pitchHeader"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 800,
      ""projectName"": ""[프로젝트명]"",
      ""appetite"": ""6주""
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 50,
      ""y"": 100,
      ""width"": 380,
      ""height"": 180,
      ""title"": ""1. PROBLEM"",
      ""content"": ""[Problem Definition 요약]""
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 450,
      ""y"": 100,
      ""width"": 380,
      ""height"": 180,
      ""title"": ""2. SOLUTION"",
      ""content"": ""[Breadboard/Fat Marker 기반 솔루션]""
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 50,
      ""y"": 300,
      ""width"": 380,
      ""height"": 140,
      ""title"": ""3. RABBIT HOLES"",
      ""items"": [
        {{ ""text"": ""[Risk Assessment 위험1]"", ""status"": ""patched"", ""solution"": ""[해결책]"" }}
      ]
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 450,
      ""y"": 300,
      ""width"": 380,
      ""height"": 140,
      ""title"": ""4. NO-GOS"",
      ""items"": [""[Risk Assessment 제외항목]""]
    }},
    {{
      ""type"": ""breadboardMini"",
      ""x"": 50,
      ""y"": 460,
      ""width"": 780,
      ""height"": 100,
      ""title"": ""5. FLOW"",
      ""places"": [
        {{ ""name"": ""[시작]"" }},
        {{ ""name"": ""[중간]"" }},
        {{ ""name"": ""[완료]"" }}
      ]
    }}
  ]
}}

모든 이전 단계의 결과를 일관성 있게 종합하세요.";
    }

    #endregion

    /// <summary>
    /// Generate Problem Definition Board
    /// </summary>
    public static string GetProblemDefinitionPrompt(string prompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
다음 요구사항을 분석하여 Problem Definition Board를 Fabric.js 호환 JSON 형식으로 생성해주세요.

## 요구사항
{prompt}

## Problem Definition Board 구성
1. **Raw Idea**: 원래 아이디어/요청
2. **Narrowed Problem**: 구체화된 문제 정의 (""언제?"" 질문으로 구체화)
3. **Baseline**: 현재 상태 (고객이 현재 하고 있는 것)
4. **Appetite**: 시간 예산 (Small Batch: 1-2주 / Big Batch: 6주)

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""problem"",
  ""title"": ""Problem Definition"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 600,
      ""height"": 400,
      ""title"": ""PROBLEM DEFINITION"",
      ""color"": ""#667eea""
    }},
    {{
      ""type"": ""section"",
      ""x"": 60,
      ""y"": 90,
      ""width"": 280,
      ""height"": 140,
      ""title"": ""Raw Idea"",
      ""content"": ""[원래 아이디어]""
    }},
    {{
      ""type"": ""section"",
      ""x"": 350,
      ""y"": 90,
      ""width"": 280,
      ""height"": 140,
      ""title"": ""Narrowed Problem"",
      ""content"": ""[구체화된 문제]""
    }},
    {{
      ""type"": ""section"",
      ""x"": 60,
      ""y"": 240,
      ""width"": 280,
      ""height"": 140,
      ""title"": ""Baseline"",
      ""content"": ""[현재 상태]""
    }},
    {{
      ""type"": ""appetite"",
      ""x"": 350,
      ""y"": 240,
      ""width"": 280,
      ""height"": 140,
      ""title"": ""Appetite"",
      ""selected"": ""big"",
      ""options"": [""Small Batch (1-2주)"", ""Big Batch (6주)""]
    }}
  ]
}}

요구사항을 분석하여 각 섹션의 content를 적절히 채워주세요.";
    }

    /// <summary>
    /// Generate Breadboard (UI flow and connections)
    /// </summary>
    public static string GetBreadboardPrompt(string prompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
다음 요구사항을 분석하여 Breadboard를 Fabric.js 호환 JSON 형식으로 생성해주세요.

## 요구사항
{prompt}

## Breadboard 구성요소
1. **Places**: 사용자가 갈 수 있는 장소 (화면/페이지) - 밑줄 친 텍스트로 표현
2. **Affordances**: 각 Place에서 사용할 수 있는 것들 (버튼, 필드, 링크) - Place 아래 나열
3. **Connection Lines**: Place 간의 연결 - 화살표로 표현

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""breadboard"",
  ""title"": ""Breadboard"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 800,
      ""height"": 300,
      ""title"": ""BREADBOARD"",
      ""color"": ""#00c6ff""
    }},
    {{
      ""type"": ""place"",
      ""id"": ""place1"",
      ""x"": 80,
      ""y"": 100,
      ""width"": 150,
      ""height"": 120,
      ""name"": ""[화면명]"",
      ""affordances"": [""버튼1"", ""필드1"", ""링크1""]
    }},
    {{
      ""type"": ""place"",
      ""id"": ""place2"",
      ""x"": 280,
      ""y"": 100,
      ""width"": 150,
      ""height"": 120,
      ""name"": ""[다음 화면]"",
      ""affordances"": [""버튼2"", ""필드2""]
    }},
    {{
      ""type"": ""connection"",
      ""from"": ""place1"",
      ""to"": ""place2"",
      ""label"": ""버튼1 클릭""
    }}
  ]
}}

요구사항에서 주요 화면들과 그 흐름을 분석하여 Places와 Connections를 생성하세요.
최대 5개의 Places와 적절한 Connections를 포함하세요.";
    }

    /// <summary>
    /// Generate Fat Marker Sketch (rough UI layout)
    /// </summary>
    public static string GetFatMarkerSketchPrompt(string prompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
다음 요구사항을 분석하여 Fat Marker Sketch를 Fabric.js 호환 JSON 형식으로 생성해주세요.

## 요구사항
{prompt}

## Fat Marker Sketch 규칙
- 굵은 펜으로만 그리기 (세부사항 추가 불가능하게)
- Squiggle lines (~~~~~)로 텍스트 대체
- 박스와 영역으로 구조만 표현
- 픽셀 단위 디자인 금지
- 색상 사용 최소화

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""fat-marker"",
  ""title"": ""Fat Marker Sketch"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 400,
      ""height"": 500,
      ""title"": ""FAT MARKER SKETCH"",
      ""color"": ""#11998e""
    }},
    {{
      ""type"": ""box"",
      ""x"": 70,
      ""y"": 90,
      ""width"": 360,
      ""height"": 40,
      ""label"": ""Header""
    }},
    {{
      ""type"": ""squiggle"",
      ""x"": 80,
      ""y"": 150,
      ""width"": 200
    }},
    {{
      ""type"": ""box"",
      ""x"": 70,
      ""y"": 180,
      ""width"": 170,
      ""height"": 100,
      ""label"": ""Main Content""
    }},
    {{
      ""type"": ""box"",
      ""x"": 250,
      ""y"": 180,
      ""width"": 170,
      ""height"": 100,
      ""label"": ""Sidebar""
    }},
    {{
      ""type"": ""annotation"",
      ""x"": 460,
      ""y"": 180,
      ""text"": ""← 주요 기능 영역"",
      ""color"": ""#764ba2""
    }}
  ]
}}

요구사항에서 주요 UI 레이아웃을 분석하여 박스와 영역을 생성하세요.
annotations으로 중요한 부분에 설명을 추가하세요.";
    }

    /// <summary>
    /// Generate Risk Assessment Board (Rabbit Holes and No-Gos)
    /// </summary>
    public static string GetRiskAssessmentPrompt(string prompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
다음 요구사항을 분석하여 Risk Assessment Board를 Fabric.js 호환 JSON 형식으로 생성해주세요.

## 요구사항
{prompt}

## Risk Assessment Board 구성
1. **Risk Distribution**: Thin-tailed(예측 가능) vs Fat-tailed(불확실) 분포
2. **Rabbit Holes**: 프로젝트를 지연시킬 수 있는 미지의 복잡한 부분
   - 상태: patched(해결책 있음), out-of-bounds(범위 제외), cut-back(축소)
3. **No-Gos**: 명시적으로 제외할 기능/범위

## 리스크 식별 체크리스트
- 기술적으로 해본 적 없는 것?
- 부품들이 어떻게 맞물리는지 가정만 하고 있나?
- 디자인 해결책이 존재한다고 가정하는가?
- 팀이 막힐 수 있는 어려운 결정이 있나?

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""risk"",
  ""title"": ""Risk Assessment"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 700,
      ""height"": 450,
      ""title"": ""RISK & RABBIT HOLES"",
      ""color"": ""#f093fb""
    }},
    {{
      ""type"": ""riskCurve"",
      ""x"": 70,
      ""y"": 90,
      ""curveType"": ""thin-tail"",
      ""label"": ""Target: Thin-tailed""
    }},
    {{
      ""type"": ""rabbitHoleList"",
      ""x"": 70,
      ""y"": 200,
      ""width"": 620,
      ""title"": ""Rabbit Holes"",
      ""items"": [
        {{
          ""description"": ""[위험 요소 1]"",
          ""status"": ""patched"",
          ""solution"": ""[해결책]""
        }},
        {{
          ""description"": ""[위험 요소 2]"",
          ""status"": ""out-of-bounds"",
          ""solution"": ""v1에서 제외""
        }}
      ]
    }},
    {{
      ""type"": ""noGoList"",
      ""x"": 70,
      ""y"": 350,
      ""width"": 620,
      ""title"": ""No-Gos"",
      ""items"": [""[제외 기능 1]"", ""[제외 기능 2]""]
    }}
  ]
}}

요구사항을 분석하여 잠재적 위험 요소와 명시적 제외 항목을 식별하세요.";
    }

    /// <summary>
    /// Generate Pitch Summary Board (complete Shape Up pitch)
    /// </summary>
    public static string GetPitchBoardPrompt(string prompt)
    {
        return $@"당신은 Shape Up 방법론 전문가입니다.
다음 요구사항을 분석하여 완전한 Pitch Board를 Fabric.js 호환 JSON 형식으로 생성해주세요.

## 요구사항
{prompt}

## Pitch Board의 5가지 필수 요소
1. **Problem**: 해결할 구체적 문제 (한 가지 스토리)
2. **Appetite**: 투자할 시간 (2주 or 6주)
3. **Solution**: 해결책 개요 (Fat Marker Sketch 포함 가능)
4. **Rabbit Holes**: 피해야 할 함정들
5. **No-Gos**: 명시적으로 제외할 것들

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 JSON만 출력합니다.

{{
  ""boardType"": ""pitch"",
  ""title"": ""Pitch Board"",
  ""elements"": [
    {{
      ""type"": ""pitchHeader"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 900,
      ""projectName"": ""[프로젝트명]"",
      ""appetite"": ""6주""
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 50,
      ""y"": 120,
      ""width"": 440,
      ""height"": 200,
      ""title"": ""1. PROBLEM"",
      ""content"": ""[문제 설명. 사용자 스토리와 Pain Point 포함]""
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 510,
      ""y"": 120,
      ""width"": 440,
      ""height"": 200,
      ""title"": ""2. SOLUTION"",
      ""content"": ""[솔루션 개요. 핵심 요소 나열]"",
      ""hasSketches"": true,
      ""sketches"": [
        {{
          ""type"": ""box"",
          ""x"": 10,
          ""y"": 80,
          ""width"": 200,
          ""height"": 80,
          ""label"": ""[UI 영역]""
        }}
      ]
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 50,
      ""y"": 340,
      ""width"": 440,
      ""height"": 150,
      ""title"": ""3. RABBIT HOLES"",
      ""items"": [
        {{ ""text"": ""[위험 1]"", ""status"": ""patched"", ""solution"": ""[해결책]"" }},
        {{ ""text"": ""[위험 2]"", ""status"": ""cut-back"" }}
      ]
    }},
    {{
      ""type"": ""pitchSection"",
      ""x"": 510,
      ""y"": 340,
      ""width"": 440,
      ""height"": 150,
      ""title"": ""4. NO-GOS"",
      ""items"": [""[제외 기능 1]"", ""[제외 기능 2]"", ""[제외 기능 3]""]
    }},
    {{
      ""type"": ""breadboardMini"",
      ""x"": 50,
      ""y"": 510,
      ""width"": 900,
      ""height"": 120,
      ""title"": ""5. FLOW"",
      ""places"": [
        {{ ""name"": ""[시작]"", ""affordances"": [""버튼""] }},
        {{ ""name"": ""[중간]"", ""affordances"": [""폼""] }},
        {{ ""name"": ""[완료]"", ""affordances"": [""확인""] }}
      ],
      ""connections"": [""시작 → 중간"", ""중간 → 완료""]
    }}
  ]
}}

요구사항을 분석하여 완전한 Pitch Board를 생성하세요.
Problem과 Solution은 구체적으로 작성하고, Rabbit Holes와 No-Gos는 실질적인 항목을 포함하세요.";
    }
}
