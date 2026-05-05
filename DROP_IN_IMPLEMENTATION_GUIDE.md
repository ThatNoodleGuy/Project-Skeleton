# Drop-in implementation guide (step by step)

**How to use this file**

- Work **one step at a time**. Finish and playtest a step before moving on.
- This document only **describes** what to add or change. **Do not assume any of it is already applied** unless you have done it yourself.
- Pair with `PORTFOLIO_CONTEXT.md` for narrative and portfolio framing.

**Convention — “Where to put it”**

- Each numbered step uses a **Where to put it** block: **after / before** a named method, comment region, or line so you can find the right place in your copy of the repo without guessing.
- If your file structure differs, search for the **anchor** (e.g. `public void StartShift()` in `StationManager.cs`) and apply the snippet there.

### Implementation status — janitor prototype (verify in Unity after pull)

Use this row to see **what this guide’s reference tree already contains in code** vs what is still **documentation-only** or **Inspector/scene** work. Your branch may differ.

| Step | Code / assets (approximate) |
|------|-----------------------------|
| **1** | **Done** — `GeneralConsumption` calls `RecordShiftResourcesIfActive` → `CurrentShift.RecordResourcesConsumed` when `ShiftInProgress`. |
| **2** | **Done** — `TaskBehavior`, `ResetForNewShift`, `StationManager.ResetAllTaskBehaviorsForNewShift`. |
| **3** | **Done** — `ShiftMetrics` manual counters + `FinalizeManualTasksForEvaluation` in `EndShift`. |
| **4** | **Done** — idle fields, `NotifyPlayerActivity` / `AccumulateIdleTime`, `AIManager` idle weight + score. |
| **5** | **Done** — `LogRegisteredManualTasks()` after `ResetAllTaskBehaviorsForNewShift()` in `StartShift`. |
| **6** | **Partial** — `ShiftTerminalUI.cs` and `StationManager.OnAcceptShiftClicked` exist; scene may still wire **`startBtnUI`** as well — avoid double **Accept**. |
| **6.5** | **A–C done** in reference code; **D** points to **Step 7** (still open). |
| **7** | **Not in code** — `ShiftEvaluationUI` sets full report string at once; no `TypewriteReport` / `WaitForSecondsRealtime` yet. |
| **8** | **Not in code** — `AIManager.CalculateTimeScore` still hard-codes `600f`; `StationManager.ShiftDurationSeconds` exists for a future wire-up. |
| **9** | **Not in code** — no `ITaskActor` / `PlayerTaskActor` in reference tree. |
| **10** | **Optional** — root `README.md` when you ship publicly. |

**Also in reference tree:** `Assets/_Scripts/Tasks/CleaningTask.cs` (minimal subclass of `TaskBehavior`).

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
| `Assets/_Scripts/Tasks/TaskBehavior.cs` | Manual task base — **canonical behavior** is defined in this script (hold interact, optional soft cancel, `ShiftMetrics` hooks). See **Step 2**. |
| `Assets/_Scripts/Tasks/CleaningTask.cs` | Example subclass for VFX/UI — **present** in reference janitor tree (`OnProgressChanged` / `OnTaskPerfected` hooks). |
| `Assets/Editor/PrototypeSetupHelper.cs` | Editor menu to spawn an example task (if you added it). |

**In `ShiftMetrics.cs`, search for:** `manualTasksAttempted`, `RecordTaskAttempted`, `RecordTaskOperational`, `RecordTaskPerfected`, `FinalizeManualTasksForEvaluation`. If those strings are missing, metrics are still **room/puzzle-centric** only until you add them (or re-apply them from your other copy).

**In `AIManager.cs`, search for:** `manualTasks` / `manualTasksAttempted` in `CalculateCompletionScore` / report text. If missing, the AI is not yet merging manual-task stats into the written evaluation.

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

**Where to put it**

- **`GeneralConsumption.cs`**
  - In **`Breath()`**: **after** you apply the oxygen delta to storage (the line that does `OxygenStorage.amount -= …`), compute **`oxygenDeltaThisFrame = Time.deltaTime * breatheDrain`** (or the same value you subtracted) and call **`RecordResourcesConsumed`** with **`0f`** for power and that value for oxygen—or pass **`power, oxygen`** as two positives matching what you subtracted this frame only.
  - In **`UsePower()`**: **after** you apply the power delta, same idea: record **`powerDeltaThisFrame`** (e.g. `Time.deltaTime * powerDrain * lights.Length`) and **`0f`** for oxygen, or combine in **`Update()`** once per frame if you prefer one call.
- **Guard every call:** only if `StationManager.Instance != null && StationManager.Instance.ShiftInProgress`.

**Drop-in behavior (no new class required)**

- After you know **how much** power and oxygen were consumed **this frame**, call:

```csharp
StationManager.Instance.CurrentShift.RecordResourcesConsumed(powerDeltaThisFrame, oxygenDeltaThisFrame);
```

**Implementation notes**

- `RecordResourcesConsumed(float power, float oxygen)` already exists on `ShiftMetrics`.
- Pass **positive** deltas that represent amount consumed (not remaining tank levels).
- If drain is split across multiple systems, either call `RecordResourcesConsumed` from each with partial deltas, or aggregate in one place once per frame.

**Verify**

- During Play, with a shift running, confirm `resourcesConsumed` increases in logs or a temporary inspector/debug view before shift end.

