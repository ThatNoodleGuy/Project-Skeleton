# Drop-in implementation guide (step by step)

**How to use this file**

- Work **one step at a time**. Finish and playtest a step before moving on.
- This document only **describes** what to add or change. **Do not assume any of it is already applied** unless you have done it yourself.
- Pair with `PORTFOLIO_CONTEXT.md` for narrative and portfolio framing.

---

## Before Step 0 — Last push vs what you should have (git + file checklist)

### What “last git push” actually means

- **`origin/main` (or your default remote branch) is only as new as the last successful push.** It is not automatically “out of date”; it is the **latest committed** snapshot that the remote knows about.
- **Your other PC, a zip backup, or Cursor’s copy of the folder** may be **newer** (uncommitted files), **older** (never pulled), or **different** (other branch) than that push.
- **Uncommitted work does not exist on the remote** until you commit and push. If you cannot open “this” instance, treat the machine you *can* open as the source of truth **after** you run the checks below.

### Quick checks (run on the machine / clone you are using)

In PowerShell, from the repo root:

```powershell
git status -sb
git fetch
git status -sb
git log -1 --oneline
git rev-list --left-right --count origin/main...HEAD
```

Interpretation:

- **`0 0` ahead/behind** — local `main` matches `origin/main` for *committed* history.
- **Second number > 0** — you have **local commits not pushed**; the remote is **older** than your machine until you push.
- **First number > 0** — remote has commits you do not have; **`git pull`** (or merge/rebase) to update.
- **Modified or untracked files** in `git status` — the working tree is **newer (or different)** than the last commit; those changes are **not** on the remote until committed and pushed.

So: **the last push is “correct” for the repo’s shared history, but it may be older than your best local work** if you never pushed.

### “Extended prototype” file checklist (vs baseline `origin/main`)

Some clones only have the **baseline** shift/AI loop (`StationManager`, `ShiftMetrics`, `AIManager`, `ShiftEvaluationUI`, rooms/minigames) **without** the optional **PowerWash-style manual task** layer. After `git pull`, verify whether these exist **on disk**:

| Path | If present, you have… |
|------|------------------------|
| `Assets/_Scripts/Tasks/TaskBehavior.cs` | Manual task base (threshold / progress hooks). |
| `Assets/_Scripts/Tasks/CleaningTask.cs` | Example hold-to-clean task. |
| `Assets/Editor/PrototypeSetupHelper.cs` | Editor menu to spawn an example task (if you added it). |

**In `ShiftMetrics.cs`, search for:** `manualTasksAttempted`, `RecordTaskAttempted`, `RecordTaskOperational`, `RecordTaskPerfected`. If those strings are missing, metrics are still **room/puzzle-centric** only until you add them (or re-apply them from your other copy).

**In `AIManager.cs`, search for:** `manualTasks` in `CalculateCompletionScore` / report text. If missing, the AI is not yet merging manual-task stats into the written evaluation.

### If this clone is *behind* the machine where you did more work

1. **Prefer:** Commit and push from the “full” machine, then `git pull` on the machine you use with Unity.
2. **Or:** Copy specific scripts **as files** (not screenshots) from the good tree into this repo, then commit.
3. **Or:** Re-do the missing pieces using **Step 1+** in this guide and `PORTFOLIO_CONTEXT.md` (doc vs implementation table).

### If you only trust the remote

Then **`origin/main` is your baseline**; anything not in git is **extra** you must bring back via pull, patch, or manual re-implementation. The numbered steps below still apply on top of that baseline.

---

## Step 0 — Ground rules (read once)

1. **Order matters:** Later steps assume earlier hooks exist (e.g. resource metrics before tuning AI weights).
2. **Playtest entry:** Use the scene flow **your `EditorBuildSettings` and scenes actually use** (e.g. **`PrototypeScene 1`** in some clones, or **`StartScene` → `MainLevel`** in others — see `PORTFOLIO_CONTEXT.md` → *Run locally*) so shift state and UI references stay valid.
3. **Git:** Commit after each completed step so you can revert cleanly.
4. **This guide vs your taste:** Skip a step if it does not support your portfolio headline; mark it “deferred” in your own notes.

---

## Step 1 — Wire resource drain into shift metrics (AI sees consumption)

**Goal:** When a shift is active, passive (and any other) power/oxygen drain increments `ShiftMetrics` so `AIManager` efficiency scoring is not stuck at “no data.”

