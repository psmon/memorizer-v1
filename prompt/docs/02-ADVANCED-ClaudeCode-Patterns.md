Advanced Claude Code Patterns That Move the Needle
I've spent 2000+ hours building with LLMs this year. These are the patterns that really work. I GUARANTEE that you have not heard of at least one of these tips. Enjoy!

Motivation

Contrary to popular belief, LLM assisted coding is an unbelievably difficult skill to master due to combining software engineering fundamentals with all of the agentic harnesses and context engineering nuances. It can be incredibly overwhelming. I made my channel with a premonition that learning agentic coding as a discipline to master, not just as a tool to use was a mental reframe that is severely lacking in the software engineering and founder communities. I think Andrej Kaparthy’s tweet on this echoes the sentiment that those in the loop have been feeling lately.

“I've never felt this much behind as a programmer. The profession is being dramatically refactored as the bits contributed by the programmer are increasingly sparse and between. I have a sense that I could be 10X more powerful if I just properly string together what has become available over the last ~year and a failure to claim the boost feels decidedly like skill issue. There's a new programmable layer of abstraction to master (in addition to the usual layers below) involving agents, subagents, their prompts, contexts, memory, modes, permissions, tools, plugins, skills, hooks, MCP, LSP, slash commands, workflows, IDE integrations, and a need to build an all-encompassing mental model for strengths and pitfalls of fundamentally stochastic, fallible, unintelligible and changing entities suddenly intermingled with what used to be good old fashioned engineering. Clearly some powerful alien tool was handed around except it comes with no manual and everyone has to figure out how to hold it and operate it, while the resulting magnitude 9 earthquake is rocking the profession. Roll up your sleeves to not fall behind.” - Andrej Kaparthy (one of the GOATs of AI research)

The Core Philosophy
Before we begin, I would like you to fully embrace the idea that any issue in LLM generated code is solely due to YOU, the user. With LLMs in the advanced state that they are now, errors that come from AI generated code are very frequently traceable to a user error, either due to improper prompt engineering (not clearly and succinctly expressing what you wanted the LLM to build), or improper context engineering (not adequately controlling what information does and does not go into the model’s context window. Context rot is a real thing and model performance significantly decreases as more context is shoved into the window.)

Pattern 1: The Error Logging System
Since we're treating Agentic Coding as a skill to learn, we have an issue. Learning is done best by tightening and clarifying the loop between what you input to a system and what you get out. The tighter this loop, the faster you improve. The more convoluted it gets, the harder the skill is to learn. Agentic coding has one of the most convoluted input-output loops of any skill I've encountered. First, the output is qualitative. In basketball, either the shot goes in or it doesn't. In agentic coding, it's "do I like this output," "is there a bug," "did the model hallucinate." Not binary. Not clean. Second, the middle is a black box. You put in a prompt, stuff happens, output appears. You can't see the intermediate states. You can't trace causality. Third, non-determinism adds noise. You can add role assignment to your prompts as a new tool in your toolkit, and it may have the exact same behavior as if you didn't. There's no verifiable difference unless you're running split tests at scale. People have tried solving this with benchmarks. SWE-bench and the like. Except those don't properly reflect how models perform in the field - they measure synthetic tasks, not your actual workflow with your actual prompts. So here's my solution: Log errors in time. When something goes wrong - hallucination, bug, anti-pattern - I capture exactly what my input was (prompt, context, harness) and exactly what the output was. I trace the triggering prompt verbatim. I categorize the failure. I ask: what did I do wrong? Over time, patterns emerge. This reconstructs the input-output loop that agentic coding normally hides from you. It turns the black box into something you can actually learn from. It puts the power back in your hands instead of just reading tips and tricks online about prompt engineering and hoping they work.

