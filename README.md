# Project Skeleton

A first-person shift-work evaluation game: keep a station's power and oxygen
systems running across timed shifts while an indifferent corporate AI scores your
performance and quietly raises its standards every shift.

**Origin:** co-developed with Asaf as a Unity course capstone
(`Prototype_Test_Janitor`); this repo is the restructured, expanded continuation.

## Stack

- Unity (URP) — check `ProjectSettings/ProjectVersion.txt` for the exact editor
  version and match it before opening the project.

## How to play

1. Open `Assets/Scenes/PrototypeScene 1.unity` and press Play.
2. Accept a shift from the home screen.
3. Keep the Power room (fuse-board puzzle) and Oxygen room (tank disposal puzzle)
   stocked, work the workstation, and complete any manual tasks in range
   (hold **E**; hold **Space** at the same time to pause progress without losing
   it).
4. When the shift timer runs out — or you end it manually — read the AI's
   evaluation and continue to the next shift.

## Docs

- [`PORTFOLIO_CONTEXT.md`](PORTFOLIO_CONTEXT.md) — design/portfolio decisions and
  narrative framing.
- [`DROP_IN_IMPLEMENTATION_GUIDE.md`](DROP_IN_IMPLEMENTATION_GUIDE.md) — step-by-step
  implementation guide and status.
- [`CLAUDE.md`](CLAUDE.md) — working context for continuing development.

## Credits

Original prototype co-developed with Asaf. Continued systems work (AI evaluation,
manual tasks, resource metrics) by Dor Nudel.
