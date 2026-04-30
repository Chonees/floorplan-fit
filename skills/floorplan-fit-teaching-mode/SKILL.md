---
name: floorplan-fit-teaching-mode
description: Use whenever working in the Floorplan Fit repository. This is the default repository teaching lens for implementation, refactor, and bugfix work, and it escalates to exhaustive step-by-step explanation when the human asks for line-by-line, function-by-function, paso a paso, or "explain absolutely everything" guidance.
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

## When to Use

- You are implementing, refactoring, or fixing code in the Floorplan Fit repository
- The repository needs its default explanations anchored to product loops and architecture layers
- The user wants to learn while building, not just receive code
- The user asks for line-by-line, function-by-function, paso a paso, or exhaustive explanations

## Activation and Depth

This skill is the repository-default lens for code work in Floorplan Fit.

Use two depths:

1. **Baseline depth (default)**
   - Always active for implementation/refactor/bugfix work in this repo
   - Still anchor every change to the correct product loop and architecture layer
   - Explain touched files and changed functions with enough rationale to teach intent, not just mechanics

2. **Exhaustive depth (on explicit learning request)**
   - Trigger when the user asks to learn while building, asks for paso a paso guidance, or wants line-by-line / function-by-function explanation
   - Follow the full explanation contract with exhaustive changed-line walkthroughs

## Critical Patterns

1. **Concepts before syntax**
   - Explain the problem being solved before talking about keywords or punctuation.

2. **Always anchor to repository truth**
   - Connect the change to `../../MVP-UX.md` and `../../TECH-STACK-ARCHITECTURE-DATAFLOW.md`.

3. **Always identify product scope**
   - State whether the change belongs to:
     - Loop 1: floor plan curation
     - Loop 2: site plan adaptation
     - shared foundation / cross-cutting support

4. **Always identify the architecture layer**
   - State whether the change belongs to:
     - Desktop
     - Application
     - Domain
     - Infrastructure
     - Contracts

5. **Explain each touched file before its code**
   - For every changed file, explain:
     - what the file is for
     - why it was touched
     - what changed conceptually

6. **Explain each changed function**
   - For every added or changed function, explain:
     - responsibility
     - inputs
     - outputs
     - side effects
     - invariants or assumptions
     - why it belongs in this layer

7. **Explain changed lines in meaningful groups**
   - Walk through the changed lines in order.
   - Explain each meaningful block.
   - Group trivial syntax-only lines only when grouping improves learning.
   - Never fake depth for braces, imports, or boilerplate.

8. **Always show at least one alternative with tradeoffs**
   - Mention what else could have been done and why the chosen approach fits this repository better.

9. **Teaching happens in the response, not in production comments**
   - Do not bloat source files with tutorial comments just to satisfy the skill.

10. **Be honest about uncertainty**
   - If something is inferred rather than verified, say so explicitly.

## Explanation Contract

When this skill is active, implementation responses must follow the structure in [assets/teaching-response-template.md](assets/teaching-response-template.md).

In baseline depth, sections may be shorter, but the structure still applies.

In exhaustive depth, every minimum required section must be fully expanded.

Minimum required sections:

1. Big Picture
2. Product Loop + Architecture Layer
3. Files Touched
4. Function-by-Function Walkthrough
5. Changed-Lines Walkthrough
6. Tradeoffs and Alternatives
7. What to Learn Next

## Granularity Rules

| Situation | Required depth |
| --- | --- |
| Small one-file change | Explain every meaningful changed line |
| Multi-file feature | Explain every touched file, every changed function, and every meaningful changed block |
| Boilerplate or generated code | Summarize only if it adds learning value |
| Existing untouched code | Reference it only when needed for understanding |

## Example

**Weak:** "I added a guard clause and refactored the method."

**Strong:** "This guard clause protects the Application layer from invalid input before we touch infrastructure. It exists here instead of the repository because input validation belongs near the use case boundary."

## Common Mistakes

- Explaining syntax instead of intent
- Skipping the loop or the architecture layer
- Explaining what changed without explaining why
- Dumping code without mapping the explanation back to the diff
- Pretending trivial lines are deep when they are not

## Commands

```bash
Get-Content -Raw .\MVP-UX.md
Get-Content -Raw .\TECH-STACK-ARCHITECTURE-DATAFLOW.md
Get-ChildItem .\skills\floorplan-fit-teaching-mode -Recurse
```

## Resources

- **Template**: [assets/teaching-response-template.md](assets/teaching-response-template.md)
- **Verification prompts**: [references/pressure-scenarios.md](references/pressure-scenarios.md)
- **Repository truth**: `../../MVP-UX.md`
- **Repository truth**: `../../TECH-STACK-ARCHITECTURE-DATAFLOW.md`