What Triggers an Error Log
Any time:
Claude hallucinates something that doesn't exist
Claude does something I didn't like
Claude builds something I didn't ask for
An anti-pattern occurs
A bug appears in something Claude built
An instruction gets ignored or misinterpreted
Context gets lost
Claude gets stuck in a loop
Basically: anything that could be attributed to a misuse of context, prompting, or harnesses.
The Workflow
Something goes wrong (hallucination, wrong build, ignored instruction, anti-pattern)
Invoke /log_error - this forks the conversation
Claude interviews me about what happened with specific questions
It captures:
The exact triggering prompt (verbatim - this is critical)
Failure category (hallucination type, instruction ignored, context lost, etc.)
Root cause analysis (surface cause AND deeper cause)
Prevention strategy
What was added to CLAUDE.md (if anything)
Whether this pattern has been seen before
Impact (time wasted, quality impact, downstream effects)
Gets logged to a queryable database
Double-escape to rewind the main conversation and continue working
The Interview Questions
The /log_error command has Claude ask me 5-8 clarifying questions that are SPECIFIC to what actually happened. Not generic. Examples:
"I suggested using localStorage for the token. What made you catch that as a security issue?"
"The loop I wrote ran 47 times before you stopped it. What should the exit condition have been?"
"I missed that edge case with empty arrays. Was this something in the requirements I should have inferred?"
The log captures:
What my prompt was - the exact words that led to failure
What went wrong - specific, not generic
How to prevent it - actionable change
Over time, Claude (or you) can analyze the logs and detect common failure patterns. This allows you to improve at agentic engineering by enabling you to try out tips and see what went wrong, learn by building, and establish strong feedback loops.
I Also Log Successes
When something works unusually well, I typically log this too! This works especially well when trying out tips from online and noting if they work really well so you can be reminded to integrate them into daily workflows.

Pattern 2: /Commands as Lightweight Local Apps

Slash commands are secretly one of the most powerful parts of Claude code. Most people think of /commands as saved prompts. I think of them as a method of utilizing Claude as a Service, which I define as building workflows with the power and complexity of a SaaS, but much quicker to build, much more dynamic and flexible, and utilizes the intelligence and filebase/knowledge base understanding of claude code.
Why /Commands Over Skills (Sometimes)
Skills cannot be invoked deterministically. You can tell Claude 'use the X skill' and it might, or it might not. I have had many times where I tell Claude to invoke a skill in my prompt only to have it completely ignored. On the other hand, /commands can be invoked deterministically.
Since skills can't be launched deterministically but contain valuable knowledge, I use this pattern:
Skill contains the knowledge/instructions
/Command is the deterministic trigger
/Command tells Claude to use the skill
How I Think About Them
/Commands are:
Easier to build than full localhosted applications
More dynamic than applications (they take arguments, can be altered mid-workflow)
Capable of launching and orchestrating the work of parallel sub-agents.
Has access to your files, repos, browsers, github, and pretty much anything that you do.
The deterministic launcher for any workflow you want (including Claude skill usage)
Think of /commands as CLI tools you're building for yourself. They take arguments, they have specific behaviors, they're testable.

How I Use Them

In order to crystallize why this is important and how to use these, here is an example of one of my most complex commands:
/presentation-to-shorts command: It takes a recorded presentation video and its Remotion codebase, then outputs four polished short-form clips with synchronized graphics, background music, and captions.

The architecture:
- Phase 1: Opus refactors the presentation into reusable components WHILE WhisperX transcribes. Parallel because neither depends on the other.
- Phase 2: Opus picks the 4 best 20-60 second moments from the transcript.
- Phase 3: Four Opus agents build vertical compositions simultaneously.
- Error Gate: Playwright MCP validates all clips load. Fail → retry.
- Phase 4: Four Sonnet agents sync animation timing to word-level timestamps.
- Phase 5: Sequential GPU rendering, then captions via Watchdog.

The patterns that make it work:
- Model routing: Opus for creative decisions, Sonnet for focused tasks, GPU for compute.
- Parallel where independent: Phases 1, 3, and 4 spawn multiple agents in one message.
- Sequential where dependent: Rendering runs one at a time (GPU memory is finite).
- Deterministic scripts for deterministic work: Transcription, video cutting, audio mixing are Python/FFmpeg—not LLM calls. The model orchestrates when to run them.

Why not build a localhost app for this? The command already knows my file system. It makes 15+ LLM calls across different models. It adapts at runtime based on content. When I want to change something, I edit a markdown file or tell the model mid-process. For turning my own presentations into content, this is exactly the tool I want.

