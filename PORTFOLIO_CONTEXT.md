# Portfolio & design context (Cursor handoff)

This file summarizes decisions and analysis from planning chats so you can **open this folder in Cursor** and continue without re-explaining the project.

**Companion:** `DROP_IN_IMPLEMENTATION_GUIDE.md` — ordered **how-to** (git baseline checks, Steps 1–10) that closes the **Known gaps** and aligns code with the old design docs. Use **this file** for *why / portfolio / narrative*; use **that file** for *what to implement next*, one step at a time.

---

## Project origin & credit

- The game **started as a scratch project** co-developed by **Dor** and **Asaf** (original Space Janitor–era work).
- **This repository** is the **janitor / evaluation prototype** (historically also called **`Prototype_Test_Janitor`** on disk or as a remote name). It is a **restructured / evolved** Unity tree — **not** a greenfield template.
- **Portfolio use:** Continue building from here; document clearly what is **shared origin** vs **your solo expansion** (wording to agree with Asaf if the repo or public-facing story is shared).

---

## Earlier portfolio direction (immune sim → AI focus)

- An **immune / ABM simulation** was considered **too close to professional work** and weak as a *differentiated* public portfolio piece.
- A **modular AI sandbox** (behavior trees, utility AI, FSMs, swappable at runtime, profiling, optional determinism) was suggested as a **broader** engineering story.
- **Refinement:** It does **not** need to be a **mass agent simulation**. **Agents can participate as part of tasks** inside a real game loop (e.g. NPC workers or subsystems completing work while the player is evaluated).

---

## Why this prototype is a good portfolio base

- **Real loop:** Shift → tasks → metrics → AI evaluation → next shift (see `StationManager`, `AIManager`, `ShiftMetrics`, `ShiftEvaluationUI`).
- **Concrete world:** Rooms, stations, player tools, minigames — better than an empty template for **meaningful AI / task context**.
- **Honest narrative:** “Evolution of a co-founded prototype” plus **your** systems and polish reads better than pretending a skeleton is the whole story.

**Caveat:** Keep **one clear headline** for recruiters (evaluation game *or* modular AI showcase); mention the other as depth so the project doesn’t look unfocused.

---

## Setting: underwater / underground vs space (design chats)

**Short answer:** Yes — moving from “spaceship janitor” to an **underwater** or **underground facility** makes **autonomous agents** feel more natural, without changing your core loop.

**Why it helps**

- **Believable automation:** Real facilities assume **pumps, drones, ROVs, scrubbers, routing valves, inspection bots**. Task agents read as *infrastructure* and *policy*, not as a gimmick next to a lone astronaut.
- **Fits the AI manager fantasy:** A faceless **evaluation / operations stack** monitoring **human + machine** throughput matches **industrial maintenance** better than heroic solo space survival.
- **Mundane labor:** The design pillar is **boring work under surveillance**. A pressure tunnel or subsurface plant reinforces **routine** and **protocol**; space can read more “adventure” than “shift work.”
- **Mechanic carry-over** (from redesign notes): e.g. oxygen ↔ **life support / pressure**, power ↔ **pumps & filtration**, breach ↔ **leak / seal failure**, contamination ↔ **biofilm / corrosion** — so existing systems can be **reskinned** rather than re-proven.

**Agents in this frame**

- Agents are **expected coworkers or subsystems**: they clear routes, tend machines, or execute checklists while **your metrics and the AI judge** still apply to *your* choices (operational vs perfect, abandonment, timing).
- Portfolio line: *“Pluggable task agents in a facility sim evaluated by an indifferent ops AI.”*

This is a **narrative and systems alignment** choice, not a technical requirement — space could still work — but **underwater/underground strengthens the story** you, ChatGPT, and Claude sketched.

---

## External design docs (Downloads folder)

These were compared against the codebase (paths on your machine, not in-repo):

