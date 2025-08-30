This project is MCP-Memorizer, which provides knowledge required by LLM Agents.
The /ui/blog endpoint is a page that displays memories in a blog style.
We aim to improve the UX and performance of this page.

Execute the instructions using the following reference information:

# Instructions
- When there is a lot of content, there are too many tag filter buttons. Display only the top 20 with the highest counts, and make horizontal scrolling responsive with touch gestures for mobile consideration.
- Change the button style from clunky square buttons to slightly smaller and rounded styles.
- Once the functionality is implemented, verify all search/filter/paging features and set up efficient search indexes in "008_blog_performance_migration.sql".
- After completing the code, verify that it builds successfully and fix any build errors. Unit tests are not required.

# Reference Information
- Project files are located in "src/Memorizer/"
- Unit test files are in "src/Memorizer.IntegrationTests/"
- "src/Memorizer/Services/Memory.cs" contains the service code for querying and managing memory storage
- Postgres schemas are in "src/Memorizer/migrations" and manage actual schema migrations according to numbering order. Reading them in sequence will provide complete schema understanding.

## Additional Upgrade Instructions
- Fix horizontal scrolling in the content view to prevent it from moving left and right, and instead implement dark mode for markdown code views with proper language-specific styling, using JS libraries if necessary.
- Add copy functionality to code blocks in the code view.
- Display the programming language to the left of the copy button.
- Redirect root URL access to /ui/blog.