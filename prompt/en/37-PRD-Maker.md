This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, and also enables graph exploration using Neo4j.
Additionally, it implements an ASK conversational bot feature using LLM, and provides various AI features through the MCP interface.

Please follow the instructions by referring to the project location, description, and local testing method.

# Instructions
- We want to add LLM-EX settings.
  - LLM will be used as-is for existing features, and LLM-EX will be used for advanced features that require deep analysis.
  - Since testing is possible in the current local development environment, please verify that the function call is possible first (curl-test) before proceeding with implementation.
    - If access is not possible, it may be a network environment issue, so please let us know and we will provide support.
```
  "LLM-EX": {
    "Type": "Custom",
    "ApiUrl": "http://192.168.0.68:1234",
    "Model": "openai/gpt-oss-120b",
    "Timeout": "00:05:00"
  },
```
- We want to implement PRD Maker as a new feature.
- The menu structure is as follows and can be used without authentication. Check the existing authentication router to make it work without authentication.
  - Memory Architect: An already implemented feature, the new menu will be added next to this.
  - PRD Maker: The newly implemented feature page
    - PRD Shared Page: View shared pages after PRD creation. (Implement with a new table by referring to the existing /ui/askbot/share/{shortlink} functionality)
- Detailed functionality is described in the PRD Maker instructions below.

## PRD Maker Instructions
We want to create a web page that interprets story-level PRD using Event Storming.
The page consists of a textarea and buttons.

When the user enters PRD content in the textarea and clicks the button, the Event Storming results are displayed.

After the Event Storming results appear, the Example Mapping function is performed next.
Based on the Event Storming results, virtual collaborators create Example Mapping through discussion.
The discussion process is completed.

After the discussion is completed, Example Mapping creation is attempted next.
Complete the Example Mapping.

- Event Storming is generated according to DDD principles.
- Event Storming results are output in the following format:
    - Events
    - Commands
    - Actors
    - Policies/Conditions/Constraints
    - Aggregates/Bounded Contexts
- Example Mapping includes:
    - User Story: A brief description of the feature or requirement to be discussed
    - Example: Specific cases or scenarios related to the user story
    - Rule: Business rules of the domain revealed through examples
    - Question: Ambiguous parts or items requiring additional verification during discussion
- Events are expressed according to the flow sequence of the business domain.
- After completion, a Share Results button appears to share results via short link.
- PRD Shared Page displays all shared results in a card-style page view.

### Additional Instructions
- Use the LLM newly configured in LLM-EX.
- Design prompts to achieve the goal.
- Clearly instruct to respond in Korean and use streaming for natural display.
  - When using LLM streaming, use SSE method for real-time reflection.
- Generate results in markdown style with support for Mermaid diagrams and code blocks.
  - Implement a viewer that expresses the style by referring to the /ui/view/{id} page view. (Utilize related modules)

### Additional Improvements
- In Event Storming, Mermaid is displayed as code style - display it as a diagram
- The progression happens all at once automatically - improve so users can progress step by step by clicking buttons
  - Additional feature: After Example Mapping results, as the next feature, generate completed suggested requirements referring to the Example Mapping.
- Add the following feature introduction to PRD Maker:
  - Acknowledge that story requirements may be incomplete and start with that - guide that it's okay to start with insufficient requirements
  - Guide that retry is possible with a new PRD by supplementing requirements at the last step
  - At the bottom of the PRD Maker page, a simple Mermaid diagram guides the process.

## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Only resolve build errors.
- After fixing build errors, testing will be done manually and modifications will be made based on feedback.
- Consider the ASPNETCORE_ENVIRONMENT=Sam environment variable and reflect any additional environments.
  - src/Memorizer/appsettings.Sam.json file location
