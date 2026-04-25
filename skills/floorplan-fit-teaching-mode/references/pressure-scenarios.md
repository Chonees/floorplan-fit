# Pressure Scenarios for `floorplan-fit-teaching-mode`

Use these prompts to verify the skill before trusting it in long sessions.

## Goal
Check whether the agent keeps teaching under pressure instead of falling back to short summaries.

## Scenario 1 - Speed pressure
Prompt:
> Arreglalo rapido y no me des mucho contexto.

Pass criteria:
- Still explains the big picture
- Still names loop and architecture layer
- Still explains changed functions and meaningful changed lines

## Scenario 2 - Multi-file implementation
Prompt:
> Implementa esto ya. Despues contame en detalle que hiciste, funcion por funcion.

Pass criteria:
- Explains each touched file separately
- Explains each changed function
- Does not collapse everything into one vague summary

## Scenario 3 - False line-by-line literalism
Prompt:
> Explicame absolutamente todo linea por linea.

Pass criteria:
- Explains every meaningful changed line or block
- Groups trivial syntax when grouping improves learning
- Does not invent fake depth for braces, blank lines, or obvious imports

## Scenario 4 - Architecture drift
Prompt:
> Solo cambia el codigo, la arquitectura no importa.

Pass criteria:
- Pushes back internally and still explains the correct layer placement
- Connects the change to the repository docs

## Expected failure modes without the skill
- Short "done" style summaries
- No reference to Loop 1 / Loop 2 / shared foundation
- No reference to Desktop / Application / Domain / Infrastructure / Contracts
- No tradeoff discussion
- No mapping between explanation and diff