**Reference tree:** Step **1** is implemented via **`RecordShiftResourcesIfActive`** in `GeneralConsumption.cs` (same pattern as **§6.5 A**).

---

## Step 2 — Manual tasks: `TaskBehavior` (reference implementation)

**Goal:** In-scene **hold-to-work** tasks that report **operational** (~75%) and **perfected** (100%) into `ShiftMetrics`, separate from room/puzzle counters. The **source of truth** for behavior is `Assets/_Scripts/Tasks/TaskBehavior.cs` — this step is **scene + tuning**, not a second spec.

**Where to put it (class file)**

- **`Assets/_Scripts/Tasks/TaskBehavior.cs`** — create the **`Tasks`** folder under **`_Scripts`** if needed. If you use a **`CleaningTask`** subclass, place it in the same folder.

**What the class already does**

- **Range:** `OnTriggerEnter` / `OnTriggerExit` with **`Player`** tag; collider should be **Is Trigger** (`[RequireComponent(typeof(Collider))]`).
- **Interaction:** Hold **`interactKey`** (default **E**). While in range and shift active, fills **0 → 1** progress at **`progressPerSecond`**.
- **Thresholds:** At **`operationalThreshold`** (default **0.75**), calls `CurrentShift.RecordTaskOperational(ResolveTaskId())`. At **1**, calls `RecordTaskPerfected` and stops.
- **Attempt:** On first qualifying hold, calls `RecordTaskAttempted` (task id = **`uniqueTaskId`** if set, else `"{name}_{GetInstanceID()}"`).
- **Soft cancel (doc parity):** If **`useSoftCancel`**, holding **`cancelKey`** (default **Space**) **pauses** progress while **E** stays held; release Space to resume. No separate `HandleCancelInput()` — logic is inline in `Update`.
- **Hooks for subclasses:** `CanReceiveInput()`, `OnProgressChanged`, `OnTaskOperational`, `OnTaskPerfected` (e.g. `CleaningTask` for UI/VFX).
- **Leave trigger:** If the task is not complete, **progress resets** and the attempt flag clears (re-entry can log another attempt; `ShiftMetrics` still dedupes by **task id** per shift).

**Scene setup**

1. Add **TaskBehavior** (or subclass) on a GameObject with a **trigger** collider.
2. Ensure the **player** has tag **`Player`** (same as `RoomController`). Add a **kinematic Rigidbody** on the player if triggers never fire with **CharacterController**.
3. **Start a shift** before expecting metrics; `TaskBehavior` only records when `StationManager.Instance.ShiftInProgress`.

**Multi-shift loop (drop-in)**

After **`taskCompleted`**, the task stays finished until reset. **`TaskBehavior`** includes **`ResetForNewShift()`**; **`StationManager.StartShift()`** in this repo calls **`ResetAllTaskBehaviorsForNewShift()`** right after **`currentShift.StartShift()`**. If your clone is older, paste:

**Where to put it**

| Snippet | File | Placement |
|--------|------|-----------|
| **`ResetForNewShift()`** | `Assets/_Scripts/Tasks/TaskBehavior.cs` | **After** the closing `}` of **`OnTriggerExit`** (or **`OnValidate`** `#endif` block at file end). **Before** `#if UNITY_EDITOR` if you use that guard at the bottom. |
| Two method calls | `Assets/_Scripts/StationManager.cs` | Inside **`public void StartShift()`**, **immediately after** `currentShift.StartShift();` and **before** `shiftInProgress = true;` (either order with `shiftInProgress` is fine; repo uses reset **after** `StartShift` on metrics and **before** UI updates). |
| **`ResetAllTaskBehaviorsForNewShift`** + **`LogRegisteredManualTasks`** + **`FindTasksInScene`** | `StationManager.cs` | **After** the closing `}` of **`StartShift()`**, **before** **`public void EndShift()`** (same region as other shift helpers). |

`TaskBehavior.cs` — add to the class (or compare with repo):

```csharp
/// <summary>
/// Call when a new shift starts so tasks can be completed again.
/// </summary>
public virtual void ResetForNewShift()
{
    progress = 0f;
    taskCompleted = false;
    hasLoggedAttemptThisShift = false;
}
```

`StationManager.cs` — inside **`StartShift()`**, after **`currentShift.StartShift();`**:

```csharp
ResetAllTaskBehaviorsForNewShift();
LogRegisteredManualTasks(); // optional; remove if you do not want the Step 5 log
```

`StationManager.cs` — add these members (same file):

```csharp
void ResetAllTaskBehaviorsForNewShift()
{
    TaskBehavior[] tasks = FindTasksInScene();
    for (int i = 0; i < tasks.Length; i++)
    {
        if (tasks[i] != null)
            tasks[i].ResetForNewShift();
    }
}

void LogRegisteredManualTasks()
{
    TaskBehavior[] tasks = FindTasksInScene();
    if (tasks.Length == 0)
    {
        Debug.Log("[StationManager] Registered manual tasks: 0");
        return;
    }
    var names = new System.Collections.Generic.List<string>(tasks.Length);
    for (int i = 0; i < tasks.Length; i++)
    {
        if (tasks[i] != null)
            names.Add(tasks[i].gameObject.name);
    }
    Debug.Log($"[StationManager] Registered manual tasks: {names.Count} — {string.Join(", ", names)}");
}

static TaskBehavior[] FindTasksInScene()
{
#if UNITY_6000_0_OR_NEWER
    return FindObjectsByType<TaskBehavior>(FindObjectsSortMode.None);
#else
    return FindObjectsOfType<TaskBehavior>();
#endif
}
```

