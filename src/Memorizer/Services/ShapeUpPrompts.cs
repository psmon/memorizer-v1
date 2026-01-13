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
            "freeboard" => GetFreeBoardPrompt(prompt),
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

    /// <summary>
    /// Generate Free Board (custom visualization based on user prompt)
    /// </summary>
    public static string GetFreeBoardPrompt(string prompt)
    {
        return $@"당신은 시각화 전문가입니다.
다음 요청을 분석하여 화이트보드에 표시할 수 있는 다이어그램/보드를 Fabric.js 호환 JSON 형식으로 생성해주세요.

## 사용자 요청
{prompt}

## 지원하는 요소 타입

### 기본 도형 (반드시 고유한 id 부여)
- **rect**: 사각형 (id, x, y, width, height, fill, stroke, label)
- **circle**: 원 (id, x, y, radius, fill, stroke, label)
- **text**: 텍스트 (id, x, y, text, fontSize, fill)

### 연결 요소 (현재 비활성화 - 사용하지 마세요)
<!-- 추후 지원 예정: arrow, connector -->

### 구조화된 요소 (반드시 고유한 id 부여)
- **entity**: 엔티티 박스 - ERD용 (id, x, y, width, name, fields: [string])
- **note**: 메모/포스트잇 (id, x, y, width, height, text, color)
- **label**: 라벨 텍스트 (id, x, y, text, fontSize, color)
- **group**: 요소 그룹화 (id, x, y, width, height, title, color, children: [elements])

### 레이아웃 요소 (반드시 고유한 id 부여)
- **frame**: 프레임 박스 (id, x, y, width, height, title, color)
- **section**: 섹션 박스 (id, x, y, width, height, title, content)

### SVG 요소 (벡터 드로잉) - ""그림"", ""아이콘"", ""도형"", ""SVG"" 요청 시 적극 활용!
- **svgbox**: 커스텀 SVG 드로잉 (id, x, y, width, height, path, stroke, strokeWidth)
  - **path**: SVG path 데이터 (d attribute) - 자유로운 도형/그림 표현
  - viewBox 0 0 24 24 기준으로 path 작성
- **svgicon**: 프리셋 아이콘 (id, x, y, iconId, size)
  - iconId: arrow-right, arrow-left, arrow-up, arrow-down, user, users, cloud, server, database, monitor, globe, lock, gear, document, folder

### SVG Path 레퍼런스 (viewBox 0 0 24 24)
별: M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z
하트: M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z
번개: M13 2L3 14h9l-1 8 10-12h-9l1-8z
체크: M20 6L9 17l-5-5
X: M18 6L6 18M6 6l12 12
다이아몬드: M12 2L2 12l10 10 10-10L12 2z
헥사곤: M21 16.5V7.5L12 2 3 7.5v9L12 22l9-5.5z
3D박스: M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z
화살표→: M5 12h14M12 5l7 7-7 7
양방향↔: M5 12h14M5 12l4-4M5 12l4 4M19 12l-4-4M19 12l-4 4
사람: M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z
서버: M2 4h20v6H2zM2 14h20v6H2zM6 7h.01M6 17h.01
DB: M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4
클라우드: M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 순수 JSON만 출력합니다.

{{
  ""boardType"": ""freeboard"",
  ""title"": ""[보드 제목]"",
  ""elements"": [
    // 요소들을 여기에 배치
  ]
}}

## 예시 1: 프로젝트 계획 보드
{{
  ""boardType"": ""freeboard"",
  ""title"": ""프로젝트 계획"",
  ""elements"": [
    {{
      ""type"": ""frame"",
      ""id"": ""frame1"",
      ""x"": 50,
      ""y"": 50,
      ""width"": 800,
      ""height"": 300,
      ""title"": ""프로젝트 개요"",
      ""color"": ""#4A90D9""
    }},
    {{
      ""type"": ""section"",
      ""id"": ""phase1"",
      ""x"": 70,
      ""y"": 100,
      ""width"": 230,
      ""height"": 150,
      ""title"": ""Phase 1"",
      ""content"": ""요구사항 분석\n설계 문서 작성""
    }},
    {{
      ""type"": ""section"",
      ""id"": ""phase2"",
      ""x"": 320,
      ""y"": 100,
      ""width"": 230,
      ""height"": 150,
      ""title"": ""Phase 2"",
      ""content"": ""개발\n테스트""
    }},
    {{
      ""type"": ""section"",
      ""id"": ""phase3"",
      ""x"": 570,
      ""y"": 100,
      ""width"": 230,
      ""height"": 150,
      ""title"": ""Phase 3"",
      ""content"": ""배포\n운영""
    }},
    {{
      ""type"": ""text"",
      ""id"": ""flow"",
      ""x"": 70,
      ""y"": 270,
      ""text"": ""[진행 흐름] Phase 1 → Phase 2 → Phase 3"",
      ""fontSize"": 14,
      ""fill"": ""#4A90D9""
    }}
  ]
}}

## 예시 2: ERD 다이어그램
{{
  ""boardType"": ""freeboard"",
  ""title"": ""ERD 다이어그램"",
  ""elements"": [
    {{
      ""type"": ""entity"",
      ""id"": ""user"",
      ""x"": 100,
      ""y"": 100,
      ""width"": 180,
      ""name"": ""User"",
      ""fields"": [""id: int PK"", ""name: varchar"", ""email: varchar""]
    }},
    {{
      ""type"": ""entity"",
      ""id"": ""order"",
      ""x"": 350,
      ""y"": 100,
      ""width"": 180,
      ""name"": ""Order"",
      ""fields"": [""id: int PK"", ""user_id: int FK"", ""total: decimal""]
    }},
    {{
      ""type"": ""entity"",
      ""id"": ""product"",
      ""x"": 600,
      ""y"": 100,
      ""width"": 180,
      ""name"": ""Product"",
      ""fields"": [""id: int PK"", ""name: varchar"", ""price: decimal""]
    }},
    {{
      ""type"": ""text"",
      ""id"": ""relations"",
      ""x"": 100,
      ""y"": 220,
      ""text"": ""[관계] User → Order (1:N) | Order ↔ Product (N:M)"",
      ""fontSize"": 14,
      ""fill"": ""#666""
    }}
  ]
}}

## 예시 3: 팀 회의 보드
{{
  ""boardType"": ""freeboard"",
  ""title"": ""팀 회의"",
  ""elements"": [
    {{
      ""type"": ""text"",
      ""id"": ""title"",
      ""x"": 50,
      ""y"": 20,
      ""text"": ""2024년 1분기 계획 회의"",
      ""fontSize"": 24,
      ""fill"": ""#333""
    }},
    {{
      ""type"": ""note"",
      ""id"": ""topic1"",
      ""x"": 50,
      ""y"": 60,
      ""width"": 200,
      ""height"": 150,
      ""text"": ""논의 주제 1\n- 세부사항 A\n- 세부사항 B"",
      ""color"": ""#ffeb3b""
    }},
    {{
      ""type"": ""note"",
      ""id"": ""action1"",
      ""x"": 280,
      ""y"": 60,
      ""width"": 200,
      ""height"": 150,
      ""text"": ""Action Items\n- 담당자: 김철수\n- 마감: 금요일"",
      ""color"": ""#4caf50""
    }},
    {{
      ""type"": ""note"",
      ""id"": ""topic2"",
      ""x"": 510,
      ""y"": 60,
      ""width"": 200,
      ""height"": 150,
      ""text"": ""결정 사항\n- 일정 확정\n- 역할 분담"",
      ""color"": ""#e1bee7""
    }}
  ]
}}

## 예시 4: 시스템 아키텍처 (SVG path 활용)
{{
  ""boardType"": ""freeboard"",
  ""title"": ""시스템 아키텍처"",
  ""elements"": [
    {{
      ""type"": ""text"",
      ""id"": ""title"",
      ""x"": 50,
      ""y"": 20,
      ""text"": ""클라우드 시스템 아키텍처"",
      ""fontSize"": 24,
      ""fill"": ""#333""
    }},
    {{
      ""type"": ""svgbox"",
      ""id"": ""user-svg"",
      ""x"": 50,
      ""y"": 80,
      ""width"": 100,
      ""height"": 100,
      ""path"": ""M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z"",
      ""stroke"": ""#4A90D9"",
      ""strokeWidth"": 2
    }},
    {{
      ""type"": ""text"",
      ""id"": ""user-label"",
      ""x"": 70,
      ""y"": 190,
      ""text"": ""사용자"",
      ""fontSize"": 14,
      ""fill"": ""#4A90D9""
    }},
    {{
      ""type"": ""svgbox"",
      ""id"": ""cloud-svg"",
      ""x"": 250,
      ""y"": 80,
      ""width"": 120,
      ""height"": 100,
      ""path"": ""M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z"",
      ""stroke"": ""#11998e"",
      ""strokeWidth"": 2
    }},
    {{
      ""type"": ""text"",
      ""id"": ""cloud-label"",
      ""x"": 275,
      ""y"": 190,
      ""text"": ""클라우드"",
      ""fontSize"": 14,
      ""fill"": ""#11998e""
    }},
    {{
      ""type"": ""svgbox"",
      ""id"": ""db-svg"",
      ""x"": 470,
      ""y"": 80,
      ""width"": 100,
      ""height"": 100,
      ""path"": ""M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4"",
      ""stroke"": ""#f093fb"",
      ""strokeWidth"": 2
    }},
    {{
      ""type"": ""text"",
      ""id"": ""db-label"",
      ""x"": 475,
      ""y"": 190,
      ""text"": ""데이터베이스"",
      ""fontSize"": 14,
      ""fill"": ""#f093fb""
    }},
    {{
      ""type"": ""text"",
      ""id"": ""flow"",
      ""x"": 50,
      ""y"": 220,
      ""text"": ""[데이터 흐름] 클라이언트 → API 서버 → 데이터베이스"",
      ""fontSize"": 14,
      ""fill"": ""#666""
    }}
  ]
}}

## 생성 규칙
1. 모든 요소에 고유한 id를 부여하세요 (예: ""user"", ""order"", ""phase1"")
2. **요소 겹침 방지 (매우 중요)**:
   - 모든 요소(도형, 텍스트, 라벨)가 서로 겹치지 않도록 배치하세요
   - 요소 간 최소 간격: 가로 30px, 세로 25px 이상 유지
   - 텍스트/라벨은 해당 도형 내부 또는 바로 아래에만 배치 (다른 요소 영역 침범 금지)
   - 긴 텍스트는 width를 충분히 확보하거나 줄바꿈(\n) 사용
3. **레이아웃 계획**:
   - 먼저 전체 레이아웃 구조를 계획한 후 요소 배치
   - 그리드 기반 배치 권장 (x: 50, 300, 550, 800 / y: 50, 200, 350, 500)
   - 요소 크기를 먼저 결정하고, 그 크기에 맞게 다음 요소 위치 계산
4. 그룹화가 필요한 경우 frame이나 group을 사용하세요
5. 가독성을 위해 적절한 색상을 사용하세요
6. 캔버스 크기 (1200x800)를 고려하여 배치하세요
7. **중요: 화살표(arrow, connector)는 사용하지 마세요** - 연결선은 사용자가 직접 그립니다
8. **연결 관계가 필요한 경우**: 별도의 text 요소를 사용하여 관계를 글로 설명하세요
   - 예: ""User → Order (1:N)"", ""Phase1 → Phase2 → Phase3""
   - 보드 하단이나 적절한 위치에 ""관계 설명"" 텍스트 블록을 배치
9. **시스템/아키텍처 다이어그램**: svgbox와 svgicon을 활용하여 시각적으로 표현
   - 서버: server, server-stack 아이콘
   - 데이터베이스: database, database-alt 아이콘
   - 클라우드: cloud, cloud-upload, cloud-download 아이콘
   - 사용자: user, users 아이콘

사용자의 요청에 맞는 시각화 보드를 생성하세요. **절대로 요소가 겹치지 않도록 주의하세요.**";
    }

    #region Memory Search Support for Free Board

    /// <summary>
    /// Extract 3 search keywords from Free Board prompt for memory search
    /// </summary>
    public static string GetSearchKeywordsExtractionPrompt(string prompt)
    {
        return $@"당신은 키워드 추출 전문가입니다.
아래 보드 생성 요청을 분석하여 관련 기술이나 도메인 지식을 검색하기 위한 핵심 키워드 3개를 추출해주세요.

## 보드 생성 요청
{prompt}

## 규칙
- 반드시 3개의 핵심 검색어를 추출 (각각 20자 이내)
- 각 키워드는 서로 다른 관점에서 추출:
  1. 주제/도메인 관련 키워드
  2. 구조/패턴 관련 키워드
  3. 기술/도구 관련 키워드
- 한국어 또는 영어 모두 가능
- 쉼표로 구분하여 키워드만 출력 (설명, 따옴표, 번호 없이)

키워드:";
    }

    /// <summary>
    /// Evaluate if memory is useful for Free Board creation
    /// </summary>
    public static string GetMemoryUsefulnessPrompt(string boardPrompt, string memoryTitle, string memoryContent)
    {
        return $@"당신은 참고자료 평가 전문가입니다.
아래 보드 생성 요청에 대해 참고 자료가 유용한지 판단해주세요.

## 보드 생성 요청 (요약)
{boardPrompt.Substring(0, Math.Min(boardPrompt.Length, 500))}

## 검색된 메모리
제목: {memoryTitle}
내용: {memoryContent.Substring(0, Math.Min(memoryContent.Length, 500))}

## 판단 기준
- 보드 생성 요청의 주제/도메인과 관련이 있는가?
- 보드 구성에 참고할 만한 구조적 정보가 있는가?
- 시각화에 도움이 될 수 있는 예시나 패턴이 있는가?

## 응답 형식
유용함 또는 유용하지않음 중 하나만 출력하세요.

판단:";
    }

    /// <summary>
    /// Batch evaluate multiple memories and select top 3 most relevant
    /// </summary>
    public static string GetBatchMemoryRelevancePrompt(string boardPrompt, List<(string Title, string Summary, int Index)> candidates)
    {
        var candidateList = new System.Text.StringBuilder();
        foreach (var (title, summary, index) in candidates)
        {
            candidateList.AppendLine($"[{index}] 제목: {title}");
            candidateList.AppendLine($"    요약: {summary}");
            candidateList.AppendLine();
        }

        return $@"당신은 참고자료 평가 전문가입니다.
아래 보드 생성 요청에 가장 연관성 높은 메모리 3개를 선택해주세요.

## 보드 생성 요청
{boardPrompt.Substring(0, Math.Min(boardPrompt.Length, 800))}

## 후보 메모리 목록
{candidateList}

## 선택 기준
1. 보드 생성 요청의 주제/도메인과 직접적 관련성
2. 시각화에 활용 가능한 구체적 정보 포함 여부
3. 보드 구성에 참고할 수 있는 패턴/구조 제공 여부

## 응답 형식
가장 연관성 높은 순서대로 3개의 인덱스를 쉼표로 구분하여 출력하세요.
연관성 있는 메모리가 3개 미만이면 연관성 있는 것만 출력하세요.
연관성 있는 메모리가 없으면 '없음'을 출력하세요.

예시: 2,5,1 또는 3,1 또는 없음

선택:";
    }

    /// <summary>
    /// Generate Free Board with memory references
    /// </summary>
    public static string GetFreeBoardWithMemoryPrompt(string prompt, string memoryReferences)
    {
        return $@"당신은 시각화 전문가입니다.
다음 요청을 분석하여 화이트보드에 표시할 수 있는 다이어그램/보드를 Fabric.js 호환 JSON 형식으로 생성해주세요.
참고 자료를 활용하여 더 풍부하고 정확한 보드를 생성하세요.

## 사용자 요청
{prompt}

## 참고 자료 (메모리에서 검색됨)
{memoryReferences}

## 지원하는 요소 타입

### 기본 도형 (반드시 고유한 id 부여)
- **rect**: 사각형 (id, x, y, width, height, fill, stroke, label)
- **circle**: 원 (id, x, y, radius, fill, stroke, label)
- **text**: 텍스트 (id, x, y, text, fontSize, fill)

### 구조화된 요소 (반드시 고유한 id 부여)
- **entity**: 엔티티 박스 - ERD용 (id, x, y, width, name, fields: [string])
- **note**: 메모/포스트잇 (id, x, y, width, height, text, color)
- **label**: 라벨 텍스트 (id, x, y, text, fontSize, color)
- **group**: 요소 그룹화 (id, x, y, width, height, title, color, children: [elements])

### 레이아웃 요소 (반드시 고유한 id 부여)
- **frame**: 프레임 박스 (id, x, y, width, height, title, color)
- **section**: 섹션 박스 (id, x, y, width, height, title, content)

### SVG 요소 (벡터 드로잉) - ""그림"", ""아이콘"", ""도형"", ""SVG"" 요청 시 적극 활용!
- **svgbox**: 커스텀 SVG 드로잉 (id, x, y, width, height, path, stroke, strokeWidth)
  - **path**: SVG path 데이터 - 자유로운 도형/그림 표현 (viewBox 0 0 24 24 기준)
- **svgicon**: 프리셋 아이콘 (id, x, y, iconId, size)
  - iconId: arrow-right, arrow-left, arrow-up, arrow-down, user, users, cloud, server, database, monitor, globe, lock, gear, document, folder

### SVG Path 레퍼런스
별: M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z
하트: M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z
번개: M13 2L3 14h9l-1 8 10-12h-9l1-8z
체크: M20 6L9 17l-5-5
다이아몬드: M12 2L2 12l10 10 10-10L12 2z
화살표: M5 12h14M12 5l7 7-7 7
사람: M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z
서버: M2 4h20v6H2zM2 14h20v6H2zM6 7h.01M6 17h.01
DB: M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4
클라우드: M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z

### 메모리 참조 표시 요소 (참고자료 활용 시 필수)
- **memoryRef**: 참고 메모리 정보 표시 (id, x, y, width, title, similarity, keyword)

## 출력 형식 (Fabric.js JSON)
다음 JSON 구조로 정확히 출력하세요. 코드 블록 없이 순수 JSON만 출력합니다.

{{
  ""boardType"": ""freeboard"",
  ""title"": ""[보드 제목]"",
  ""memoryReferences"": [
    {{
      ""title"": ""[참고한 메모리 제목]"",
      ""keyword"": ""[검색 키워드]"",
      ""similarity"": 0.85
    }}
  ],
  ""elements"": [
    // 요소들을 여기에 배치
    // 마지막에 참고 메모리 정보 표시 영역 추가
  ]
}}

## 참고 메모리 표시 규칙
보드 하단에 참고한 메모리 정보를 표시하세요:
{{
  ""type"": ""frame"",
  ""id"": ""memory-refs"",
  ""x"": 50,
  ""y"": 600,
  ""width"": 400,
  ""height"": 100,
  ""title"": ""참고 메모리"",
  ""color"": ""#e8f4fd""
}},
{{
  ""type"": ""text"",
  ""id"": ""memory-ref-1"",
  ""x"": 60,
  ""y"": 640,
  ""text"": ""[메모리 제목] (연관성: 85%)"",
  ""fontSize"": 12,
  ""fill"": ""#666""
}}

## 생성 규칙
1. 모든 요소에 고유한 id를 부여하세요
2. **요소 겹침 방지 (매우 중요)**:
   - 모든 요소(도형, 텍스트, 라벨)가 서로 겹치지 않도록 배치하세요
   - 요소 간 최소 간격: 가로 30px, 세로 25px 이상 유지
   - 텍스트/라벨은 해당 도형 내부 또는 바로 아래에만 배치 (다른 요소 영역 침범 금지)
   - 긴 텍스트는 width를 충분히 확보하거나 줄바꿈(\n) 사용
3. **레이아웃 계획**:
   - 먼저 전체 레이아웃 구조를 계획한 후 요소 배치
   - 그리드 기반 배치 권장 (x: 50, 300, 550, 800 / y: 50, 200, 350, 500)
   - 요소 크기를 먼저 결정하고, 그 크기에 맞게 다음 요소 위치 계산
4. 가독성을 위해 적절한 색상을 사용하세요
5. 캔버스 크기 (1200x800)를 고려하여 배치하세요
6. **중요: 화살표(arrow, connector)는 사용하지 마세요**
7. **참고 자료의 정보를 보드 구성에 적극 활용하세요**
8. **보드 하단에 참고한 메모리 정보를 표시하세요**
9. **시스템/아키텍처 다이어그램**: svgbox와 svgicon을 활용하여 시각적으로 표현
   - 서버: server, server-stack 아이콘
   - 데이터베이스: database, database-alt 아이콘
   - 클라우드: cloud, cloud-upload, cloud-download 아이콘
   - 사용자: user, users 아이콘

사용자의 요청과 참고 자료를 활용하여 풍부한 시각화 보드를 생성하세요. **절대로 요소가 겹치지 않도록 주의하세요.**";
    }

    /// <summary>
    /// Summarize a long prompt for wireframe generation
    /// Preserves core features, UI components, and user flow
    /// </summary>
    public static string GetWireframeSummarizationPrompt(string longPrompt)
    {
        return $@"당신은 요구사항 요약 전문가입니다.
아래의 긴 요청 내용을 와이어프레임 생성에 적합하도록 2000자 이내로 요약해주세요.

## 원본 요청
{longPrompt}

## 요약 규칙
반드시 다음 내용을 우선적으로 유지하세요:
1. **핵심 기능**: 구현해야 할 주요 기능과 특징
2. **화면 구성 요소**: 버튼, 폼, 테이블, 카드 등 UI 컴포넌트
3. **사용자 흐름**: 화면 간 이동, 상호작용 순서
4. **데이터 표시**: 표시해야 할 데이터 항목

다음 내용은 제거하거나 간략화하세요:
- 불필요한 설명이나 배경 정보
- 중복된 내용
- 기술적 세부 구현 사항
- 부가적인 비기능 요구사항

## 출력 형식
요약된 요청만 출력하세요. 설명이나 마크업 없이 본문만 작성합니다.

요약:";
    }

    #endregion
}