Check out my video on this for the six levels of slash command usage: The Six Levels of Claude Code Slash Commands (Most are stuck at Level 1)

Pattern 3: Hooks for Deterministic Safety
Hooks are code that runs before/after Claude takes actions. I use them as guardrails.
The Setup
dangerously-skip-permissions + hooks that prevent dangerous actions = flow state without fear (use at your own risk!!)
"Dangerously-skip-permissions + Claude hooks which prevent Claude from doing things you don't like is a beautiful life hack for not having to sit there and press continue (use at your own risk)"
Example: If Claude is about to run something like rm -rf, a hook intercepts it deterministically. No more sitting in the loop on experimental applications waiting to accept a cd statement from Claude.
Why This Matters
A core belief that I have about working with LLMs while coding is that knowing when to force guardrails and determinicity into your workflows is one of the top skills in building proper agentic harnesses.

Pattern 4: Context Hygiene
Context rot is real. Every irrelevant token degrades performance. Coding models typically get injected thousands of tokens immediately before you even start prompting due to CLAUDE.md bloat, unruly MCP usage, unused skills, repo reading, and more. Some models begin to degrade performance by up to 50%+ at just 50k tokens in context, which means that our coding agents frequently are not performing at the level that they could before our prompt even starts.
CLAUDE.md Discipline
Keep it hyper lean. I prefer to get context into the model just-in-time rather than front-loading everything into CLAUDE.md. My global CLAUDE.md is nearly at defaults. Project-level ones are minimal. Every token in the context must earn its place.
Recently, I was working with Claude and I was reading the CLAUDE.md, And I realized that Claude had added thousands of tokens of repo-specific instructions into my global CLAUDE.md without me even noticing. This means that every prompt that I gave Claude for at least a couple of weeks was including context about a repo that made no sense to the model and likely deteriorated my model's performance without me even knowing. So here’s an actionable step:
Check your CLAUDE.md file right now with the strict mindset that every token in this file must earn its place and truly deserve to be in there. Be ruthless and at very least move to repo-specific CLAUDE.md files.
Signs your CLAUDE.md is too bloated:
It has content about multiple unrelated projects
There are instructions you don't remember adding
You haven't reviewed it in over a week
It's longer than ~50 lines
Compaction (One of the most important variables of Agentic Coding)
Here’s an actionable step:
Disable autocompact
Add a context status line (e.g [Opus 4.5] 55%) to keep you in the loop
From now on, compaction is done when and how you choose.
Now that the ball is in our court for context management, here are some RAPID FIRE TIPS for context management:
/clear + repo-specific CLAUDE.md is our best method of compaction for very clear break points. Start fresh sessions more often than you think. Context rot is insidious and comes on slowly. https://www.youtube.com/watch?v=IMWpVV-VtnI for more on context rot.
Treat Claude code as an orchestrator and have him launch opus subagents for isolated tasks.
If you have non-critical changes and Opus’ context is full, treat Sonnet[1m] as the break glass in case of emergency to get those done.
/compact manually (at the right times) is still useful for when you just want a quick solution to tone down context and you don’t want to restate to the model what you are up to.
Create a custom slash command /handoff {NOTES} where NOTES are all of the notes that you want the model to know on what you are doing, what to focus on in compacting the conversation, etc. This is a very strong and quick way of managing context once the model really begins to fill up with too much context. This method has the best results but takes the most time and brainpower.
Context trimming via double escape (see below)
The Double-Escape Time Travel
This is the most underutilized feature in Claude Code.
Double-pressing escape lets you jump to any point in the conversation and choose: restore code and conversation or just conversation. Bar none, my favorite thing to do in Claude code is finding the right time to hit Restore Conversation (not Restore Conversation and Code). Treat this as a crucial method for context trimming. Whenever the conversation goes off of the main topic, we can recenter without hurting good code changes. Here’s the most common example:
The Bug Fix Pattern:
Claude builds an app
Claude introduces a bug
You work with Claude to fix it - maybe 5-10 turns of debugging
Bug is fixed, code works
Double-escape → restore only conversation, not code
Now in Claude's mind, the bug never existed
Why this matters: You keep the working code, but you don't dilute context with debugging history that's no longer relevant. Claude continues with a clean mental model.
The Runaway Recovery:
When Claude starts looping or going off the rails:
Double-escape → restore both code and conversation
You're back to a known good state
Try a different approach
Use it aggressively. Most people treat conversation history as sacred. It's not. It's context, and stale context is harmful context.

