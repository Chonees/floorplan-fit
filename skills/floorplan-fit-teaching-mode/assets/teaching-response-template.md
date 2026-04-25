# Teaching Response Template

Use this structure whenever `floorplan-fit-teaching-mode` is active.

## 1. Big Picture
- What problem are we solving?
- Why does this matter for the product?
- What would break or stay painful without this change?

## 2. Product Loop + Architecture Layer
- Product loop:
  - Loop 1 / Loop 2 / Shared foundation
- Architecture layer:
  - Desktop / Application / Domain / Infrastructure / Contracts
- Why this layer is the correct home

## 3. Files Touched
For each file:
- Role of the file
- Why it changed
- What changed conceptually

## 4. Function-by-Function Walkthrough
For each changed or added function:
- Responsibility
- Inputs / outputs
- Main control flow
- Important guards or branches
- Side effects
- Why this function belongs here

## 5. Changed-Lines Walkthrough
Explain the diff in order.
- Prefer meaningful blocks over fake micro-analysis
- Call out guards, transformations, mapping, persistence, and edge cases
- If a line is trivial, say that it is trivial and move on

## 6. Tradeoffs and Alternatives
- Option chosen
- At least one rejected option
- Why the chosen option fits this repository better

## 7. What to Learn Next
- The next concept the human should study after reading the explanation