**Optional extensions (not in the base script)**

- **Hard cancel:** in **`Update()`**, when **`isPlayerInRange`** and **`Input.GetKeyDown(KeyCode.Escape)`** (or your key), set **`progress = 0f`** (and optionally reset **`hasLoggedAttemptThisShift`** if you want a new “attempt”).
- **`ITaskActor`:** see **Step 9**.

**Verify**

- With shift running: enter trigger, hold **E**, see progress; cross 75% and 100%; hold **Space** with **E** and confirm progress pauses.
- End shift: **manual** lines appear in evaluation if **`AIManager`** / **`ShiftMetrics`** are wired (**Step 3**).
- Start a **second** shift: the same **`TaskBehavior`** can reach 100% again.

---

## Step 3 — Manual task abandonment in `ShiftMetrics` (operational threshold story)

**Goal:** If the player **starts** a manual task (`RecordTaskAttempted`) but ends the shift **without** reaching operational, **`manualTasksAbandoned`** reflects that — separate from **room** `tasksAbandoned`.

**Files:** `Assets/_Scripts/ShiftMetrics.cs` — ensure **`ShiftMetrics.EndShift()`** calls **`FinalizeManualTasksForEvaluation()`** after room abandonment math and **before** any code reads manual counts for the report.

**Where to put it** (`ShiftMetrics.cs`)

| Snippet | Placement |
|--------|-----------|
| Manual **fields** + **HashSets** | **After** the last existing **`[Header("Safety & Risk")]`** field block (`timerExpirations`). **Before** `// Internal tracking` (room `HashSet`s) **or** after room-only private fields—keep manual task **public** counters with the other headers. |
| **`StartShift()`** resets | **Inside** **`public void StartShift()`**, **after** you zero **`timerExpirations`** / contamination, **before** **`roomsEntered.Clear()`** (or grouped with other per-shift clears). |
| **`RecordTask*`** + **`Finalize*`** methods | **After** **`RecordMoneyEarned`** (or after other `Record*` methods). **Before** the `// ===== CALCULATIONS =====` region (or before **`GetShiftDuration`**). |
| **`FinalizeManualTasksForEvaluation();`** | **Inside** **`public void EndShift()`**, **after** `tasksAbandoned = roomsEntered.Count - roomsCompleted.Count;` and **after** the **`Debug.Log`** for shift ended if you want logs to show pre-finalize counts—or **immediately before** the method’s closing `}` so it always runs. |

**Drop-in — `ShiftMetrics` fields (with room metrics)**

```csharp
[Header("Manual Tasks (hold-to-work, etc.) — separate from room tasks")]
public int manualTasksAttempted;
public int manualTasksOperational;
public int manualTasksPerfected;
public int manualTasksAbandoned;

private HashSet<string> manualTaskIdsStarted = new HashSet<string>();
private HashSet<string> manualTasksReachedOperationalIds = new HashSet<string>();
private HashSet<string> manualTasksReachedPerfectedIds = new HashSet<string>();
```

**Drop-in — clear in `StartShift()`**

```csharp
manualTasksAttempted = manualTasksOperational = manualTasksPerfected = manualTasksAbandoned = 0;
manualTaskIdsStarted.Clear();
manualTasksReachedOperationalIds.Clear();
manualTasksReachedPerfectedIds.Clear();
```

**Drop-in — methods**

```csharp
public void RecordTaskAttempted(string taskId)
{
    if (string.IsNullOrEmpty(taskId)) taskId = "unknown_manual_task";
    if (manualTaskIdsStarted.Add(taskId))
        manualTasksAttempted = manualTaskIdsStarted.Count;
}

public void RecordTaskOperational(string taskId)
{
    if (string.IsNullOrEmpty(taskId)) taskId = "unknown_manual_task";
    if (manualTasksReachedOperationalIds.Add(taskId))
        manualTasksOperational++;
}

public void RecordTaskPerfected(string taskId)
{
    if (string.IsNullOrEmpty(taskId)) taskId = "unknown_manual_task";
    RecordTaskOperational(taskId);
    if (manualTasksReachedPerfectedIds.Add(taskId))
        manualTasksPerfected++;
}

public void FinalizeManualTasksForEvaluation()
{
    manualTasksAbandoned = 0;
    foreach (string id in manualTaskIdsStarted)
    {
        if (!manualTasksReachedOperationalIds.Contains(id))
            manualTasksAbandoned++;
    }
}
```

**Drop-in — end of `EndShift()`**

```csharp
FinalizeManualTasksForEvaluation();
```

**Do not** duplicate finalize in **`StationManager`** unless **`EndShift()`** is bypassed somewhere custom.

**Verify**

- Start a **`TaskBehavior`**, stop before operational threshold, end shift: summary / AI text reflects **manual** abandonment.

---

## Step 4 — Idle / activity time (optional pillar)

**Goal:** README-style “idle vs active” scoring; only worth it if `AIManager` will use it.

**Where to put it**

