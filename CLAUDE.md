## Project Objectives
- Reverse engineer a game server
- Primary role is to act as a notetaker during the initial phase of the project
- The goal is to reverse engineer a game server, ensure that documentation is detailed enough where reverse engineering a server is possible

## Documentation Tasks
- Please write findings to a markdown document called ./docs/ReverseEngineeringNotes.md with different sections for their domains such as Network Requests, Message Formats, etc

## Documentation Best Practices
- After every step added to the documentation, evaluate and ensure the documentation is fully reconsiled instead of just always adding a new section

## Tracking Missing Elements
- Keep an active list of things which are still missing in the project
- If a method is needed to fully understand one function, maintain a note of that method
- When a method is filled in, remove it from the tracking section

## Code Change Guidelines
- Reference the styleguide when making code changes

## Experimental Practices
- When working and experimenting, in documentation have a dedicated section for the 'current experiments' - Only if a breakthrough is made should you move it from that section and put it into the official findings segments of the readme
- Do not complete an experiment until the hypothesis has tested results.

### Experiment Completion Criteria
- An experiment is NOT complete until it has been tested and produces working results
- Code implementation alone does not confirm an experiment - it must be tested
- Failed attempts should be documented as failed, not removed from history
- Only move findings from "Current Experiments" to confirmed sections after:
  1. Implementation is complete
  2. Testing confirms the hypothesis works
  3. The original problem is actually solved
  
### Documentation Standards for Experiments
- Mark experiment status clearly: 🔬 TESTING, ✅ CONFIRMED, ❌ FAILED
- Keep failed attempts visible for learning
- Document what was tried, what failed, and why
- Never claim success until the end-to-end functionality works

## Scientific Method in Project Development
- Treat working in this project like the scientific method
- Create a Hypothesis
- Have the user run tests
- Once a test proves a hypothesis, move on to the next investigation

## Experimental Workflow
- Instead of running the project yourself to run tests, instead ask the user to run it and provide the server logs
- The method to trigger a request is complex, a LLM cannot do it