You may find this video interesting for exploring context engineering more: Why context engineering is the most important skill...

Pattern 5: Subagent Control
Subagents are incredibly powerful tools when utilized right. One thing that I noticed that most people don’t know is that Claude code consistently spawns Sonnet and Haiku subagents, even for knowledge tasks. Also, I’m sure that you’ve noticed that the CLI is severely lacking in the ability to tell what spawned agents are doing in real time. Luckily there is a solution for this! I have a localhosted agent dashboard automatically spawn any time an agent is launched which allows you to monitor all active agents closely and see what they are saying, what their prompts were, and what tools they run. So here are your actionable steps for this section:
Add the following to your global CLAUDE.md file: “Always launch opus subagents unless specified otherwise”
Join my free skool community: https://www.skool.com/the-agentic-lab-6743/about?ref=6be3bb2df7b744df8202baebef624812 and grab my exact code for monitoring your subagents. Enjoy!
My Subagent Philosophy
Keep subagents simple - give them very specific patterns to follow, plus skills to access, in my experience it is better to give them specific isolated work rather than abstract roles.
Parallelize aggressively - if tasks have isolated context, run them in parallel
More Subagents is Better (If done right) - One rule I learned early on is that more subagents >> more tasks per subagent. Break up complicated tasks into isolated parts so that subagents can focus on their roles.
The downside of subagents - If utilizing multiple subagents for a workflow, be careful for any hallucinations, because if one subagent relies on the output of another, a single hallucination anywhere in the chain can poison the whole workflow. Define clear boundaries and clear checks (leaning towards deterministic checks or Agent X checks Agent Y’s work).

Pattern 6: My Lean Tool Stack
By now you know that context is absolutely sacred, and every token of context must fight for its place in my coding agents. As a result, it should come as no surprise that I very rarely use any MCPs outside of the essential Context7 MCP. My actionable step is for you to go download these two and at least try them out in your workflows.

Context7 MCP
Due to the limitations in the training data of large language models always lagging by a couple of months, it is important to have access to up to date and stable documentation, which is where Context7 MCP comes into play. This MCP literally allows our models to look at the documentation of practically any project or framework out there, meaning it can stay up to date and understand the ins and outs of these frameworks. This is absolutely essential for anyone who codes with LLMs.
Dev Browser / Playwright MCP

For those of you who have not explored the idea of browser automation, you absolutely must get and try out one of these. This allows Claude code to easily control your web browser, look for console errors in UIs for debugging, and allows it to take screenshots so it can utilize its multimodality for improved understanding of designs. The potential in these is endless. I highly suggest dev-browser, which is quicker and more context efficient than Playwright MCP.

Pattern 7: Prompt Engineering on Steroids
Prompt engineering is obviously a crucial element to all agentic coding workflows, and I noticed two things very early on in my LLM coding journey:
Due to the speed of agentic coding, in many cases, your bottleneck is your typing speed
Many parts of prompt engineering are pretty easy and automatable (XML tags, prompt structuring, role assignment).
So obviously I created the following system:
The Reprompter System
This is how I generate high-quality prompts fast:
Press a keybind
Dictate what I want (speaking, not typing)
The system asks me clarifying questions based on my dictation
I answer the questions (still voice)
It generates a thorough prompt with XML tags, role assignment, and utilizes all of the literature on good prompting in order to restructure a messy dictated prompt into something easy for the LLM to follow.
This means I can prompt at high quality, quickly, without the friction of typing out XML structures and remembering prompt engineering best practices every time. Once again, find it for free here: https://www.skool.com/the-agentic-lab-6743/about?ref=6be3bb2df7b744df8202baebef624812