| Snippet | File | Placement |
|--------|------|-----------|
| Idle **fields** | `ShiftMetrics.cs` | **After** manual-task headers/fields (**Step 3**). **Before** `// Internal tracking` or **`// ===== CALCULATIONS =====`**. |
| Idle **`StartShift()`** resets | `ShiftMetrics.cs` | **End** of **`StartShift()`**, **before** `Debug.Log("[ShiftMetrics] Shift initialized");`. |
| **`NotifyPlayerActivity`** / **`AccumulateIdleTime`** | `ShiftMetrics.cs` | With other **`Record*`** methods (**before** `// ===== CALCULATIONS =====`). |
| **`AccumulateIdleTime`** call | `StationManager.cs` | **`void Update()`**, inside **`if (shiftInProgress)`** (same block as **`UpdateShiftTimer()`**). **After** `UpdateShiftTimer();` is a stable anchor. |
| Movement **notify** | `PlayerMovement.cs` | **`HandleMovementInput()`**, **after** `horizontal` / `vertical` are read from **`Input.GetAxis`**. **Before** **`inputDirection`** / **`targetVelocity`** math. |
| **`idleWeight`** + **`CalculateIdleScore`** | `AIManager.cs` | New **`SerializeField`** with other weights at top of class. **`CalculateIdleScore`** **after** **`CalculateSafetyScore`** (or near other **`Calculate*Score`** helpers). Blend into **`EvaluateShift`** **after** sub-scores are computed **and** **before** **`DetermineClassification`**. |

**Drop-in — `ShiftMetrics` fields**

```csharp
[Header("Idle / activity (optional)")]
public float idleSeconds;
public float activeSeconds;
public float idleThresholdSeconds = 2f; // no activity this long => idle chunk
private float lastActivityTime;
```

**Drop-in — `ShiftMetrics` methods**

```csharp
public void NotifyPlayerActivity()
{
    lastActivityTime = Time.time;
}

/// <summary>Call once per frame from StationManager.Update while shift active.</summary>
public void AccumulateIdleTime(float deltaTime)
{
    if (Time.time - lastActivityTime > idleThresholdSeconds)
        idleSeconds += deltaTime;
    else
        activeSeconds += deltaTime;
}
```

**Drop-in — `StartShift()` reset**

```csharp
idleSeconds = 0f;
activeSeconds = 0f;
lastActivityTime = Time.time;
```

**Drop-in — `StationManager.Update()`** (inside `if (shiftInProgress)` branch, after timer logic is fine):

```csharp
if (currentShift != null)
    currentShift.AccumulateIdleTime(Time.deltaTime);
```

**Drop-in — `PlayerMovement` (or input hub)** after you detect movement intent:

```csharp
if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
{
    if (StationManager.Instance != null && StationManager.Instance.ShiftInProgress)
        StationManager.Instance.CurrentShift.NotifyPlayerActivity();
}
```

**Drop-in — `AIManager`**: add **`[SerializeField] private float idleWeight = 0.1f;`**, subtract it from the weight sum you use elsewhere or add:

```csharp
float CalculateIdleScore(ShiftMetrics m)
{
    float t = m.idleSeconds + m.activeSeconds;
    if (t <= 0f) return 1f;
    return Mathf.Clamp01(m.activeSeconds / t);
}
```

Then blend **`CalculateIdleScore(metrics)`** into **`evaluation.overallScore`** the same way as other sub-scores.

**Verify**

- Stand still vs move: shift summary or debug shows different idle totals.

---

## Step 5 — Task registry & discovery (“Registered X tasks”)

**Goal:** One log line on shift start listing **`TaskBehavior`** instances.

**Where to put it**

- **`StationManager.StartShift()`**: **after** **`ResetAllTaskBehaviorsForNewShift();`** (or **after** **`currentShift.StartShift();`** if you only want the log). Remove **`LogRegisteredManualTasks();`** to silence the console.
- Helpers live in **`StationManager.cs`** as in **Step 2** (**before** **`EndShift()`**).

**Drop-in:** Use the **`LogRegisteredManualTasks()`** + **`FindTasksInScene()`** block from **Step 2** (already wired in repo **`StationManager`**). To disable logging, remove the **`LogRegisteredManualTasks();`** call from **`StartShift()`**.

**Approach B — explicit registration** (optional alternative)

```csharp
// On TaskBehavior:
void OnEnable()
{
    // StationManager.Instance?.RegisterManualTask(this);
}
void OnDisable()
{
    // StationManager.Instance?.UnregisterManualTask(this);
}
```

**Verify**

- Console: **`[StationManager] Registered manual tasks: N — ...`** when a shift starts.

---

## Step 6 — UI split: Shift terminal vs performance review (doc layout)

**Goal:** Thin UI script for **Accept Shift**; keep **`ShiftEvaluationUI`** for the report.

**Where to put it**

| Item | Placement |
|------|-----------|
| **`ShiftTerminalUI.cs`** | **New file** under **`Assets/_Scripts/`** (or **`Assets/_Scripts/UI/`** if you add that folder). Not inside **`Editor/`**. |
| Component on scene | Add **`ShiftTerminalUI`** to a **Canvas** or station terminal object. Wire **Accept** button → **`OnAcceptShiftClicked`** (or wire button **`OnClick`** to **`StationManager.StartShift`** directly). |
| **`OnAcceptShiftClicked` on `StationManager`** | **Optional.** Add **`public void OnAcceptShiftClicked() => StartShift();`** **after** **`StartShift()`** or **before** **`EndShift()`** in **`StationManager.cs`**, with other public shift API. |
| Stop double-firing | If **`startBtnUI`** already calls **`StartShift`**, either **remove** that hook and use **`ShiftTerminalUI`** only, or **do not** also bind the same button in **`ShiftTerminalUI`**. |

