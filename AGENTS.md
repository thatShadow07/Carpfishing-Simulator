# AGENTS.md

## Project context

CarpFishing Simulator is a Unity 6 first-person carp fishing simulator. The goal is an immersive and authentic fishing-session experience while remaining accessible and performant on mid-range PCs.

## Current milestone

Vertical slice deadline: **March 1, 2027**.

Prioritize the complete basic fishing loop before secondary features.

## Engineering rules

- Use C# and Unity 6.
- Prefer small, focused components.
- Keep code readable for a beginner programmer.
- Do not introduce unnecessary frameworks.
- Do not change architecture without documenting the reason.
- Avoid unrelated refactors.
- Test changes before integrating them into larger systems.
- Optimize only when there is a measured or obvious need; do not sacrifice clarity prematurely.

## Scope discipline

The current priority is the vertical slice. Multiplayer, advanced rigs, extra venues, competitions and other long-term features should remain outside the implementation unless explicitly requested.

## Source of truth

Use these files as project references:

- `GAME_DESIGN.md` — game vision and systems
- `ROADMAP.md` — development phases and priorities
- `ARCHITECTURE.md` — technical structure
- `CLAUDE.md` — collaboration and coding rules

## Git

Use focused commits with clear messages. Never commit secrets, credentials or generated build artifacts.