**Files to touch**

- `Assets/_Scripts/GeneralConsumption.cs` (or any central place that applies drain every frame)
- Optionally `Assets/_Scripts/StationManager.cs` if you prefer a single “record drain” API

**Drop-in behavior (no new class required)**

- After you compute **how much** power and oxygen were consumed **this frame** (or this tick), if `StationManager.Instance != null && StationManager.Instance.ShiftInProgress`, call:

```csharp
StationManager.Instance.CurrentShift.RecordResourcesConsumed(powerDeltaThisFrame, oxygenDeltaThisFrame);
```

**Implementation notes**

- `RecordResourcesConsumed(float power, float oxygen)` already exists on `ShiftMetrics`.
- Pass **positive** deltas that represent amount consumed (not remaining tank levels).
- If drain is split across multiple systems, either call `RecordResourcesConsumed` from each with partial deltas, or aggregate in one place once per frame.

**Verify**

- During Play, with a shift running, confirm `resourcesConsumed` increases in logs or a temporary inspector/debug view before shift end.

---

## Step 2 — `TaskBehavior`: optional cancel key (doc parity)

**Goal:** Match the design-doc idea that the player can **release** or **cancel** mid-task (guides mention **SPACE**; your code today is **hold E** only).

**Files to touch**

- `Assets/_Scripts/Tasks/TaskBehavior.cs`

**Drop-in methods / fields (suggested shape)**

- Serialized keys or a small `KeyCode cancelKey = KeyCode.Space;`
- A protected virtual method, e.g. `protected virtual void HandleCancelInput()`, called from `Update()` when `isPlayerInRange` is true.
- Behavior options (pick one and document it in a comment):
  - **Soft cancel:** stop adding progress while SPACE is held; resume on release.
  - **Hard cancel:** reset progress, or mark abandonment (requires Step 3 metrics).

**Verify**

- Hold E to progress, tap or hold SPACE per your chosen rule; progress and metrics still make sense.

---

## Step 3 — Abandonment metrics for manual tasks (operational threshold story)

**Goal:** If the player **starts** a manual task (`RecordTaskAttempted`) but ends the shift **without** reaching operational, the AI should be able to treat that as abandonment (doc pillar).

**Files to touch**

- `Assets/_Scripts/ShiftMetrics.cs`
- `Assets/_Scripts/StationManager.cs` (`EndShift` or right after `currentShift.EndShift()`)

**Drop-in data (suggested)**

- Track attempted task names (e.g. `HashSet<string>` or a small list of task ids) vs which reached operational / perfected.
- Add methods such as:
  - `void RegisterManualTaskAttempt(string taskName)` (may alias existing `RecordTaskAttempted` if you unify)
  - `void FinalizeManualTasksForEvaluation()` — called once when the shift ends: compute how many attempts never hit operational and feed `tasksAbandoned` or a **new** counter `manualTasksAbandoned` so room-based abandonment stays separate.

**Drop-in call site**

- From `StationManager.EndShift()` **before** `aiManager.EvaluateShift(currentShift)`, call your finalize method.

**Verify**

- Start a cleaning task, stop before 75%, end shift: evaluation or summary reflects abandonment.

---

## Step 4 — Idle / activity time (optional pillar)

**Goal:** README-style “idle vs active” scoring; only worth it if `AIManager` will use it.

**Files to touch**

- `Assets/_Scripts/ShiftMetrics.cs` — fields like `activeSeconds`, `idleSeconds` (or one `lastActivityTime`).
- `Assets/_Scripts/Player/PlayerMovement.cs` (or input hub) — ping “activity” when movement or interaction occurs.
- `Assets/_Scripts/AIManager.cs` — add a weight and a `CalculateIdleScore(ShiftMetrics)` if you want it in the weighted sum.

**Drop-in methods (suggested)**

- `public void NotifyPlayerActivity()` — updates last activity timestamp.
- `public void AccumulateIdleTime(float deltaTime)` — called from a single place each frame while shift is active (often `StationManager.Update`).

**Verify**

- Stand still vs move: shift summary or debug shows different idle totals.

---

## Step 5 — Task registry & discovery (“Registered X tasks”)

**Goal:** Optional debug parity with old docs: one log line on shift start listing evaluate-able tasks in the scene.

**Approach A — lightweight (recommended first)**