**Drop-in — new file `Assets/_Scripts/ShiftTerminalUI.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShiftTerminalUI : MonoBehaviour
{
    [SerializeField] private Button acceptShiftButton;
    [SerializeField] private TextMeshProUGUI shiftLabel;

    void Start()
    {
        if (acceptShiftButton != null)
            acceptShiftButton.onClick.AddListener(OnAcceptShiftClicked);
    }

    void OnDestroy()
    {
        if (acceptShiftButton != null)
            acceptShiftButton.onClick.RemoveListener(OnAcceptShiftClicked);
    }

    public void OnAcceptShiftClicked()
    {
        if (StationManager.Instance != null)
            StationManager.Instance.StartShift();
    }

    public void SetShiftLabel(string text)
    {
        if (shiftLabel != null)
            shiftLabel.text = text;
    }
}
```

**Drop-in — `StationManager`**: add **`public void OnAcceptShiftClicked() => StartShift();`** if you prefer button **`OnClick`** to call a named handler without **`ShiftTerminalUI`**.

**Verify**

- Accept → play → evaluation → Continue → Accept again.

### Scene YAML map (exact objects in `Assets/Scenes/PrototypeScene 1.unity`)

Use this when wiring in the Editor so you do not guess object names:

| Object in scene YAML | What it already has | What to add / change now |
|------|----------------------|--------------------------|
| `MainRoom` | `StationManager` + `GeneralConsumption` (same GameObject) | Nothing for Step 6. Ensure this object stays active because it owns shift flow and passive drain recording. |
| `Player` | Player tag and movement stack | Keep tag `Player` (required by `TaskBehavior`/`CleaningTask` triggers). |
| `PlayerUI` | Root UI canvas hierarchy | Keep as HUD root; no mandatory new script here. |
| `ShiftEvaluationPanel (Panel)` | `ShiftEvaluationUI` component is already attached and referenced by `StationManager.evaluationUI` | Step 7 work goes on this existing script/component; do not create a second evaluation panel. |
| `HomePanel` | Home screen panel containing `StartBtn` | Recommended host for `ShiftTerminalUI` if you use it. |
| `StartBtn` | Already wired as `StationManager.startBtnUI` and calls `StationManager.StartShift` via Button `OnClick` | If you add `ShiftTerminalUI`, either: (A) keep current `OnClick` and do not bind again from `ShiftTerminalUI`, or (B) move handling to `ShiftTerminalUI` and remove duplicate listener. |

Not present in this scene YAML right now:

- `ShiftTerminalUI` component (script exists in project, but not attached in `PrototypeScene 1` yet).
- Any `TaskBehavior` or `CleaningTask` component instance.

Exact add recommendation for manual task testing (first instance):

1. Under `MainRoom`, create `CleaningTask_01` (3D object or empty + mesh child).
2. Add a `BoxCollider` on `CleaningTask_01`, set `Is Trigger = true`.
3. Add `CleaningTask` component.
4. Tune `TaskBehavior` fields on that component (`progressPerSecond`, `operationalThreshold`, optional `uniqueTaskId`).
5. Play and verify metrics update on shift end.

---

## Step 6.5 — Parity catch-up (resources, task log, `CleaningTask`, typewriter)

**Why this step exists**

Use this section to **patch older clones** (zip / stale branch) in one pass. On the **reference janitor prototype**, **§6.5 A–C are already in the codebase**; treat the subsections below as **verify / copy** instructions. **§6.5 D** and **Step 7** remain **optional polish** (typewriter not implemented in reference `ShiftEvaluationUI`).

**Typical gaps (forks & minimal clones only)**

| Area | Often missing if you are not on the reference tree |
|------|------------------------------------------------------|
| **Step 1** | **`GeneralConsumption`** only changes **`Storage`**; **no** **`RecordResourcesConsumed`** during shifts. |
| **Step 5** | **No** **`LogRegisteredManualTasks()`** in **`StartShift`**. |
| **Optional** | **`CleaningTask.cs`** absent — add from **§6.5 C**. |
| **Step 7** | **No** typewriter on evaluation report text. |

**Reference tree short version:** **1**, **5**, and **`CleaningTask`** are **done** in code; **6** may still need scene cleanup (two Accept paths); **7** and **8** are the next **code** steps if you want parity with old design docs.

---

### 6.5 A — Wire passive drain to `ShiftMetrics` (closes **Step 1**)

**Status (reference tree):** **Implemented** — see `Breath` / `UsePower` / `RecordShiftResourcesIfActive` in `GeneralConsumption.cs`.

**Where to put it** — `Assets/_Scripts/GeneralConsumption.cs`

- Add **`RecordShiftResourcesIfActive`** (private helper) **after** **`UsePower()`** and **before** **`LightsOn()`** (or after **`LightsOff`**).
- Replace the bodies of **`Breath()`** and **`UsePower()`** so each computes the **same** delta it subtracts, then calls the helper.

