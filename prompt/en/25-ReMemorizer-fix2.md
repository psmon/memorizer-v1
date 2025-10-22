
This project uses .NET 9.0 and consists of a full-stack (API, UI) architecture.
It supports search and vector search using PostgreSQL, and graph traversal using Neo4j.

The following features have been recently added and are working well:
- The ui/askbot/share/{shortlink} page displays shared chatbot conversation content.
  - This page is accessible without authentication and allows registering shared conversations as memories.
    - Memory registration requires authorization, so authentication is needed.
    - Uses the MemoryTools Store function to save.
      - After saving, if there are reference documents, memories can be linked via relations.
      - LLM is utilized to perform this function.
- Shared chatbot conversation content can be converted to memory using "Save As Memory".

The following improvements and minor enhancements will be made according to the improvement guidelines.

# Improvement Guidelines
- Supports Markdown but Mermaid rendering is partially broken. Check for frontend issues and refer to working examples.
  - Broken locations: ui/askbot, ui/askbot/share/{shortlink}
    - Error: "Syntax error in text mermaid version 10.6.0"
  - Working locations: ui/view, ui/blog - ViewMore

## Broken Sample Code
```
Original Mermaid Code
flowchart TD

  %% ---------- Pillars ----------
  spec[Specification]
  rules[Rules]
  oversight[Oversight]

  %% ---------- Specification ----------
  spec --> goal("1️⃣ Define Goal\n• Result & success criteria")
  spec --> scope("2️⃣ Set Scope\n• Include / exclude")
  spec --> us("3️⃣ User Stories\n• Who, What, Why")

  %% ---------- Rules ----------
  rules --> pref("1️⃣ Coding Preferences\n• SOLID, DRY, KISS")
  rules --> stack("2️⃣ Tech‑Stack\n• Language, framework, tools")
  rules --> wf("3️⃣ Workflow\n• Steps, checkpoints")
  rules --> comm("4️⃣ Communication\n• Prompt format, change tags")

  %% ---------- Rule‑File Structure ----------
  pref --> file("📂 .cursor/rules/\n├─ coding-preferences.md\n├─ tech‑stack.md\n├─ workflow‑preferences.md\n└─ communication-preferences.md")

  %% ---------- Oversight ----------
  oversight --> pre("1️⃣ Pre‑review\n• Plan & design approval")
  oversight --> mid("2️⃣ Mid‑process\n• Checkpoints, quick reviews")
  oversight --> post("3️⃣ Post‑process\n• Metrics, retrospectives")

  %% ---------- PDCA Loop ----------
  subgraph pdca[PDCA Continuous Improvement]
    plan("Plan\n• Collect data & analyse")
    do("Do\n• Update rules, prompts, tools")
    check("Check\n• A/B test, metrics, feedback")
    act("Act\n• Standardise wins, rollback fails")
  end

  %% ---------- Metrics ----------
  subgraph metrics[Key Performance Indicators]
    eff("Efficiency\n• LOC/token > 0.5, <3 iterations")
    quality("Quality\n• Bugs/1k LOC < 2, Coverage > 85%")
    prod("Productivity\n• 2× speed, <1 context switch/day")
  end

  %% ---------- Connections ----------
  spec --> rules
  rules --> oversight

  oversight --> pdca
  pdca --> metrics

  %% ---------- Optional Enhancements ----------
  subgraph auto[Workflow Automation]
    trigger("Trigger\n• PR created / updated")
    lint("Lint + format\n• Pre‑commit hooks")
    test("Run tests\n• CI pipeline")
    review("Automated review\n• AI comment generation")
  end

  auto --> trigger
  trigger --> lint
  lint --> test
  test --> review
  review --> oversight

  %% ---------- End ----------
```
- When sharing in ui/askbot, if the same session has already been shared, it doesn't update. Improve it to update existing data.


## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can utilize LLM and embedding.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to them when actor models are needed.
  - ChatBotActor.cs - Actor model that processes conversation requests by connecting with users via SSE to edge. Generates responses using search and decision actors.
  - SearchMemoryActor.cs - Actor model responsible for memory search.
  - DecisionActor.cs - Actor model that determines whether searched memory is relevant to the request.
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Processes metadata embedding asynchronously during memory registration. Vectorizes using embedding in this process.
  - GraphsyncActor.cs - Actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test projects.
- PageUrl: Has the following page URLs:
    - ui: Includes content View with Edit, Delete functions, and Vector Search is also available.
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by id
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - Shared chatbot conversation content view page
    - ui/sharelist - Shared chatbot conversation content list page
- src/Memorizer.IntegrationTests/Actors: Existing unit tests are available. Refer to unit testing methods

## Local Testing Methods
- Can be tested locally using docker-compose.local-psmon.yml. Use this file if there are Docker operation issues.
- The basic configuration may already be running. Don't use the down command to stop everything; only rebuild and restart memorizer-app
  - postgmem-postgres has a well-functioning DB setup and sample data included in the database. Do not down this component especially, treat it carefully as if it were a production DB
- This application is built and operated through memorizer-app. When rebuild/restart is needed, use only Docker without dotnet CLI
- For actor model unit testing methods, refer to the following. When creating an actor model, write or update unit tests for that actor.
  - Write actor model tests in src/Memorizer.IntegrationTests/Actors location.
- When API testing is needed, test using the Client available in .NET. Use appropriate client modules when SSE or WebSocket is needed.

## API Authentication Method During Testing
- Check src/Memorizer/Controllers/AuthController.cs code, pass API authentication, and proceed through the registration API.
- Note: View is unauthenticated, memory registration is only possible through authenticated API. After successful login with admin/admin123, use the returned cookie value
- src/Memorizer/Controllers/MemoryController.cs Memory registration uses CreateMemory... Don't manipulate the DB directly, understand and utilize the provided APIs

## Additional Improvements - QA
- When pressing the share button in ui/askbot, a unique shortlink is created and initially saved, but when pressing the share button again after additional conversation in askbot, it doesn't update.
  - Improve it so that when pressing the share button again in the same session, it updates if existing data exists.
  - ui/askbot/share/hlMPU2 - This is the shortlink where the problem occurred. For reference
- Fix the following error in ui/askbot/share
  - Xy6R2y:570 Uncaught SyntaxError: await is only valid in async functions and the top level bodies of modules