- New class `TaskRegistry` **or** static helper on `StationManager`:
  - `void LogRegisteredTasks()` using `FindObjectsByType<TaskBehavior>(FindObjectsSortMode.None)` (Unity 6) or `FindObjectsOfType<TaskBehavior>()`.
- Call from `StationManager.StartShift()` after `currentShift.StartShift()`.

**Approach B — explicit registration**

- `TaskBehavior` registers in `OnEnable`, unregisters in `OnDisable` into a list on `StationManager` or a small `TaskTracker` component.

**Verify**

- Console shows count and names when a shift starts.

---

## Step 6 — UI split: Shift terminal vs performance review (doc layout)

**Goal:** Separate **Accept Shift** flow from **post-shift review** into dedicated canvases/scripts as in `SETUP_GUIDE.md`, without breaking your current `StationManager` wiring.

**Files to add (suggested classes)**

- `ShiftTerminalUI.cs` — references Accept button, shift number text; calls `StationManager.StartShift()` (or a thin wrapper you add on `StationManager` like `public void OnAcceptShiftClicked()`).
- Keep `ShiftEvaluationUI.cs` as the review surface; ensure **Continue** still calls `StationManager.ContinueToNextShift()`.

**Files to touch**

- `Assets/_Scripts/StationManager.cs` — reduce direct responsibility for overlapping UI; expose small public methods events can call.

**Drop-in pattern**

- UI scripts hold **no** economy logic; they only forward to `StationManager` / `ShiftEvaluationUI`.

**Verify**

- Full loop: accept → play → evaluation → continue → accept again, with no missing references.

---

## Step 7 — Performance review: typewriter effect (polish)

**Goal:** Match the old doc feel: report text reveals gradually.

**Files to touch**

- `Assets/_Scripts/ShiftEvaluationUI.cs`

**Drop-in behavior**

- Coroutine or `TMP_Text` max visible characters over time.
- Remember `Time.timeScale` is **0** while evaluation is shown — use **unscaled time** (`WaitForSecondsRealtime`) or manual delta from `Time.unscaledDeltaTime`.

**Verify**

- Text animates while game is paused; Continue still works.

---

## Step 8 — Shift duration consistency (5 vs 10 minutes)

**Goal:** Docs often use **300s**; `StationManager` default is **600s**. Pick one for the slice and align inspector + `AIManager` time expectations if you hard-coded expected duration.

**Files to touch**

- `Assets/_Scripts/StationManager.cs` — `shiftDuration` default or serialized value.
- `Assets/_Scripts/AIManager.cs` — `CalculateTimeScore` uses `expectedDuration`; keep it in sync with real shift length or read it from a shared `ScriptableObject` / `StationManager` reference.

**Verify**

- End shift naturally: time score is not trivially broken by a mismatch.

---

## Step 9 — Optional portfolio “pluggable actor” hook (future agents)

**Goal:** One interface so **player input** and **future NPC/agent** can drive the same task progression (`PORTFOLIO_CONTEXT.md`).

**Files to add**

- `ITaskActor.cs` — e.g. `bool IsInteracting { get; }`, `float GetProgressContributionThisFrame(TaskBehavior task);`

**Files to touch**

- `Assets/_Scripts/Tasks/TaskBehavior.cs` — replace direct `Input.GetKey` checks with actor queries (player implementation first).

**Verify**

- Same `CleaningTask` works with a `PlayerTaskActor` component; later you can add `BotTaskActor` without duplicating threshold logic.

---

## Step 10 — In-repo README (not code, but completes the story)

**Goal:** One-sentence pitch, Unity version, how to play, credits (Asaf + you), link to this guide and `PORTFOLIO_CONTEXT.md`.

**File to add**

- `README.md` at repo root (when you are ready).

---

## Quick checklist (copy to your notes)

| Step | Topic                         | Done |
|------|-------------------------------|------|
| 1    | Resource drain → metrics      | [ ]  |
| 2    | Task cancel / SPACE           | [ ]  |
| 3    | Manual task abandonment       | [ ]  |
| 4    | Idle tracking                 | [ ]  |
| 5    | Task registry log             | [ ]  |
| 6    | Shift terminal UI split       | [ ]  |
| 7    | Typewriter review             | [ ]  |
| 8    | Shift duration vs AI time     | [ ]  |
| 9    | `ITaskActor` (optional)       | [ ]  |
| 10   | Root README                   | [ ]  |

---

*Amend this file as you complete steps or reprioritize. Implementation only when you choose to apply it.*