**Drop-in (complete `GeneralConsumption.cs` reference version)**

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralConsumption : MonoBehaviour
{
    [SerializeField] private bool usePassiveO2;
    [SerializeField] private bool usePassivePower;
    [SerializeField] private GameObject[] lights;
    StationManager stationManager;
    [SerializeField] private float breatheDrain;
    [SerializeField] private float powerDrain;

    private float valueToDrainFast = 100f;
    private float valueToStop = 0;
    private float valueToDrainSlow = 0.05f;

    private void Start()
    {
        lights = GameObject.FindGameObjectsWithTag("RoomLight");
        stationManager = StationManager.Instance;
    }

    private void Update()
    {
        if (usePassiveO2)
        {
            Breath();
        }

        if (usePassivePower)
        {
            UsePower();
        }

        if (StationManager.Instance.PowerStorage.amount <= 0)
        {
            LightsOff();
        }
        else
        {
            LightsOn();
        }

        if (Input.GetKey(KeyCode.Q))
        {
            breatheDrain = valueToDrainFast;
            powerDrain = valueToDrainFast;
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            breatheDrain = valueToStop;
            powerDrain = valueToStop;
        }
        else
        {
            breatheDrain = valueToDrainSlow;
            powerDrain = valueToDrainSlow;
        }
    }

public void Breath()
{
    float oxygenThisFrame = Time.deltaTime * breatheDrain;
    StationManager.Instance.OxygenStorage.amount -= oxygenThisFrame;
    RecordShiftResourcesIfActive(0f, oxygenThisFrame);
}

public void UsePower()
{
    float powerThisFrame = Time.deltaTime * powerDrain * lights.Length;
    StationManager.Instance.PowerStorage.amount -= powerThisFrame;
    RecordShiftResourcesIfActive(powerThisFrame, 0f);
}

void RecordShiftResourcesIfActive(float powerConsumed, float oxygenConsumed)
{
    StationManager sm = StationManager.Instance;
    if (sm != null && sm.ShiftInProgress)
        sm.CurrentShift.RecordResourcesConsumed(powerConsumed, oxygenConsumed);
}

    public void LightsOn()
    {
        foreach (var item in lights)
        {
            item.SetActive(true);
        }
    }

    public void LightsOff()
    {
        foreach (var item in lights)
        {
            item.SetActive(false);
        }
    }
}
```

**Verify** — During an active shift, **`resourcesConsumed`** (or **`GetSummary()`**) increases over time while passive O₂/power drain runs.

---

### 6.5 B — Shift-start task registry log (closes **Step 5**)

**Status (reference tree):** **Implemented** — `StartShift` calls `LogRegisteredManualTasks()` after `ResetAllTaskBehaviorsForNewShift()`; helpers live next to `FindTasksInScene` in `StationManager.cs`.

**Where to put it** — `Assets/_Scripts/StationManager.cs`

- If **`LogRegisteredManualTasks`** is **missing**, copy **`LogRegisteredManualTasks`** + **`FindTasksInScene`** from **Step 2** (same file, **after** **`ResetAllTaskBehaviorsForNewShift`**).
- Inside **`StartShift()`**, **after** **`ResetAllTaskBehaviorsForNewShift();`**, add **`LogRegisteredManualTasks();`** (remove the call if you want a silent console).

**Verify** — Console: **`[StationManager] Registered manual tasks: N — ...`** each time you accept a shift.

---

### 6.5 C — Optional example: `CleaningTask` subclass

**Status (reference tree):** **Implemented** — `Assets/_Scripts/Tasks/CleaningTask.cs` subclasses `TaskBehavior` with hook overrides (extend with VFX/UI).

**Where to put it** — new file **`Assets/_Scripts/Tasks/CleaningTask.cs`** (skip if the file already exists).

**Drop-in** — minimal subclass; extend with particles, audio, world-space UI:

```csharp
using UnityEngine;

public class CleaningTask : TaskBehavior
{
    protected override void OnProgressChanged(float normalizedProgress)
    {
        // Example: drive a material blend, animator float, or TMP fill — keep cheap in Update-driven paths.
    }

    protected override void OnTaskPerfected()
    {
        base.OnTaskPerfected(); // optional if you add a base implementation later
        Debug.Log($"[CleaningTask] Perfected: {name}");
    }
}
```

Add **`CleaningTask`** to a GameObject that already has a trigger collider (or swap **`TaskBehavior`** for **`CleaningTask`** on the same object).

---

### 6.5 D — Typewriter evaluation text → see **Step 7**

**Status (reference tree):** **Not implemented** — evaluation report still appears in full when **`ShowEvaluation`** runs; implement **Step 7** when you want staggered reveal.

**When ready:** Implement **Step 7** below (`TypewriteReport`, **`WaitForSecondsRealtime`**, stop coroutine in **`HideEvaluation`**).

**Verify** — Same as **Step 7**.

---

## Step 7 — Performance review: typewriter effect (polish)

**Status (reference tree):** **Not implemented** — `ShiftEvaluationUI.ShowEvaluation` assigns `reportText.text = evaluation.message` directly.

**Goal:** Report text reveals over time while **`Time.timeScale == 0`**.

**Drop-in (full methods version)** — `Assets/_Scripts/ShiftEvaluationUI.cs`

Replace these methods + add helper fields exactly as shown.

```csharp
// Add these fields with other UI settings fields.
[SerializeField] private float typewriterCharsPerSecond = 48f;
private Coroutine typewriterRoutine;

public void ShowEvaluation(ShiftEvaluation evaluation)
{
    if (evaluationPanel == null)
    {
        Debug.LogError("[ShiftEvaluationUI] Evaluation panel not assigned!");
        return;
    }

    isShowing = true;

    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
    Time.timeScale = 0f;

    if (classificationText != null)
    {
        classificationText.text = evaluation.classification;
        classificationText.color = GetClassificationColor(evaluation.overallScore);
    }

    // Typewriter report text (realtime because timescale is 0 during review)
    if (reportText != null)
    {
        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        typewriterRoutine = StartCoroutine(TypewriteReport(evaluation.message));
    }

    if (observationsText != null)
    {
        if (evaluation.observations != null && evaluation.observations.Count > 0)
        {
            string obsText = "OBSERVATIONS:\n";
            foreach (string obs in evaluation.observations)
                obsText += $"• {obs}\n";
            observationsText.text = obsText;
        }
        else
        {
            observationsText.text = "OBSERVATIONS:\nNone.";
        }
    }

    evaluationPanel.SetActive(true);
    Debug.Log($"[ShiftEvaluationUI] Showing evaluation: {evaluation.classification}");
}