- `d:\downloads\files\README.md` — “Space Janitor: Evaluation System Prototype” (shift loop, AI judge, PowerWash-style thresholds).
- `d:\downloads\files\SETUP_GUIDE.md` — Scene setup, manager list, UI canvases, testing checklist.
- `d:\downloads\files\QUICK_REFERENCE.md` — Same pillars, troubleshooting, phased roadmap.

**`ARCHITECTURE.md`** was referenced in QUICK_REFERENCE but was **not** in the attached set; add or link if you create it.

---

## Doc vs implementation (high level)

The markdown describes a **split architecture**. The prototype **implements the ideas** but often **merges or renames** types.

| Design doc | Prototype (`Assets/_Scripts` and related) |
|------------|-------------------------------------------|
| `ShiftManager` | **`StationManager`** — shift start/end, timer, links to UI, workstation, storage, economy. Default shift length here is **600s (10 min)**; docs often use **300s (5 min)**. |
| `TaskTracker` | **No class with that name.** Tracking via **`ShiftMetrics`**, **`RoomController`**, and **`TaskBehavior`** calling `StationManager.Instance.CurrentShift.Record…`. |
| `ResourceManager` | **No class with that name.** **`Storage`**, **`GeneralConsumption`**, **`StationManager`** fields. **`ShiftMetrics.RecordResourcesConsumed`** exists but was **not wired** from consumption code at last check — evaluation may under-use resources until hooked up. |
| `PerformanceEvaluator` | **`AIManager`** — weighted scores, classification, report text, observations, strictness over shifts. Wording differs from doc examples; same role. |
| `TaskBehavior` / `CleaningTask` | **Match the design closely** — operational threshold (~75%), perfected at 100%, hold **E**, metrics hooks. |
| `ShiftTerminalUI` | **Not a separate script.** “Accept shift” flows through **`StationManager`** (e.g. `startBtnUI` / `StartShift()`), not a dedicated terminal canvas as in SETUP_GUIDE. |
| `PerformanceReviewUI` | **`ShiftEvaluationUI`** — classification, report, observations, Continue → **`ContinueToNextShift()`**. Typewriter effect from docs **not** present in code at last check. |
| `SceneSetupHelper` “Create Full Scene” | **`Assets/Editor/PrototypeSetupHelper.cs`** — only **`Tools → Prototype Setup → Create Example Task`**. |

---

## Known gaps vs the written guides (for future work)

- Dedicated **Shift Terminal** / **Performance Review** canvas setup as in SETUP_GUIDE vs current **combined station UI**.
- **TaskTracker-style** “Registered X tasks” discovery — tasks report on interaction instead.
- **SPACE to stop** mid-task (mentioned in QUICK_REFERENCE) — **`TaskBehavior`** uses **E** only unless extended.
- **Idle / behavior metrics** emphasized in README — **`ShiftMetrics`** may need explicit idle tracking if that pillar matters.
- **Resource consumption → shift metrics** for AI scoring — wire if you want doc-parity on “resource use” judgments.

---

## Key scripts (quick navigation)

| Area | Files |
|------|--------|
| Shift lifecycle & economy | `StationManager.cs` |
| AI evaluation | `AIManager.cs` (`ShiftEvaluation` type at bottom of file) |
| Per-shift numbers | `ShiftMetrics.cs` |
| Post-shift UI | `ShiftEvaluationUI.cs` |
| Task base + example | `Tasks/TaskBehavior.cs`, `Tasks/CleaningTask.cs` |
| Rooms / minigames | `RoomController.cs`, `Rooms MiniGames/...` |
| Passive resource drain | `GeneralConsumption.cs` |
| Editor helper | `Assets/Editor/PrototypeSetupHelper.cs` |

---

## Suggested portfolio next steps

1. **Vertical slice:** One shift, 2–3 task types, evaluation clearly reacts to operational vs perfected vs abandoned play.
2. **README in repo:** One-sentence pitch, stack (Unity 6, URP, etc.), **credit to Asaf** on original, your role on this branch.
3. **Optional “your” feature:** e.g. **pluggable AI** for task agents (interface + 2 implementations + runtime swap) with a short **metrics or profiler** note.
4. **Close only gaps that support the headline** (e.g. resource metrics if the AI judge should talk about waste).

