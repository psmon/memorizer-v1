This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also implements an ASK conversational bot feature using LLM. It also provides various AI features through MCP interface.

Please follow the instructions while referring to the project location and description, and local testing methods.

# Instructions
- The recent prompt/kr/37-PRD-Maker.md activity has been implemented, and we want to add the following features.
  - Implement the Bounded Context (BC) definition feature as an additional generation step after step 5 (Refined PRD).
  - Explain how to define Bounded Contexts (BC) according to DDD principles, and implement a feature to derive BC from PRD using LLM-EX.
  - Derive BC by referencing the already generated Event Storming, Example Mapping, and Refined PRD results.
  - Output the BC derivation results in markdown format, including mermaid diagrams.
  - Include a short link sharing feature via the Share Results button.
  - Design prompts according to the goals, referring to the following Bounded Context definition guidelines.
    - Use various criteria such as subdomain decomposition, ubiquitous language conflicts, reasons for change, transaction boundaries, data ownership, organizational boundaries, etc., and include explanations.

### Bounded Context (BC) Definition Guidelines
```
2) Subdomain Decomposition (Strategic DDD): Core / Supporting / Generic
First, divide the business into subdomains and map BCs to each subdomain.
Core: Competitive advantage/differentiation areas → Deep, independent models
Supporting: Business that supports the core → Relatively simple/flexible
Generic: General-purpose (login, notifications, etc.) → Consider packages/external services
👉 Usually Core BCs are divided more finely/clearly, while Generic ones are often integrated/externalized.
3) Setting boundaries by "Ubiquitous Language (terminology)" conflicts
If the same word has different meanings depending on the team/business, it's a signal for BC separation.
Example: **"Settlement"**
Payment team: Payment settlement based on PG
Sales team: Settlement reflecting sales/commission/promotions
Accounting team: Settlement based on accounting vouchers/closing
→ If the names are the same but the models are different, it's safer to put them in translation (context mapping) relationships.
4) Cutting by reasons for change (Why it changes)
For BC, **reasons for change (the axis on which rules change)** is more important than "data".
If discount policies change frequently with many experiments → Candidate for "Price/Promotion" BC separation
If carriers change but order rules are stable → "Shipping integration" as a separate BC/adapter
5) Cutting by transaction/consistency boundaries (strong hint)
If there's a lot that "must be saved/rolled back together at once", they're likely in the same BC.
Group things that require strong consistency (immediate consistency) together
Others are good to separate with domain events + eventual consistency.
Example: Order creation (Order BC) ↔ Payment approval (Payment BC) are often connected by events.
6) Based on data ownership and write model
Determine the owning BC based on "who writes this data?"
Other BCs preferably don't modify directly
For queries, replicate (read model) or
Bring via events/ACL (Anti-Corruption Layer).
7) Use organizational/team boundaries (Conway's Law) as 'reference'
Realistically, if teams are different, releases/priorities/terminology diverge, so it's often natural for BCs to diverge too.
However, "organization is divided = unconditional BC separation" is risky
→ It's better to confirm separation only when **coupling (change/transaction/terminology)** aligns together.
Quick Checklist (BC separation candidate signals)
If 2-3 or more of the following apply, consider it a strong separation candidate.
Same terminology is used with different meanings (language conflict)
The axes on which rules/policies frequently change are different (separation by reason for change)
Deployment cycles/responsible teams are completely different
No need to be bound by strong transactions (events are sufficient)
Many external system integrations, want to isolate with "adapter/ACL"
Data write subjects are separated (ownership)
Three common mistakes
Cutting by DB tables: Tables are implementation results and easily misalign with domain boundaries
Cutting too finely: Only increases event/communication/operational complexity
Forcing shared models: The core of BC is "same language/same model", sharing accumulates conflicts
```

### Additional Improvements
- Partial improvement to step 3 Example Mapping discussion phase
  - When utilizing the Event Storming phase, improve to search memory for related technical/domain knowledge and reflect it in the discussion
  - Use the search feature in src/Memorizer/Services/Memory.cs
    - First select 1 related sentence (within 20 characters) to use for search, and search for up to 3 memories with 0.4 or higher similarity
    - Use LLM-EX to determine if the searched memories are useful for the discussion
    - All progress is included within step 3... when displaying progress status... distinguish between "searching memory" and "generating storming"
    - Use the memory search feature to determine if related knowledge is useful for the discussion
      - If no useful material, proceed the same as existing functionality
      - If useful material exists, include as reference material in the discussion, with discussant name designated as "Memoriz"
- Update PRD Maker description Flow diagram

## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Only resolve build errors.
- After fixing build errors, testing is planned to be done manually with modifications based on feedback.
