namespace Memorizer.Services;

/// <summary>
/// LLM prompts for Memory Architecture feature
/// Combines two memories to generate creative architecture ideas and implementations
/// </summary>
public static class ArchitecturePrompts
{
    /// <summary>
    /// Generate a creative idea prompt from two memory contents
    /// </summary>
    public static string GetIdeaGenerationPrompt(string memory1Title, string memory1Content, string memory2Title, string memory2Content)
    {
        return $@"You are a creative software architect who combines different concepts to create innovative ideas.

Given two different memories/knowledge bases, create a creative and innovative idea that combines elements from both.

## Memory A: {memory1Title}
{memory1Content}

## Memory B: {memory2Title}
{memory2Content}

## Task
Create a creative idea that combines key concepts from both memories. The idea should:
1. Be innovative and practical
2. Leverage strengths from both concepts
3. Create synergy between the two domains
4. Be technically feasible

## Response Format (Markdown)
Format your response in Markdown with the following structure:

### [아이디어 제목을 여기에 직접 작성]
(예: ### 액터-그래프 시각화 플랫폼)

### 개요
2-3문장으로 아이디어의 핵심 컨셉과 두 메모리가 어떻게 융합되는지 설명.

### 가치 제안
1-2문장으로 이 조합의 고유한 장점과 강력한 이유 설명.

### 핵심 요소
- **Memory A 활용**: 활용되는 핵심 요소
- **Memory B 활용**: 활용되는 핵심 요소
- **시너지**: 두 요소가 어떻게 함께 작동하는지

**중요: 반드시 한국어로 응답하세요. 첫 번째 ### 헤딩에 아이디어 제목을 직접 작성하세요 (라벨 없이).**";
    }

    /// <summary>
    /// Generate an architecture implementation prompt
    /// </summary>
    public static string GetArchitectureGenerationPrompt(
        string memory1Title,
        string memory1Content,
        string memory2Title,
        string memory2Content,
        string ideaPrompt,
        string targetLanguage = "C#")
    {
        return $@"You are an expert software architect who designs practical and elegant architectures.

## Context
Two knowledge domains are being combined to create a new architecture.

### Memory A: {memory1Title}
{memory1Content}

### Memory B: {memory2Title}
{memory2Content}

### Creative Idea to Implement
{ideaPrompt}

## Task
Design a comprehensive architecture that implements the above idea. Your response should include:

### 1. Architecture Overview
Provide a clear, easy-to-understand explanation of the architecture (2-3 paragraphs).

### 2. System Diagram
Create a Mermaid diagram showing the main components and their relationships.
Use appropriate diagram type (flowchart, sequence, class, etc.) based on what best illustrates the architecture.

```mermaid
[Your diagram here]
```

### 3. Key Components
List and briefly describe the main components/modules of the system.

### 4. Sample Implementation
Provide a sample code snippet in {targetLanguage} that demonstrates a key part of the architecture.

```{targetLanguage.ToLower()}
// Sample implementation code
```

### 5. Integration Points
Explain how Memory A and Memory B concepts are integrated in this architecture.

## Guidelines
- Keep explanations concise but comprehensive
- Use practical, production-ready patterns
- Include error handling considerations
- Consider scalability and maintainability

**중요: 반드시 한국어로 응답하세요. All responses must be in Korean.**";
    }

    /// <summary>
    /// Get regeneration prompt for idea with different angle
    /// </summary>
    public static string GetIdeaRegenerationPrompt(string memory1Title, string memory1Content, string memory2Title, string memory2Content, string previousIdea)
    {
        return $@"You are a creative software architect who combines different concepts to create innovative ideas.

Given two different memories/knowledge bases, create a NEW creative idea (different from the previous one) that combines elements from both.

## Memory A: {memory1Title}
{memory1Content}

## Memory B: {memory2Title}
{memory2Content}

## Previous Idea (generate something completely different)
{previousIdea}

## Task
Create a DIFFERENT creative idea that:
1. Takes a completely different angle or approach
2. Focuses on different aspects of the memories
3. Proposes an alternative combination strategy
4. Is equally innovative and practical

## Response Format (Markdown)
Format your response in Markdown with the following structure:

### [새로운 아이디어 제목을 여기에 직접 작성]
(예: ### 리액티브 이벤트 스토어)

### 개요
2-3문장으로 아이디어의 핵심 컨셉과 두 메모리가 어떻게 융합되는지 설명.

### 가치 제안
1-2문장으로 이 조합의 고유한 장점과 강력한 이유 설명.

### 핵심 요소
- **Memory A 활용**: 활용되는 핵심 요소
- **Memory B 활용**: 활용되는 핵심 요소
- **시너지**: 두 요소가 어떻게 함께 작동하는지

**중요: 반드시 한국어로 응답하세요. 첫 번째 ### 헤딩에 아이디어 제목을 직접 작성하세요 (라벨 없이).**";
    }

    /// <summary>
    /// Supported programming languages for code samples
    /// </summary>
    public static readonly string[] SupportedLanguages = new[]
    {
        "C#",
        "Python",
        "Java",
        "TypeScript",
        "JavaScript",
        "Go",
        "Rust",
        "Kotlin",
        "Swift",
        "Ruby",
        "PHP",
        "Scala"
    };
}