Ask the Model to Ask You Questions
If you don’t want to commit to a full on reprompter, at the very least as an actionable step:
Have the models you are working with interview you way way more than you are now. Many times, even the couple of questions they ask in plan mode are insufficient for truly extracting exactly what you want.


Quick Reference
Situation
Action
Claude does something wrong
/log_error → fork → interview → capture verbatim prompt → rewind
Something worked unusually well
/log_success → capture what made it click
Need reliable workflow execution
/command (deterministic) wrapping skill (knowledge)
Want a complex workflow fit to your file system
/command with parallel subagents + sequential dependencies
Claude asks for too much permission
Hooks + dangerously-skip-permissions
Context filling up too fast
Disable autocompact, add status line [Opus 4.5] 55%, manual compact
Bug is fixed but context is polluted
Double-escape → restore conversation only (keep code)
Claude is looping/runaway
Double-escape → restore both code and conversation
CLAUDE.md feels bloated
Weekly review, repo-specific files, ruthlessly trim, check for Claude additions
Need clean handoff between sessions
/handoff {NOTES} custom command
Clear breakpoint reached
/clear + repo-specific CLAUDE.md
Subagents using wrong model
Add "Always launch opus subagents" to global CLAUDE.md
Can't see what subagents are doing
Agent monitoring dashboard (localhost)
Hallucination poisoning subagent chain
Isolated tasks, deterministic checks, Agent X validates Agent Y
Typing prompts is slow
Reprompter: voice → clarifying questions → structured prompt


If you got value from this in any manner at all or learned something new, I promise that the value you get out of joining my free skool community and subscribing to my youtube will be much more.
Youtube: https://www.youtube.com/channel/UCD-gasIQYzXqQ4dr7mGPRfw
FREE skool community (master agentic coding): https://www.skool.com/the-agentic-lab-6743/about?ref=6be3bb2df7b744df8202baebef624812
Regardless, thank you so much for taking the time to read this.

Appendix: The Log Error Command (Full)
Log Error
You are helping the user log an error/failure that just occurred during agentic coding. The PRIMARY goal is to identify what the USER did wrong in their prompting, context management, or harness configuration. This is about building USER skill, not cataloging model failures.
Core Philosophy
Errors in agentic coding are almost always traceable to:
Bad Prompt - Ambiguous, missing constraints, too verbose, wrong structure
Context Rot - Didn't /clear, conversation too long, stale context polluting responses
Bad Harnessing - Wrong agent type, didn't pass context to subagents, missing guardrails
The model is the constant. The user's input is the variable. Focus on the variable.
Logs Directory
All logs are stored in: /home/romij/claude_accessible/agentic_practice_logs/
Errors: /errors/error-XXX.md
Metadata (for ID tracking): /metadata.json
Your Task
Review the conversation to identify what went wrong.
Ask 5-8 pointed questions focused on USER behavior:
"Your prompt was 4000 words. What were the 3 most important requirements?"
"Did you specify what NOT to do, or only what to do?"
"When did you last /clear? How full was context?"
"Did you verify the subagents received the critical context?"
"Was this reference material or explicit requirements?"
"What constraints were in your head but not in the prompt?"
Trace the triggering prompt - Get the EXACT prompt that led to failure.
Be critical of the user - They asked for this. Don't soften it.
Log the error with the template below.
Log Template
markdown
# Error #[ID]: [Short Descriptive Name]
**Date:** [Date]
**Project/Context:** [What were you working on]

## What Happened
[2-3 sentences - what went wrong specifically]

## User Error Category
**Primary cause:** [Pick ONE]

### Prompt Errors
- [ ] **Ambiguous instruction** - Could be interpreted multiple ways
- [ ] **Missing constraints** - Didn't specify what NOT to do
- [ ] **Too verbose** - Buried key requirements in walls of text
- [ ] **Reference vs requirements** - Gave reference material, expected extracted requirements
- [ ] **Implicit expectations** - Had requirements in head, not in prompt
- [ ] **No success criteria** - Didn't define what "done" looks like
- [ ] **Wrong abstraction level** - Too high-level or too detailed for the task