private IEnumerator TypewriteReport(string fullText)
{
    if (reportText == null)
        yield break;

    reportText.text = "";
    float delay = 1f / Mathf.Max(0.01f, typewriterCharsPerSecond);

    int n = fullText != null ? fullText.Length : 0;
    for (int i = 0; i <= n; i++)
    {
        reportText.text = fullText.Substring(0, i);
        yield return new WaitForSecondsRealtime(delay);
    }

    typewriterRoutine = null;
}

public void HideEvaluation()
{
    if (typewriterRoutine != null)
    {
        StopCoroutine(typewriterRoutine);
        typewriterRoutine = null;
    }

    isShowing = false;

    if (evaluationPanel != null)
        evaluationPanel.SetActive(false);

    Time.timeScale = 1f;
    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;

    Debug.Log("[ShiftEvaluationUI] Evaluation hidden");
}
```

**Verify**

- Text animates while paused; **Continue** still works.
- Reopening evaluation in same session does not stack coroutines.

---

## Step 8 — Shift duration consistency (5 vs 10 minutes)

**Status (reference tree):** **Partial** — `StationManager.ShiftDurationSeconds => shiftDuration` exists; **`AIManager.CalculateTimeScore`** still uses a **literal `float expectedDuration = 600f`**. Wire **`stationManager`** (or query singleton) and replace that line per **Drop-in** below so Inspector shift length matches scoring.

**Goal:** **`AIManager.CalculateTimeScore`** uses the same expected length as **`StationManager.shiftDuration`**.

**Drop-in (full methods version)**

`Assets/_Scripts/StationManager.cs` (property should exist; add if missing):

```csharp
public float ShiftDurationSeconds => shiftDuration;
```

`Assets/_Scripts/AIManager.cs`:

```csharp
// Add with other serialized fields (assign in Inspector if you do not auto-resolve).
[SerializeField] private StationManager stationManager;

void Start()
{
    InitializeThresholds();

    if (stationManager == null)
        stationManager = StationManager.Instance;
}

float CalculateTimeScore(ShiftMetrics metrics)
{
    float expectedDuration = stationManager != null ? stationManager.ShiftDurationSeconds : 600f;
    float duration = metrics.GetShiftDuration();

    float deviation = Mathf.Abs(duration - expectedDuration) / Mathf.Max(1f, expectedDuration);
    return Mathf.Clamp01(1.0f - deviation);
}
```

Set `StationManager.shiftDuration` in Inspector (300, 600, or your target).  
This now makes AI time scoring follow the actual configured shift length.

**Verify**

- Natural shift end: time score changes when `shiftDuration` changes.

---

## Step 9 — Optional portfolio “pluggable actor” hook (future agents)

**Goal:** **`TaskBehavior`** asks an **`ITaskActor`** for “interacting” and “progress this frame” instead of **`Input`** only.

**Drop-in (full scripts + full methods)**

`Assets/_Scripts/Tasks/ITaskActor.cs`

```csharp
public interface ITaskActor
{
    bool WantsInteractHold(TaskBehavior task);
    bool WantsCancelHold(TaskBehavior task);
}
```

`Assets/_Scripts/Tasks/PlayerTaskActor.cs`

```csharp
using UnityEngine;

