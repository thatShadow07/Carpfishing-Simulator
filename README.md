# CarpFishing Simulator

First-person carp fishing simulator focused on realism, immersion and the authentic feeling of going fishing in Portugal.

> **Não queremos apenas um jogo onde pescas carpas. Queremos um jogo onde vais pescar.**

## Goal

Build a playable vertical slice by **March 1, 2027**.

**Prepare → Buy → Travel → Explore → Choose spot → Set up → Build rig → Cast → Fish → Bite → Fight → Land → Capture → Log → Upgrade**

## Tech stack

- Unity 6
- C#
- Blender
- GitHub

## Vertical slice

1. First-person player
2. Small lake
3. Basic rod and reel
4. Casting
5. Rig in water
6. Basic carp AI
7. Bite detection and alarm
8. Fish fight
9. Landing
10. Fish-in-hand/capture moment

## Repository structure

```text
Carpfishing-Simulator/
├── Assets/
│   ├── Art/
│   ├── Audio/
│   ├── Materials/
│   ├── Models/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   ├── Settings/
│   └── UI/
├── Docs/
│   └── FISHING_SYSTEM.md
├── ProjectSettings/
├── Packages/
├── UserSettings/
├── .gitignore
├── AGENTS.md
├── ARCHITECTURE.md
├── CLAUDE.md
├── GAME_DESIGN.md
├── ROADMAP.md
└── README.md
```

## Development rules

- Keep systems modular and beginner-friendly.
- Prefer readable C# over clever abstractions.
- Do not change architecture without documenting why.
- Keep gameplay code independent from presentation where practical.
- Optimize for mid-range PCs.
- Test systems in isolation before integration.
- Use small, focused commits.

## Collaboration

**ChatGPT:** architecture, Unity/C#, implementation, debugging and planning.

**Claude:** code review, refactoring, documentation and multi-file analysis.

**GitHub:** source of truth for the project.