---

## Run locally (Unity)

- **Editor version:** `ProjectSettings/ProjectVersion.txt` — match **`m_EditorVersion`** (this clone: **6000.4.5f1** — yours may differ after pull; always read the file).
- **Scenes in build** (`ProjectSettings/EditorBuildSettings.asset`): **verify on your branch.** This clone lists **`Assets/Scenes/PrototypeScene 1.unity`** in the build list; **`SampleScene.unity`** also exists under `Assets/Scenes/`. Use whatever scene your shift/UI setup expects — if you reintroduce a **StartScene → MainLevel** flow, update build settings to match.
- **Controls (from scripts — verify in your scene):** **`TaskBehavior`** uses **hold E** in range; **`CleaningTask`** shows on-screen hints. Player movement / look live under **`Assets/_Scripts/Player/`** — document **your** final bindings in the public README when you ship a slice.

---

## If you make the repo public (checklist)

- **README.md** in project root: pitch, Unity version, **how to open & play**, **credits** (Asaf + you + tools used).
- **`.gitignore`:** Ensure **`Library/`**, **`Temp/`**, **`Logs/`**, **`UserSettings/`** (if personal) are ignored — standard Unity git template; avoids huge useless diffs.
- **License:** If co-owned, agree with Asaf on **license** and who maintains the public fork.
- **Third-party & generated assets:** Note anything under **`Assets/AssetsFiles`** (or similar) that is **not** yours — keeps portfolio conversations honest.
- **Short demo:** A **30–60s capture** (shift → task → evaluation) does more than extra features for recruiters.

---

## Modular AI agents (where to grow, when you add them)

No agent abstraction exists yet; natural **integration points**:

- **`TaskBehavior` / `CleaningTask`:** Progress is driven by player input today — introduce an **`ITaskActor`** (or similar) so **player vs agent** can call the same “apply progress / complete operational” path without duplicating logic.
- **`StationManager` / `ShiftMetrics`:** Already records manual task attempts / operational / perfected — extend with **per-agent** or **delegated-task** metrics if agents complete work on your behalf and the judge should react.
- **`AIManager`:** Stays the **evaluator**; optional future split: **policy data** (ScriptableObjects) vs **scoring code** so copy and strictness curves are easier to tune for the facility theme.

Keep **one demo scenario** (e.g. one agent type + one swap) before scaling variants.

---

## Related references (on your machine, not in-repo)

- Portfolio pivot notes (immune sim → modular AI): `d:\downloads\portfolio_project_plan_full.pdf`
- Evaluation prototype writeups: `d:\downloads\files\README.md`, `SETUP_GUIDE.md`, `QUICK_REFERENCE.md`
- Add **`ARCHITECTURE.md`** here in-repo when you draw **one** diagram or folder diagram — optional but high leverage for interviews.

---

## Opening this project in Cursor

1. **Unity + Cursor root:** open **this repository’s root** (the folder that contains **`Assets/`**, **`Packages/`**, **`ProjectSettings/`**). If you **git cloned the prototype into** `Project-Skeleton`, that path **is** the project — you do **not** need a second nested Unity project unless you intentionally keep one.
2. **Optional:** A subfolder named `Prototype_Test_Janitor/` at repo root is **not** part of a standard Unity layout; if it is empty or leftover, remove it to avoid confusion (Unity should load the project from the parent folder).
3. In chat, use **`@PORTFOLIO_CONTEXT.md`** and **`@DROP_IN_IMPLEMENTATION_GUIDE.md`** (or “continue from portfolio + drop-in”) to restore context.
4. Unity version: **`ProjectSettings/ProjectVersion.txt`** (`m_EditorVersion`).

---

*Last updated from planning conversation — amend this file as the project evolves.*