public class PlayerTaskActor : MonoBehaviour, ITaskActor
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode cancelKey = KeyCode.Space;

    public bool WantsInteractHold(TaskBehavior task) => Input.GetKey(interactKey);
    public bool WantsCancelHold(TaskBehavior task) => Input.GetKey(cancelKey);
}
```

`Assets/_Scripts/Tasks/TaskBehavior.cs` (full script version with actor support):

```csharp
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TaskBehavior : MonoBehaviour
{
    [Header("Identity (ShiftMetrics)")]
    [SerializeField] private string uniqueTaskId;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float progressPerSecond = 0.35f;
    [Range(0f, 1f)] [SerializeField] private float operationalThreshold = 0.75f;
    [SerializeField] private KeyCode cancelKey = KeyCode.Space;
    [SerializeField] private bool useSoftCancel = true;

    [Header("Actor (optional)")]
    [SerializeField] private MonoBehaviour taskActorBehaviour;

    [Header("State")]
    [SerializeField] [Range(0f, 1f)] private float progress;
    [SerializeField] private bool taskCompleted;

    private ITaskActor taskActor;
    private Collider col;
    private bool isPlayerInRange;
    private bool hasLoggedAttemptThisShift;

    public float Progress => progress;
    public bool IsComplete => taskCompleted;
    public float OperationalThreshold => operationalThreshold;

    protected virtual void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[TaskBehavior] {name}: collider should be IsTrigger for range detection.");

        taskActor = taskActorBehaviour as ITaskActor;
    }

    protected virtual void Update()
    {
        if (taskCompleted)
            return;
        if (!isPlayerInRange || !CanReceiveInput())
            return;

        bool interactHeld = taskActor != null ? taskActor.WantsInteractHold(this) : Input.GetKey(interactKey);
        bool cancelHeld = useSoftCancel &&
                          (taskActor != null ? taskActor.WantsCancelHold(this) : Input.GetKey(cancelKey));

        if (interactHeld && !cancelHeld)
        {
            TryRecordAttempt();

            float delta = progressPerSecond * Time.deltaTime;
            float previous = progress;
            progress = Mathf.Clamp01(progress + delta);

            if (previous < operationalThreshold && progress >= operationalThreshold)
                OnReachedOperational();

            if (progress >= 1f)
                OnReachedPerfected();
            else if (progress != previous)
                OnProgressChanged(progress);
        }
    }

    protected virtual bool CanReceiveInput() => true;

    private void TryRecordAttempt()
    {
        StationManager sm = StationManager.Instance;
        if (sm == null || !sm.ShiftInProgress)
            return;

        if (!hasLoggedAttemptThisShift)
        {
            sm.CurrentShift.RecordTaskAttempted(ResolveTaskId());
            hasLoggedAttemptThisShift = true;
        }
    }

    private void OnReachedOperational()
    {
        StationManager sm = StationManager.Instance;
        if (sm != null && sm.ShiftInProgress)
            sm.CurrentShift.RecordTaskOperational(ResolveTaskId());

        OnTaskOperational();
    }

    private void OnReachedPerfected()
    {
        progress = 1f;
        taskCompleted = true;

        StationManager sm = StationManager.Instance;
        if (sm != null && sm.ShiftInProgress)
            sm.CurrentShift.RecordTaskPerfected(ResolveTaskId());

        OnTaskPerfected();
        OnProgressChanged(1f);
    }

    protected virtual void OnProgressChanged(float normalizedProgress) { }
    protected virtual void OnTaskOperational() { }
    protected virtual void OnTaskPerfected() { }

    private string ResolveTaskId()
    {
        if (!string.IsNullOrEmpty(uniqueTaskId))
            return uniqueTaskId;
        return $"{gameObject.name}_{GetInstanceID()}";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        isPlayerInRange = false;
        if (!taskCompleted)
        {
            progress = 0f;
            hasLoggedAttemptThisShift = false;
        }
    }

    public virtual void ResetForNewShift()
    {
        progress = 0f;
        taskCompleted = false;
        hasLoggedAttemptThisShift = false;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        operationalThreshold = Mathf.Clamp01(operationalThreshold);
        progress = Mathf.Clamp01(progress);
    }
#endif
}
```

**Optional task-feedback notification script (during task, not only at shift end)**  
`Assets/_Scripts/Tasks/CleaningTask.cs`

```csharp
using UnityEngine;
using TMPro;

public class CleaningTask : TaskBehavior
{
    [Header("Optional UI feedback")]
    [SerializeField] private TextMeshProUGUI taskHintText;
    [SerializeField] private string startedText = "Cleaning started";
    [SerializeField] private string operationalText = "Operational threshold reached";
    [SerializeField] private string perfectedText = "Task perfected";

    private bool announcedStart;

    protected override void OnProgressChanged(float normalizedProgress)
    {
        if (!announcedStart && normalizedProgress > 0f)
        {
            announcedStart = true;
            ShowHint(startedText);
        }
    }

    protected override void OnTaskOperational()
    {
        ShowHint(operationalText);
    }

    protected override void OnTaskPerfected()
    {
        ShowHint(perfectedText);
        Debug.Log($"[CleaningTask] Perfected: {name}");
    }

    public override void ResetForNewShift()
    {
        base.ResetForNewShift();
        announcedStart = false;
    }

    private void ShowHint(string text)
    {
        if (taskHintText != null)
            taskHintText.text = text;
    }
}
```

**Verify**

- Works with direct input (no actor assigned) and with `PlayerTaskActor` when assigned.
- Task notifications appear during progress (optional UI ref).

---

## Step 10 — In-repo README (not code, but completes the story)

**Goal:** One-sentence pitch, Unity version, how to play, credits (Asaf + you), link to this guide and `PORTFOLIO_CONTEXT.md`.

**Where to put it**

- **`README.md`** at the **repository root** (same folder as **`Assets/`**, **`Packages/`**, **`ProjectSettings/`**), **not** inside **`Assets/`**.

**File to add**

- `README.md` at repo root (when you are ready).

---

## Quick checklist (copy to your notes)

| Step | Topic                         | Done |
|------|-------------------------------|------|
| 1    | Resource drain → metrics      | [ ]  |
| 2    | `TaskBehavior` + scene wiring | [ ]  |
| 3    | `ShiftMetrics` manual abandon | [ ]  |
| 4    | Idle tracking                 | [ ]  |
| 5    | Task registry log             | [ ]  |
| 6    | Shift terminal UI split       | [ ]  |
| 6.5  | Parity catch-up (§6.5 A–D)   | [ ]  |
| 7    | Typewriter review             | [ ]  |
| 8    | Shift duration vs AI time     | [ ]  |
| 9    | `ITaskActor` (optional)       | [ ]  |
| 10   | Root README                   | [ ]  |

**Suggested marks for the reference janitor tree (adjust for your branch):** 1–5 **done**; 6 **partial** (code yes, scene may duplicate Accept); 6.5 **A–C done**, **D** open; 7–9 **open**; 10 optional.

---

*Amend this file as you complete steps or reprioritize. Implementation only when you choose to apply it.*
