# Project Skeleton — working context

Unity/C# shift-work evaluation game. Player works timed shifts maintaining a
station's power and oxygen systems (room minigames + manual hold-to-work tasks),
then an indifferent corporate AI scores the shift and quietly raises its standards
every time. Owner: Dor Nudel (ThatNoodleGuy).

Lineage: started as a Unity course capstone co-developed with Asaf, historically
called `Prototype_Test_Janitor`. This repo is the restructured/evolved continuation
of that prototype — not a greenfield project. Credit Asaf on the original when this
goes public (wording to agree with him if the repo or public story is shared).

An earlier direction (agent-based immune-cell simulation, Milestones 0–9, uniform
grid / data-oriented agents / scalar fields) was explored and deliberately dropped:
too close to the author's day job (CellStudio/CellStudioSE) and weak as a
differentiated portfolio piece. **Do not resurrect that plan unless explicitly
asked.** `immune_simulation_plan.pdf` is kept only as historical reference.

## Current state

- Full shift loop is playable: accept shift → work rooms/tasks under a timer → AI
  evaluation → continue. Core classes: `StationManager`, `AIManager`,
  `ShiftMetrics`, `ShiftEvaluationUI`.
- Two companion docs carry the real narrative and implementation detail — read
  them before assuming anything about scope:
  - `PORTFOLIO_CONTEXT.md` — why / portfolio framing / design decisions (setting
    pivot options, credit split with Asaf, what makes this a good portfolio base).
  - `DROP_IN_IMPLEMENTATION_GUIDE.md` — step-by-step (Steps 1–10) implementation
    guide with "where to put it" anchors.
- **Both docs have drifted out of sync with the actual code at least once.**
  Verified directly against source (not trusted from either doc's own status
  table):
  - Steps 1–6.5(A–C) are done, matching their tables.
  - Steps 7 (typewriter evaluation text), 8 (AI time-scoring wired to
    `StationManager.ShiftDurationSeconds`), and 9 (`ITaskActor` /
    `PlayerTaskActor` pluggable input) are **also already fully implemented** —
    the guide's table calls these "not in code" / "partial," but
    `ShiftEvaluationUI.cs`, `AIManager.cs`, and `Assets/_Scripts/Tasks/` all have
    them. Don't redo this work; re-verify against source before trusting either
    doc's checklist again.
- Real open gaps:
  - **Scene wiring**, not code: per the guide's own scene-YAML map, no
    `TaskBehavior`/`CleaningTask` instance is placed in `PrototypeScene 1` yet
    (the system is fully coded but unused in the level), and `ShiftTerminalUI`
    isn't attached to anything in-scene. This needs the Unity Editor to check —
    not verifiable from source alone.
  - Step 10 (this file + `README.md` being accurate) — in progress via this edit.
- Repo hygiene: `Prototype_Test_Janitor.slnx`, a stale duplicate solution file
  next to `Project Skeleton.slnx`, has been deleted from disk (not yet committed
  as of this writing — check `git status`).
- The janitor-prototype commits in this repo's history (`938fa8c` through
  `7f9bb70`) are ahead of whatever's on the separate `Prototype_Test_Janitor`
  GitHub repo. Porting that work back there is a real task, but not urgent —
  nothing is lost by leaving it only here for now.

## Next task

Work through `DROP_IN_IMPLEMENTATION_GUIDE.md`'s actually-open items, in order:

1. Open the project in Unity and confirm/fix scene wiring: place a
   `TaskBehavior`/`CleaningTask` instance in `PrototypeScene 1` with a trigger
   collider, and attach `ShiftTerminalUI` to the Accept-shift UI (watch for the
   double-Accept-button risk the guide calls out in Step 6).
2. Decide the setting pivot (space station vs. underwater/underground facility —
   see `PORTFOLIO_CONTEXT.md`) before adding more content that would need
   reskinning later.
3. Build the "pluggable AI agent" portfolio hook on top of the existing
   `ITaskActor` interface — one non-player agent implementation, runtime-swappable
   with `PlayerTaskActor`.

## Conventions

- Unity + C#. Follow the existing style in `Assets/_Scripts/`: no namespaces,
  singleton access via `Singleton<T>` / `BaseSingleton`, `StationManager.Instance`
  as the central hub other systems query.
- Small commits, one concern at a time.
- Don't trust either markdown guide's status/checklist claims without checking the
  actual code first — both have drifted out of sync with real commits before.

## How to work with me here

- Push back on design decisions rather than implementing whatever is asked.
- Keep one clear portfolio headline (evaluation game *or* modular AI showcase, not
  both at once) — see `PORTFOLIO_CONTEXT.md`'s caveat on this.
- Prefer a working vertical slice over adding more parallel systems.