### Context Errors
- [ ] **Context rot** - Conversation too long, should have /cleared
- [ ] **Stale context** - Old information polluting new responses
- [ ] **Context overflow** - Too much info degraded performance
- [ ] **Missing context** - Assumed Claude remembered something it didn't
- [ ] **Wrong context** - Irrelevant information drowning signal

### Harness Errors
- [ ] **Subagent context loss** - Critical info didn't reach subagents
- [ ] **Wrong agent type** - Used wrong specialized agent for task
- [ ] **No guardrails** - Didn't constrain agent behavior appropriately
- [ ] **Parallel when sequential needed** - Launched agents that had dependencies
- [ ] **Sequential when parallel possible** - Slow execution due to unnecessary serialization
- [ ] **Missing validation** - No check that agent output was correct
- [ ] **Trusted without verification** - Accepted agent output without review

### Meta Errors
- [ ] **Didn't ask clarifying questions** - Could have caught this earlier
- [ ] **Rushed to implementation** - Skipped planning/verification
- [ ] **Assumed competence** - Expected Claude to infer too much

## The Triggering Prompt
```
[Exact prompt - verbatim]
```

## What Was Wrong With This Prompt
[Be specific and critical. What should have been different?]

## What The User Should Have Said Instead
```
[Rewritten prompt that would have prevented this error]
```

## The Gap
- **What user expected:** [Expected outcome]
- **What user got:** [Actual outcome]
- **Why the gap exists:** [Direct connection to user error above]

## Impact
- **Time wasted:** [X minutes]
- **Rework required:** [What needs to be redone]

## Prevention - User Action Items
1. [Specific action user should take next time]
2. [Another specific action]
3. [Consider adding to personal CLAUDE.md or workflow]

## Pattern Check
- **Seen this before?** [Yes/No - if yes, this is a habit to break]
- **Predictable?** [Should user have anticipated this?]

## One-Line Lesson (for the USER)
[Actionable takeaway about prompting/context/harnessing - NOT about model behavior]

---
*Logged on [timestamp]*
Important
Be CRITICAL of the user - they're logging this to learn, not to feel good
Focus 80% on user error, 20% on model behavior
The goal is to improve USER skill at agentic coding
If the user can't identify their mistake, help them find it
Sanitized logs are useless - be specific and honest



Appendix: The Log Success Command (Full)
# Log Success

You are helping the user log a success/win that occurred during agentic coding. Most people skip this - they only log failures. But understanding WHY things work is just as important as why they fail. Capture what went RIGHT.

## Logs Directory
All logs are stored in: `/home/[user]/claude_accessible/agentic_practice_logs/`
- Successes: `/successes/success-XXX.md`
- Metadata (for ID tracking): `/metadata.json`

## Your Task

1. **First, review the recent conversation context** to understand what went notably well. Look for:
    - What task was accomplished smoothly
    - What approach was used that worked well
    - Any moments where something just clicked
    - Unusually fast completion
    - First-try successes
    - Elegant solutions
    - Minimal intervention needed
    - Good tool/command usage

2. **Ask 4-6 clarifying questions** to extract WHY it worked. Be SPECIFIC to what actually happened. Examples:
    - "That auth flow came together in under 20 minutes. What about the prompt setup made it work so smoothly?"
    - "You didn't have to correct me once during the refactor. Was that luck or did the context in CLAUDE.md help?"
    - "The solution I suggested was cleaner than what you initially had in mind. What made it click?"

   Questions should cover:
    - **What specifically went well:** Not generic "it worked" but precise win
    - **Why it worked:** The contributing factors
    - **The setup:** What context/prompt/approach was used
    - **Key ingredient:** What was the one thing that made the difference?
    - **Reproducibility:** Could you do this again? Should this become standard practice?

3. **Trace the triggering prompt**: After the interview, identify and quote the EXACT user prompt(s) that led to this success. This is critical for understanding what instruction produced the win. Ask the user to confirm or paste the exact prompt if you can't find it in context.

4. **After getting answers**, read metadata.json to get the next success ID, then create the log file.

5. **Update metadata.json** with the incremented counter.

