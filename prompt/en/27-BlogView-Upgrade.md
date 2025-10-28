This project is MCP-Memorizer, which provides the knowledge that LLM Agents need.
The /ui/blog endpoint is a page that displays memories in a blog-style format.
We want to improve the UX and performance of this page.

Use the following reference information to perform the instructions

# Instructions
- The detail view (ui/view/{id}) allows viewing related documents in the Related Memories section.
- The blog view doesn't have this feature, and we will provide functionality to view related documents.
  - /ui/blog has a button to view a popup called View More.
    - Add a related documents view button to the right of this button.
    - When this button is clicked, it shows a list of related documents in a popup view, using the same popup view frontend technology used in View More.

# Reference Information
- The project files are in "src/Memorizer/".
- Unit test files are in "src/Memorizer.IntegrationTests/".
- "src/Memorizer/Services/Memory.cs" contains the service code for querying and managing memory storage.
- The postgres schema is in "src/Memorizer/migrations", and schema migration is managed according to the numbering. Reading all of them in order will give you the complete schema.


# Additional Instructions
- Confirmed working well, but adjust as follows:
  - There is an inconvenience of having to press Related and then press View once more to access related documents
    - Next to the View More button, if there are no related documents, don't display anything. If there are, create RelateView buttons (maximum 5 buttons only), displaying up to 10 characters of the document title as the button's subtitle
- Maintain the width of the RelateView button at 10 characters (don't truncate), and add an animation effect that continuously scrolls to the left so the full title can be seen
