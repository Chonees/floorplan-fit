# Existing knowledge stack compared with LLM Wiki

The project already implements most of Karpathy's LLM Wiki pattern:

- `obsidian-vault/` is the human-readable Markdown knowledge base.
- `Current State.md`, `Decisions/`, `Bugs/`, `Experiments/`, and `Implementation/` provide a maintained schema.
- `AGENTS.md` tells agents how to route and update knowledge.
- Engram provides a separate persistent memory/index layer.

The main missing pieces are an explicit immutable `raw/` source layer, systematic source-to-claim provenance, and automatic cross-link/contradiction maintenance across the whole vault. Therefore this is already an agent-maintained project wiki, but not yet a complete Karpathy-style source-compilation pipeline.
