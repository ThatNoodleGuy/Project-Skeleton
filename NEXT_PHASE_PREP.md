# Next Phase Prep — Days, Work Orders, Minigame Replug, World Feedback

> **Status: prep only.** Nothing under `Assets/` was changed. Every code block
> below is an **uncompiled draft**. I wrote it against the source at commit
> `46c4196` with no Unity Editor available. Expect small compile fixes: a
> missing `using`, an API name, or a serialized field to reassign.
> Confidence tags: **[Certain]** = checked in source or scene YAML;
> **[Probable]** = strong inference; **[Guess]** = a tuning or design call you
> should check by playing.

---

## Contents

0. [Read this first — the unpleasant findings](#0-read-this-first--the-unpleasant-findings)
1. [Verified bug list (file:line)](#1-verified-bug-list-fileline)
2. [Camera decision: keep isometric, stop porting FPS interactions](#2-camera-decision-keep-isometric-stop-porting-fps-interactions)
3. [Milestone plan (order matters)](#3-milestone-plan-order-matters)
4. [M0 — Replug hotfix (one evening)](#4-m0--replug-hotfix-one-evening)
5. [M1 — Interaction foundation for isometric play](#5-m1--interaction-foundation-for-isometric-play)
6. [M2 — The AI hands out the work: ShiftDirector, WorkOrders, Days](#6-m2--the-ai-hands-out-the-work-shiftdirector-workorders-days)
7. [M3 — Minigames v2: power and oxygen as production systems](#7-m3--minigames-v2-power-and-oxygen-as-production-systems)
8. [M4 — World feedback: seeing progress and decay](#8-m4--world-feedback-seeing-progress-and-decay)
9. [M5 — New task catalogue](#9-m5--new-task-catalogue)
10. [M6 — Tests, docs, hygiene](#10-m6--tests-docs-hygiene)
11. [Deferred: the pluggable agent hook](#11-deferred-the-pluggable-agent-hook)
12. [Appendices: keybindings, layers, tuning, scene checklists, commit plan](#12-appendices)

---

## 0. Read this first — the unpleasant findings

1. **The minigames aren't unplugged. The isometric conversion broke them.**
   **[Certain]** `FuseBoard` and `OxygenPuzzle` are still placed and wired in
   `PrototypeScene 1`, with their prefabs, rooms and materials assigned. The
   problem is `PlayerInteraction`. It casts from the **camera** with
   `interactionRange = 3`, but `IsometricCameraRig.offset = (0, 5, -4)`
   puts the camera about **6.4 m** from the player. No ray can reach a tank or
   a switch, so both games are unplayable. "Replug" is a 20-line fix (M0). "Refine"
   is the real work (M1–M3).

2. **The AI's grading has a hidden ceiling that closes by day 23.**
   **[Certain, arithmetic from `AIManager.cs`]** Strictness is applied
   **twice**. The score is divided by `strictness` (line 77) and every
   threshold is multiplied by `strictness` (line 171). The weights also add
   up to 1.1, not 1.0. Here is the best possible grade with *perfect* play:

   | Shift # | Strictness | Best reachable class |
   |---|---|---|
   | 1–2 | 1.00–1.05 | EXEMPLARY OPERATOR |
   | 3 | 1.10 | EFFICIENT OPERATOR |
   | 4–6 | 1.15–1.25 | ADEQUATE ASSET |
   | 7–9 | 1.30–1.40 | ACCEPTABLE PERFORMANCE |
   | 10–14 | 1.45–1.65 | SUBOPTIMAL BEHAVIOR |
   | 15–22 | 1.70–2.05 | INEFFICIENT PROCESS |
   | 23+ | ≥ 2.10 | **UNPRODUCTIVE SHIFT regardless of play** |

   "Quietly raising standards" is the right theme. An invisible ceiling that
   ends in a fixed outcome is not. Players read it as "nothing I do matters"
   and stop trying. Fix in M0.

3. **The minigames aren't paced. They're a side effect of a broken economy.**
   **[Certain]**
   - `WorkStation.Work()` takes `reqAmount * level` = **50/s** from *each* room
     at level 1. That empties a 500-unit tank in about 9 s.
   - Passive power drain is **zero**. `GeneralConsumption` multiplies by
     `lights.Length`, and no `RoomLight`-tagged object exists in scene 1.
   - Passive O₂ drain is hard-coded to 0.05/s by lines 59–63. That overwrites
     the serialized `20` every frame.
   - `WorkStation.UpgradeWorkStation` does `addPoints *= upgradePerc * level`.
     Income goes 3 → 10.5 → 73.5 → 771 $/s by level 4, while upgrade cost
     only grows linearly.
   - Workstation consumption is **never recorded** in `ShiftMetrics`. The
     efficiency ratio is `money / (passive O₂ only)`, so it maxes out after
     about $90.

   The AI "judge" is mostly judging numbers that aren't connected to anything
   the player decides. The M2/M3 redesign exists to fix this.

4. **After the first evaluation, click-to-move dies.** **[Certain]**
   `ShiftEvaluationUI.HideEvaluation()` (line 166) locks and hides the cursor,
   which is FPS-era code. `ShiftEvaluationUI.Start()` (line 50) also locks it,
   racing `PlayerMovement.Start()`, which unlocks it. Whichever `Start` runs
   last wins **[Probable — execution order isn't set]**.

5. **Q throws a tank *and* drains every resource at 100/s.** **[Certain]**
   `PlayerInteraction` uses Q to throw. `GeneralConsumption` uses Q as a debug
   "drain fast" key in shipping code. **Space** is also both *jump*
   (`PlayerMovement`) and *pause task progress* (`TaskBehavior`). Hold Space to
   pause cleaning and you hop in place.

6. **`CLAUDE.md` and `README.md` have drifted again.** **[Certain]**
   - `CLAUDE.md` says no `CleaningTask` is placed and `ShiftTerminalUI` isn't
     attached. Both are now in scene 1: `cleaning_task_01`, and
     `ShiftTerminalUI.acceptShiftButton` is assigned.
   - `README.md` line 3 still says "first-person".
   - `ProjectSettings/ProjectVersion.txt` says `2022.3.62f3`, but the code uses
     Unity 6 APIs (`rb.linearVelocity`) and the manifest pins URP 17.4 (Unity
     6.x). **[Probable]** The rewritten version file was never committed.
     Commit what the editor writes the next time you open the project.

---

## 1. Verified bug list (file:line)

| # | Where | What | Severity | Fixed in |
|---|---|---|---|---|
| B1 | `Player/PlayerInteraction.cs:146,157,208` | Rays limited to 3 m from a camera ~6.4 m away → nothing is ever interactable | **Blocker** | M0 |
| B2 | `AIManager.cs:77` + `:171` | Strictness applied twice → grade ceiling collapses (table above) | **High** | M0 |
| B3 | `AIManager.cs:19-23` | Weights add up to 1.10; the score inflates, then gets clamped | Medium | M0 |
| B4 | `AIManager.cs:16,285` | `toleranceThreshold` is never used in scoring. It is shown as "Efficiency Requirement" and goes **down** as the AI gets stricter (backwards) | Medium | M2 (becomes the probation line) |
| B5 | `AIManager.cs:34,169` | Classification depends on `Dictionary` enumeration order (works in practice, not guaranteed) | Low | M0 |
| B6 | `ShiftEvaluationUI.cs:50,166` | Cursor locked after evaluation → click-to-move broken | **High** | M0 |
| B7 | `GeneralConsumption.cs:49-63` | Debug Q/Z keys overwrite serialized drain every frame; Q collides with throw | High | M0 |
| B8 | `GeneralConsumption.cs:20,79` | Power drain × `lights.Length`, which is 0 in scene 1 → power never drains passively | Medium | M0 |
| B9 | `Rooms MiniGames/Electricity/FuseBoard.cs:101-127` | Start state is 50/50 random, then "errors" re-randomize the target → the error count is meaningless. The board can spawn **already solved**, and it's only checked on toggle → the player must break it to fix it | Medium | M0 (patch), M3 (replace) |
| B10 | `Rooms MiniGames/Electricity/FuseSwitch.cs:176,187` | `renderer.material` in `UpdateVisuals` leaks a material instance on every toggle | Low | M3 |
| B11 | `Rooms MiniGames/Oxygen/OxygenTank.cs:106,187` | `Physics.IgnoreLayerCollision` is **global**. It toggles collision between the two layers for every object, not just this tank and the player | Medium | M1 (carry rewrite) |
| B12 | `WorkStation.cs:37` | `addPoints *= upgradePerc * level` → runaway income (3 → 10.5 → 73.5 → 771 $/s) | High | M3 |
| B13 | `WorkStation.cs:48` | Workstation consumption is never recorded in `ShiftMetrics` → efficiency subscore is always ~1 | High | M3 |
| B14 | `StationManager.cs:209-212` | `AccumulateIdleTime` runs outside shifts too (harmless because `StartShift` resets it, but it's wasted work and confusing) | Low | M2 |
| B15 | `StationManager.cs:651-652,661-662,671-672,680-681` | Upgrade/heal subtract the cost **after** the upgrade and are only correct because `upgradeCost` / `missingHealth` refresh in the *next* `Update`. That's correct by accident and breaks if anyone recomputes eagerly | Low | M3 (econ pass) |
| B16 | `Player/PlayerMovement.cs` (Jump) vs `Tasks/TaskBehavior.cs` (cancel) | Space is used for both | Medium | M1 (remove jump) |
| B17 | `Cameras/CamerasManager.cs:34` vs `PlayerInteraction` vs `TaskBehavior` | E has three owners that don't know about each other | Medium | M1 |
| B18 | Layers (per the `46c4196` commit message) | Layer 6 "Ground" holds the **walls**; layer 7 "Interactable" holds the **floors**. `PlayerInteraction.interactableLayer` = bit 128 = the floors | Medium | M1 (rename layers) |
| B19 | `Tasks/NPCTaskActor.cs:31` | Moves with `transform.position` in a straight line, so it walks through walls and ignores the NavMesh you just baked | Low (deferred) | §11 |

---

## 2. Camera decision: keep isometric, stop porting FPS interactions

**I disagree with dropping the isometric/third-person view**, if that's where
you're leaning. The camera isn't what failed. The *interactions* were written
for a first-person camera and ported by swapping the ray source. Replace the
interaction model and keep the camera.

### Why the fixed-angle camera is the right frame for *this* game **[Probable]**

1. **It *is* the AI.** A fixed, high, slightly-too-far camera reads as CCTV.
   The core fantasy is "an indifferent system watches you work." You can go
   further: during the evaluation, show the last seconds of the shift through
   a scanline/security-feed filter. First-person throws that framing away.
2. **World-state feedback only works if you can see the room.** Grime,
   flickering lights, gauges, the growing number of surveillance cameras (M4):
   all of it is invisible from inside your own eyes.
3. **The agent hook (§11) needs a spectator view.** Watching an NPC worker
   route through the station is the portfolio shot. First-person hides it.
4. **Cost.** First-person done well needs hands or tool viewmodels, a
   crosshair, and diegetic UI. You already paid for the iso conversion
   (NavMesh, click-to-move, wall occluder).

### Where I'd push back harder: over-the-shoulder third person

If "third person" means a chase camera behind the player: **don't**. You get
camera collision, worse wall occlusion, and aim-based interaction that is
just as fragile, with none of the CCTV framing. It's the worst of both.

### What to do instead: a hybrid

- **World navigation and coarse actions:** fixed-angle camera. Interaction is
  **proximity + facing**, from the player's body (not the camera), with
  mouse-hover as a precision override. Click an out-of-reach object and the
  player walks there, then uses it.
- **Precision puzzles (breaker panel, valves, terminal):** a short
  **focus view**. Blend a second camera to an authored anchor in front of
  the panel, freeze movement, click parts directly, Esc/E to leave.
  `CamerasManager` already does a crude version of this for the PC screen.
  M1 generalizes it into `PanelFocusView`.

### If you still want to revert to first person

- Revert `IsometricCameraRig`, `ClickToMove`, and the movement/interaction
  parts of `46c4196`.
- Re-parent `PlayerCamera` under `CameraPivot` and re-enable
  `ToolFollowCamera` + the crosshair.
- Keep `WallOccluder` off.

That's roughly one evening of revert work plus retesting. M2–M4 in this doc
don't depend on the camera. Only M1 does.

---

## 3. Milestone plan (order matters)

| M | Goal | Why this order | Rough size |
|---|---|---|---|
| **M0** | Replug hotfix: both old minigames playable in iso, grading fixed, cursor fixed, debug keys out | Unblocks playtesting everything else | 1 evening |
| **M1** | Interaction foundation: `IInteractable`, `PlayerInteractor`, carry/sockets, focus view, layers, keybinds | Every new task and minigame is built from these primitives | 2–3 evenings |
| **M2** | The AI issues work orders; days advance, carry over and save | Makes tasks *mean* something and gives the AI a visible role | 3–4 evenings |
| **M3** | Minigames v2: breaker panel + oxygen manifold as production systems; economy retune | "More integral and substantial": they drive the income loop | 3–4 evenings |
| **M4** | World feedback: condition, flicker, grime, gauges, surveillance cameras, day board | Makes progress readable without opening a menu | 2 evenings |
| **M5** | New tasks: inspection round first, then others | Content on top of proven primitives | 1 evening each |
| **M6** | Tests for scoring, README/CLAUDE refresh, cleanup | Portfolio signal, stops docs from drifting | 1 evening |

**The rule I'd hold you to:** don't start M3 content until M2 has run three
in-game days in a row without you touching the Inspector. That's your
vertical slice.

---

## 4. M0 — Replug hotfix (one evening)

Smallest changes that make the **existing** games playable and fair again.
No new architecture.

### 4.1 `PlayerInteraction.cs` — measure reach from the player, not the camera

Replace `FindClosestInteractable()` and `FindClosestFuseSwitch()`, and add one
field and one helper.

```csharp
[Header("Mouse")]
[SerializeField] private float mouseRayDistance = 100f;   // camera → world; reach is checked separately from the player's body

bool WithinReach(Vector3 worldPoint)
{
    Vector3 d = worldPoint - transform.position;
    d.y = 0f;
    return d.magnitude <= interactionRange;
}

/// <summary>
/// Mouse hover wins if the hovered tank is within reach of the player's body;
/// otherwise fall back to the nearest tank around the player (keyboard-only play).
/// </summary>
OxygenTank FindClosestInteractable()
{
    Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
    OxygenTank best = null;
    float bestRayDistance = float.MaxValue;

    RaycastHit[] hits = Physics.SphereCastAll(ray, pickupRadius, mouseRayDistance, interactableLayer);
    foreach (RaycastHit h in hits)
    {
        OxygenTank tank = h.collider.GetComponentInParent<OxygenTank>();
        if (tank != null && tank.CanInteract(transform.position) && h.distance < bestRayDistance)
        {
            best = tank;
            bestRayDistance = h.distance;
        }
    }
    if (best != null)
        return best;

    float bestSqr = float.MaxValue;
    Collider[] nearby = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);
    foreach (Collider c in nearby)
    {
        OxygenTank tank = c.GetComponentInParent<OxygenTank>();
        if (tank == null || !tank.CanInteract(transform.position))
            continue;
        float sqr = (tank.transform.position - transform.position).sqrMagnitude;
        if (sqr < bestSqr)
        {
            bestSqr = sqr;
            best = tank;
        }
    }
    return best;
}

FuseSwitch FindClosestFuseSwitch()
{
    Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
    FuseSwitch best = null;
    float bestDistance = float.MaxValue;

    // RaycastAll + reach filter: walls/floors along the ray no longer swallow the click.
    foreach (RaycastHit h in Physics.RaycastAll(ray, mouseRayDistance))
    {
        FuseSwitch sw = h.collider.GetComponent<FuseSwitch>();
        if (sw == null || h.distance >= bestDistance || !WithinReach(sw.transform.position))
            continue;
        best = sw;
        bestDistance = h.distance;
    }
    return best;
}
```

Also change the throw key from `KeyCode.Q` to `KeyCode.G` in `HandleInput`
(until M1 replaces this script). **[Probable]** The fuse switches are
0.15 m boxes 0.3 m apart. From 6 m at 45° they're about 12 px targets, which
is fiddly but workable. That's why M1 moves the panel into a focus view.
**[Guess]** If the board is on a wall that faces away from the camera, you
won't be able to click it at all. Check in the editor.

### 4.2 `GeneralConsumption.cs` — debug keys out of shipping input, drain made real

```csharp
[Header("Debug (Editor / Development builds only)")]
[SerializeField] private KeyCode debugDrainFastKey = KeyCode.F9;
[SerializeField] private KeyCode debugFreezeKey = KeyCode.F10;
[SerializeField] private float debugFastMultiplier = 200f;

private float drainMultiplier = 1f;

private void Update()
{
    StationManager sm = StationManager.Instance;
    if (sm == null || sm.PowerStorage == null || sm.OxygenStorage == null)
        return;

    drainMultiplier = 1f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    if (Input.GetKey(debugDrainFastKey)) drainMultiplier = debugFastMultiplier;
    else if (Input.GetKey(debugFreezeKey)) drainMultiplier = 0f;
#endif

    if (usePassiveO2) Breath();
    if (usePassivePower) UsePower();

    if (sm.PowerStorage.amount <= 0) LightsOff();
    else LightsOn();
}

public void Breath()
{
    float oxygenThisFrame = Time.deltaTime * breatheDrain * drainMultiplier;
    // ... unchanged below
}

public void UsePower()
{
    // Lights are cosmetic multipliers; never let "no tagged lights" mean "no drain".
    float lightFactor = Mathf.Max(1, lights.Length);
    float powerThisFrame = Time.deltaTime * powerDrain * drainMultiplier * lightFactor;
    // ... unchanged below
}
```

> ⚠️ **Retune the scene after this.** `breatheDrain` / `powerDrain` are
> serialized as **20** in scene 1 but were silently overwritten to 0.05. After
> the fix, 20/s drains a 500 tank in 25 s. Set both to **0.3–0.5** for M0
> **[Guess]**. M3 redoes the whole economy anyway.

### 4.3 `ShiftEvaluationUI.cs` — stop locking the cursor

Delete the two lines in `Start()` (`Cursor.visible = false; Cursor.lockState = Locked;`)
and change `HideEvaluation()` to:

```csharp
Time.timeScale = 1f;
Cursor.visible = true;
Cursor.lockState = CursorLockMode.None;   // isometric + click-to-move needs a free cursor
```

### 4.4 `FuseBoard.cs` — generate a real "N errors" board that is never pre-solved

Add `public bool IsOn => isOn;` to `FuseSwitch`. Then replace the body of
`GeneratePuzzle()` from `// Create switch grid` through the `errorCount` loop:

```csharp
int total = gridSize * gridSize;
int errorCount = Mathf.Clamp(level, 1, Mathf.Max(1, total / 2));

// Choose which switches start wrong BEFORE spawning, so the error count is exact.
HashSet<int> wrong = new HashSet<int>();
while (wrong.Count < errorCount)
    wrong.Add(Random.Range(0, total));

int index = 0;
for (int row = 0; row < gridSize; row++)
{
    for (int col = 0; col < gridSize; col++)
    {
        Vector3 localPos = gridOrigin + new Vector3(
            col * switchSpacing.x - (gridSize - 1) * switchSpacing.x / 2f,
            row * switchSpacing.y - (gridSize - 1) * switchSpacing.y / 2f,
            switchForwardOffset);
        Vector3 worldPos = transform.TransformPoint(localPos);

        GameObject switchObj = Instantiate(switchPrefab, worldPos, transform.rotation,
            switchContainer != null ? switchContainer : transform);
        switchObj.name = $"Switch_R{row}_C{col}";

        FuseSwitch fuseSwitch = switchObj.GetComponent<FuseSwitch>();
        if (fuseSwitch != null)
        {
            bool targetOn = Random.value > 0.5f;
            bool startOn = wrong.Contains(index) ? !targetOn : targetOn;
            fuseSwitch.Initialize(this, index, startOn, targetOn);
            fuseSwitch.SetMaterials(correctMaterial, incorrectMaterial);
            switches.Add(fuseSwitch);
        }
        index++;
    }
}
```

### 4.5 `AIManager.cs` — apply strictness once, keep the top tier reachable

Replace `classificationThresholds` / `InitializeThresholds()` /
`DetermineClassification()` and the weighted-sum block:

```csharp
// Ordered best → worst. Last entry is the catch-all and is never adjusted.
private static readonly (string label, float baseThreshold)[] Tiers =
{
    ("EXEMPLARY OPERATOR",     0.95f),
    ("EFFICIENT OPERATOR",     0.85f),
    ("ADEQUATE ASSET",         0.70f),
    ("ACCEPTABLE PERFORMANCE", 0.55f),
    ("SUBOPTIMAL BEHAVIOR",    0.40f),
    ("INEFFICIENT PROCESS",    0.25f),
    ("UNPRODUCTIVE SHIFT",     0.00f),
};

/// <summary>
/// Standards rise by squeezing the gap to 1.0: at strictness s a tier needs
/// 1 - (1 - base)/s. Every tier stays reachable, the climb just gets narrower.
/// </summary>
public float AdjustedThreshold(float baseThreshold) =>
    1f - (1f - baseThreshold) / Mathf.Max(1f, strictnessLevel);

string DetermineClassification(float score)
{
    for (int i = 0; i < Tiers.Length - 1; i++)
    {
        if (score >= AdjustedThreshold(Tiers[i].baseThreshold))
            return Tiers[i].label;
    }
    return Tiers[Tiers.Length - 1].label;
}

// In EvaluateShift, replace the weighted sum + "*= 1/strictness" with:
float weightSum = completionWeight + efficiencyWeight + timeWeight + safetyWeight + idleWeight;
float weighted =
    completionScore * completionWeight +
    efficiencyScore * efficiencyWeight +
    timeScore * timeWeight +
    safetyScore * safetyWeight +
    idleScore * idleWeight;
evaluation.overallScore = Mathf.Clamp01(weighted / Mathf.Max(0.0001f, weightSum));
```

Resulting thresholds **[Certain, arithmetic]**:

| Tier | s = 1.0 | s = 1.5 | s = 2.0 | s = 3.0 (cap) |
|---|---|---|---|---|
| EXEMPLARY | 0.950 | 0.967 | 0.975 | 0.983 |
| EFFICIENT | 0.850 | 0.900 | 0.925 | 0.950 |
| ADEQUATE | 0.700 | 0.800 | 0.850 | 0.900 |
| ACCEPTABLE | 0.550 | 0.700 | 0.775 | 0.850 |
| SUBOPTIMAL | 0.400 | 0.600 | 0.700 | 0.800 |
| INEFFICIENT | 0.250 | 0.500 | 0.625 | 0.750 |

Remove the `InitializeThresholds()` call in `Start()`. `ShiftEvaluationUI`
picks its colour from the raw score, so colour and label will disagree at
high strictness. Pass the tier index in `ShiftEvaluation` if that bothers you.

### 4.6 M0 acceptance

- Day 1: run the workstation until a room drops below `reqAmount`, then solve
  the fuse board **and** the tank disposal in iso view, mouse and keyboard.
- After the evaluation, click-to-move still works.
- Force strictness to 3.0 in the Inspector. A perfect shift still gets
  EXEMPLARY.

---

## 5. M1 — Interaction foundation for isometric play

Three primitives cover every task you'll want to build:

| Primitive | Components | Tasks built from it |
|---|---|---|
| **Use / Hold** | `IInteractable` + existing `TaskBehavior` (hold-to-work) | Cleaning, welding a breach, logging a gauge |
| **Carry** | `CarryableItem` + `ItemSocket` | Canister swap, filter swap, parts delivery |
| **Panel** | `IFocusPanel` + `IFocusClickable` + `PanelFocusView` | Breaker panel, valve balancing, terminal |

That table is also a portfolio talking point: *new tasks are compositions,
not new systems.*

### 5.0 Layers first (rename only — no scene surgery)

Layer masks are serialized as bits, so **renaming** a layer doesn't change
what any mask hits. **[Certain]**

| Index | Now named | Actually holds | Rename to |
|---|---|---|---|
| 6 | Ground | walls | **Wall** |
| 7 | Interactable | floors | **Ground** |
| 8 | — | — | **Usable** (new): solid colliders of things you can use |

Then:

- Put tank/switch/panel/socket colliders on **Usable**.
- `WallOccluder` can use a `LayerMask` instead of `name.Contains("Wall")`.
- `PlayerMovement.groundLayer` / `ClickToMove.groundLayer` stay on bit 7,
  which is now correctly named.

### 5.1 `Interaction/IInteractable.cs` (new)

```csharp
using UnityEngine;

/// <summary>
/// Anything the player can use with the interact key or a click while in reach:
/// carryables, sockets, panels, gauges, terminals, hold-to-work tasks.
/// </summary>
public interface IInteractable
{
    /// <summary>Point used for reach checks. Trigger-volume tasks return the closest point on their volume.</summary>
    Vector3 GetInteractionPoint(Vector3 from);
    bool CanInteract(PlayerInteractor interactor);
    string GetPrompt(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
}

/// <summary>Optional second action on the same target (e.g. "Report anomaly" on a gauge).</summary>
public interface ISecondaryInteractable
{
    string GetSecondaryPrompt(PlayerInteractor interactor);
    void InteractSecondary(PlayerInteractor interactor);
}
```

### 5.2 `Interaction/PlayerInteractor.cs` (new — replaces `PlayerInteraction`)

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single owner of "use" for the isometric camera. Reach is measured from the
/// player's body. Focus = mouse-hovered target if it's in reach, otherwise the
/// best nearby target by distance weighted toward the facing direction.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Reach")]
    [SerializeField] private float reachRadius = 1.8f;
    [Tooltip("0 = pure distance. Higher values prefer targets in front of the player.")]
    [SerializeField] private float facingBias = 0.75f;
    [SerializeField] private LayerMask usableLayer;

    [Header("Mouse")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float mouseRayDistance = 100f;

    [Header("Carry")]
    [SerializeField] private Transform holdPoint;             // the existing HeldItemTransform on the Player
    [SerializeField] private float dropForwardDistance = 0.8f;
    [SerializeField] private LayerMask dropSurfaceLayer;      // Ground

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode secondaryKey = KeyCode.R;
    [SerializeField] private KeyCode dropKey = KeyCode.G;

    [Header("UI")]
    [SerializeField] private Text interactionPrompt;

    private readonly Collider[] overlapBuffer = new Collider[32];
    private PlayerMovement movement;
    private IInteractable focused;
    private IInteractable pendingTarget;
    private CarryableItem carried;

    public CarryableItem Carried => carried;
    public bool IsCarrying => carried != null;
    public IInteractable Focused => focused;

    /// <summary>
    /// Interface references bypass UnityEngine.Object's null overload, so a destroyed
    /// MonoBehaviour held as IInteractable is "not null". Always check through this.
    /// </summary>
    public static bool IsAlive(IInteractable target)
    {
        if (target is Object unityObject)
            return unityObject != null;
        return target != null;
    }

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        if (viewCamera == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("PlayerCamera");
            if (camObj != null)
                viewCamera = camObj.GetComponent<Camera>();
        }
        if (holdPoint == null)
            Debug.LogWarning("[PlayerInteractor] No holdPoint assigned — carried items will attach to the player root.");
        HidePrompt();
    }

    void OnDisable()
    {
        pendingTarget = null;
        focused = null;
        HidePrompt();
    }

    void Update()
    {
        // WASD cancels a click-to-use walk, same as it cancels click-to-move.
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f)
            pendingTarget = null;

        focused = ResolveFocus();
        UpdatePrompt();

        if (Input.GetKeyDown(interactKey) && IsAlive(focused))
        {
            TryInteract(focused);
        }
        else if (Input.GetKeyDown(secondaryKey) && focused is ISecondaryInteractable secondary && focused.CanInteract(this))
        {
            secondary.InteractSecondary(this);
            NotifyActivity();
        }
        else if (Input.GetKeyDown(dropKey) && carried != null)
        {
            DropCarried();
        }

        if (IsAlive(pendingTarget) && IsInReach(pendingTarget))
        {
            IInteractable target = pendingTarget;
            pendingTarget = null;
            TryInteract(target);
        }
    }

    IInteractable ResolveFocus()
    {
        IInteractable hovered = GetHovered();
        if (IsAlive(hovered) && IsInReach(hovered) && hovered.CanInteract(this))
            return hovered;

        int count = Physics.OverlapSphereNonAlloc(transform.position, reachRadius, overlapBuffer,
            usableLayer, QueryTriggerInteraction.Collide);

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        IInteractable best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            IInteractable candidate = overlapBuffer[i].GetComponentInParent<IInteractable>();
            if (!IsAlive(candidate) || ReferenceEquals(candidate, carried) || !candidate.CanInteract(this))
                continue;

            Vector3 to = candidate.GetInteractionPoint(transform.position) - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance > reachRadius)
                continue;

            float facing = distance > 0.001f ? Vector3.Dot(forward, to / distance) : 1f;   // -1..1
            float score = distance * (1f + facingBias * (1f - facing));                    // behind you costs up to (1 + 2*bias)x
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }

    /// <summary>Solid colliders only — trigger volumes (task ranges, rooms) must not eat the hover.</summary>
    public IInteractable GetHovered()
    {
        if (viewCamera == null)
            return null;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, mouseRayDistance, usableLayer, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<IInteractable>();
        return null;
    }

    public bool IsInReach(IInteractable target)
    {
        if (!IsAlive(target))
            return false;
        Vector3 d = target.GetInteractionPoint(transform.position) - transform.position;
        d.y = 0f;
        return d.magnitude <= reachRadius;
    }

    /// <summary>Called by ClickToMove when the click hit a usable thing that's out of reach.</summary>
    public void SetPendingTarget(IInteractable target) => pendingTarget = target;

    public bool TryInteract(IInteractable target)
    {
        if (!IsAlive(target) || !IsInReach(target) || !target.CanInteract(this))
            return false;
        target.Interact(this);
        NotifyActivity();
        return true;
    }

    public void BeginCarry(CarryableItem item)
    {
        if (item == null || carried != null)
            return;
        carried = item;
        item.AttachTo(holdPoint != null ? holdPoint : transform);
        if (movement != null)
            movement.SetSpeedMultiplier(item.CarrySpeedMultiplier);
    }

    /// <summary>Hands the carried item to the caller (socket, recycler). The caller decides where it goes.</summary>
    public CarryableItem ReleaseCarried()
    {
        CarryableItem item = carried;
        carried = null;
        if (movement != null)
            movement.SetSpeedMultiplier(1f);
        return item;
    }

    public void DropCarried()
    {
        CarryableItem item = ReleaseCarried();
        if (item == null)
            return;

        Vector3 origin = transform.position + transform.forward * dropForwardDistance + Vector3.up;
        Vector3 drop = origin + Vector3.down;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, dropSurfaceLayer, QueryTriggerInteraction.Ignore))
            drop = hit.point;

        item.PlaceAt(drop + Vector3.up * item.GroundOffset, Quaternion.LookRotation(transform.forward, Vector3.up));
    }

    void NotifyActivity()
    {
        StationManager sm = StationManager.Instance;
        if (sm != null && sm.ShiftInProgress)
            sm.CurrentShift.NotifyPlayerActivity();
    }

    void UpdatePrompt()
    {
        if (interactionPrompt == null)
            return;

        string text = null;
        if (IsAlive(focused))
        {
            text = $"[{interactKey}] {focused.GetPrompt(this)}";
            if (focused is ISecondaryInteractable secondary)
                text += $"   [{secondaryKey}] {secondary.GetSecondaryPrompt(this)}";
        }
        if (carried != null)
            text = text == null ? $"[{dropKey}] Drop" : $"{text}   [{dropKey}] Drop";

        if (string.IsNullOrEmpty(text))
        {
            HidePrompt();
            return;
        }
        interactionPrompt.text = text;
        if (!interactionPrompt.gameObject.activeSelf)
            interactionPrompt.gameObject.SetActive(true);
    }

    void HidePrompt()
    {
        if (interactionPrompt != null && interactionPrompt.gameObject.activeSelf)
            interactionPrompt.gameObject.SetActive(false);
    }
}
```

**I disagree with keeping throw.** In an iso view, throwing is aim-less and the
physics result is random. It turns a work task into a toy. "Place in front of
you" (G) plus sockets is precise and readable. If you want throwing for fun,
add it back once the core loop is fun without it.

### 5.3 `Interaction/CarryableItem.cs` (new — replaces `OxygenTank`'s carry)

```csharp
using UnityEngine;

/// <summary>
/// Picked up into the player's hands or seated in an ItemSocket. While held/seated
/// it has no physics and no colliders of its own (no global IgnoreLayerCollision).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarryableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemType = "canister";         // sockets match on this
    [SerializeField] private string displayName = "Canister";
    [SerializeField] [Range(0.3f, 1f)] private float carrySpeedMultiplier = 0.75f;
    [SerializeField] private float groundOffset = 0.25f;

    private Rigidbody rb;
    private Collider[] colliders;
    private bool attached;

    public string ItemType => itemType;
    public virtual string DisplayName => displayName;
    public float CarrySpeedMultiplier => carrySpeedMultiplier;
    public float GroundOffset => groundOffset;
    public bool IsFree => !attached;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public Vector3 GetInteractionPoint(Vector3 from) => transform.position;
    public virtual bool CanInteract(PlayerInteractor interactor) => IsFree && !interactor.IsCarrying;
    public virtual string GetPrompt(PlayerInteractor interactor) => $"Pick up {DisplayName}";
    public virtual void Interact(PlayerInteractor interactor) => interactor.BeginCarry(this);

    /// <summary>Held in hands or seated in a socket.</summary>
    public void AttachTo(Transform anchor)
    {
        attached = true;
        rb.isKinematic = true;
        SetCollidersEnabled(false);
        transform.SetParent(anchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        attached = false;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(position, rotation);
        SetCollidersEnabled(true);
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void SetCollidersEnabled(bool value)
    {
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = value;
    }
}
```

### 5.4 `Interaction/ItemSocket.cs` (new)

```csharp
using UnityEngine;

/// <summary>A slot that takes one CarryableItem of a given type (manifold port, filter housing, recycler).</summary>
public class ItemSocket : MonoBehaviour, IInteractable
{
    [SerializeField] private string acceptsItemType = "canister";
    [SerializeField] private Transform seat;
    [SerializeField] private bool allowRemoval = true;

    private CarryableItem seated;

    public CarryableItem Seated => seated;
    public bool IsEmpty => seated == null;

    /// <summary>Optional extra rule set by the owning task.</summary>
    public System.Func<CarryableItem, bool> AcceptFilter { get; set; }

    public event System.Action<ItemSocket, CarryableItem> Inserted;
    public event System.Action<ItemSocket, CarryableItem> Removed;

    public bool Accepts(CarryableItem item) =>
        item != null && item.ItemType == acceptsItemType && (AcceptFilter == null || AcceptFilter(item));

    public Vector3 GetInteractionPoint(Vector3 from) => transform.position;

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (interactor.IsCarrying)
            return seated == null && Accepts(interactor.Carried);
        return seated != null && allowRemoval;
    }

    public string GetPrompt(PlayerInteractor interactor) =>
        interactor.IsCarrying ? $"Insert {interactor.Carried.DisplayName}" : $"Remove {seated.DisplayName}";

    public void Interact(PlayerInteractor interactor)
    {
        if (interactor.IsCarrying)
        {
            Seat(interactor.ReleaseCarried());
        }
        else if (seated != null)
        {
            CarryableItem item = seated;
            seated = null;
            interactor.BeginCarry(item);
            Removed?.Invoke(this, item);
        }
    }

    /// <summary>Also used by tasks to pre-seat items without a player.</summary>
    public void Seat(CarryableItem item)
    {
        if (item == null || seated != null)
            return;
        seated = item;
        item.AttachTo(seat != null ? seat : transform);
        Inserted?.Invoke(this, item);
    }
}
```

### 5.5 `Interaction/PanelFocusView.cs` (new)

```csharp
using System.Collections;
using UnityEngine;

public interface IFocusPanel
{
    Transform FocusAnchor { get; }
    void OnFocusEntered();
    void OnFocusExited();
}

public interface IFocusClickable
{
    void OnFocusClick();
}

/// <summary>
/// Blends a dedicated camera from the isometric view to an authored anchor in front
/// of a panel, freezes player control, and routes mouse clicks to IFocusClickable parts.
/// Generalizes what CamerasManager does for the PC screen.
/// </summary>
public class PanelFocusView : Singleton<PanelFocusView>
{
    [SerializeField] private Camera worldCamera;          // the isometric PlayerCamera
    [SerializeField] private Camera focusCamera;          // separate Camera, disabled in the scene
    [SerializeField] private float blendSeconds = 0.35f;
    [SerializeField] private LayerMask clickableLayer = ~0;
    [SerializeField] private float clickRayDistance = 5f;
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;
    [SerializeField] private KeyCode altExitKey = KeyCode.E;

    [Tooltip("PlayerMovement, ClickToMove, PlayerInteractor, CamerasManager — anything that reads E/click/WASD.")]
    [SerializeField] private Behaviour[] disableWhileFocused;

    private IFocusPanel active;
    private int enteredFrame = -1;
    private Coroutine blendRoutine;

    public bool IsFocused => active != null;

    protected override void Awake()
    {
        base.Awake();
        if (focusCamera != null)
            focusCamera.gameObject.SetActive(false);
    }

    public void Enter(IFocusPanel panel)
    {
        if (panel == null || active != null || focusCamera == null || worldCamera == null || panel.FocusAnchor == null)
            return;

        active = panel;
        enteredFrame = Time.frameCount;
        SetControlsEnabled(false);

        focusCamera.transform.SetPositionAndRotation(worldCamera.transform.position, worldCamera.transform.rotation);
        focusCamera.fieldOfView = worldCamera.fieldOfView;
        focusCamera.gameObject.SetActive(true);
        worldCamera.enabled = false;   // keep its GameObject (rig, occluder) alive; just stop rendering

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartBlend(panel.FocusAnchor.position, panel.FocusAnchor.rotation, null);
        panel.OnFocusEntered();
    }

    public void Exit()
    {
        if (active == null)
            return;

        IFocusPanel panel = active;
        active = null;
        StartBlend(worldCamera.transform.position, worldCamera.transform.rotation, () =>
        {
            focusCamera.gameObject.SetActive(false);
            worldCamera.enabled = true;
            SetControlsEnabled(true);
        });
        panel.OnFocusExited();
    }

    void Update()
    {
        if (active == null || Time.frameCount == enteredFrame)
            return;   // the same E press that opened the panel must not close it

        if (Input.GetKeyDown(exitKey) || Input.GetKeyDown(altExitKey))
        {
            Exit();
            return;
        }

        if (blendRoutine == null && Input.GetMouseButtonDown(0))
        {
            Ray ray = focusCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickableLayer, QueryTriggerInteraction.Collide))
            {
                IFocusClickable clickable = hit.collider.GetComponentInParent<IFocusClickable>();
                if (clickable != null)
                    clickable.OnFocusClick();
            }
        }
    }

    void SetControlsEnabled(bool value)
    {
        for (int i = 0; i < disableWhileFocused.Length; i++)
        {
            if (disableWhileFocused[i] != null)
                disableWhileFocused[i].enabled = value;
        }
    }

    void StartBlend(Vector3 toPos, Quaternion toRot, System.Action onDone)
    {
        if (blendRoutine != null)
            StopCoroutine(blendRoutine);
        blendRoutine = StartCoroutine(Blend(toPos, toRot, onDone));
    }

    IEnumerator Blend(Vector3 toPos, Quaternion toRot, System.Action onDone)
    {
        Transform t = focusCamera.transform;
        Vector3 fromPos = t.position;
        Quaternion fromRot = t.rotation;
        float elapsed = 0f;
        while (elapsed < blendSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, elapsed / blendSeconds);
            t.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, k), Quaternion.Slerp(fromRot, toRot, k));
            yield return null;
        }
        t.SetPositionAndRotation(toPos, toRot);
        blendRoutine = null;
        onDone?.Invoke();
    }
}
```

**Optional [Probable]:** Cinemachine 3.1.5 is already in the manifest. A
`CinemachineCamera` per panel with a priority bump would replace both the
blend coroutine and `IsometricCameraRig`, and reads well in a portfolio.
I wrote the plain version because it matches existing code and I can't
check the CM3 API here.

### 5.6 Patches to existing scripts for M1

**`PlayerMovement.cs`** — carry slowdown, a clean halt, no jump:

```csharp
private float speedMultiplier = 1f;

public void SetSpeedMultiplier(float multiplier) => speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);

/// <summary>Stop dead (focus view, cutscene, evaluation). Keeps vertical velocity for gravity.</summary>
public void Halt()
{
    clickMoveActive = false;
    inputDirection = Vector3.zero;
    targetVelocity = Vector3.zero;
    if (rb != null)
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
}

void OnDisable() => Halt();

// In HandleMovementInput:
float targetSpeed = (IsSprinting ? currentMovementSpeed * sprintMultiplier : currentMovementSpeed) * speedMultiplier;
// ...and delete the "Jump" block. Space belongs to TaskBehavior's soft-cancel.
```

**`ClickToMove.cs`** — becomes the single owner of left-click:

```csharp
[SerializeField] private PlayerInteractor interactor;
[SerializeField] private LayerMask usableLayer;
[SerializeField] private float destinationSnapRadius = 1.5f;

void Update()
{
    if (!Input.GetMouseButtonDown(0) || cam == null || playerMovement == null)
        return;

    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
    int mask = groundLayer | usableLayer;
    if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, mask, QueryTriggerInteraction.Ignore))
        return;

    Vector3 destination = hit.point;
    IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
    if (target != null && interactor != null)
    {
        if (interactor.TryInteract(target))
            return;                                   // in reach: use it, don't walk
        interactor.SetPendingTarget(target);          // walk there; PlayerInteractor fires it on arrival
        destination = target.GetInteractionPoint(transform.position);
    }

    // Interaction points sit on walls/benches, off the NavMesh — snap to the nearest walkable point.
    if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, destinationSnapRadius, NavMesh.AllAreas))
        destination = navHit.position;

    NavMeshPath path = new NavMeshPath();
    if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path)
        && path.status != NavMeshPathStatus.PathInvalid)
        playerMovement.SetClickMovePath(path.corners);
}
```

**`TaskBehavior.cs`** — becomes `IInteractable`, exposes events and
difficulty, and gets an opt-out for "progress resets when you step out":

```csharp
public class TaskBehavior : MonoBehaviour, IInteractable
{
    // new serialized fields
    [SerializeField] private string displayVerb = "work";
    [Tooltip("Off for spills/grime: partial scrubbing should persist if you step away.")]
    [SerializeField] private bool resetProgressOnExit = true;

    public event System.Action<TaskBehavior> ReachedOperational;
    public event System.Action<TaskBehavior> ReachedPerfected;

    /// <summary>Set by ManualShiftTask from WorkOrder.difficulty. 1 = authored speed.</summary>
    public float DifficultyMultiplier { get; set; } = 1f;
    public string TaskId => ResolveTaskId();

    // In Update: float delta = progressPerSecond / Mathf.Max(0.1f, DifficultyMultiplier) * Time.deltaTime;
    // In OnReachedOperational(): after OnTaskOperational(); add  ReachedOperational?.Invoke(this);
    // In OnReachedPerfected():   after OnTaskPerfected();   add  ReachedPerfected?.Invoke(this);
    // In OnTriggerExit: wrap the progress reset in  if (resetProgressOnExit && !taskCompleted) { ... }

    public Vector3 GetInteractionPoint(Vector3 from) => col != null ? col.ClosestPoint(from) : transform.position;
    public virtual bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && !taskCompleted && !interactor.IsCarrying;
    public virtual string GetPrompt(PlayerInteractor interactor) => $"Hold to {displayVerb} ({Mathf.RoundToInt(progress * 100f)}%)";
    public void Interact(PlayerInteractor interactor) { }   // progress comes from the hold, read via ITaskActor in Update
}
```

**`PlayerTaskActor.cs`** — only the focused task gets the hold. This
removes the "one E press both picks up a canister and scrubs a spill"
conflict:

```csharp
public class PlayerTaskActor : MonoBehaviour, ITaskActor
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode cancelKey = KeyCode.Space;
    [SerializeField] private PlayerInteractor interactor;

    void Awake()
    {
        if (interactor == null)
            interactor = GetComponent<PlayerInteractor>();
    }

    public bool WantsInteractHold(TaskBehavior task) =>
        Input.GetKey(interactKey) && (interactor == null || ReferenceEquals(interactor.Focused, task));

    public bool WantsCancelHold(TaskBehavior task) => Input.GetKey(cancelKey);
}
```

**`CamerasManager.cs`** — stop polling E yourself. Put this on each PC
position so the terminal goes through the same interactor:

```csharp
public class TerminalInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private CamerasManager camerasManager;
    [SerializeField] private string prompt = "Use terminal";

    public Vector3 GetInteractionPoint(Vector3 from) => transform.position;
    public bool CanInteract(PlayerInteractor interactor) => camerasManager != null && !interactor.IsCarrying;
    public string GetPrompt(PlayerInteractor interactor) => prompt;
    public void Interact(PlayerInteractor interactor) => camerasManager.SetCamera();
}
```

Then delete the `NearerstPos()` / `GetKeyDown(E)` block in
`CamerasManager.Update`, and keep only the Escape handling.
**[Probable]** Later, make the terminal an `IFocusPanel` and retire
`CamerasManager` entirely.

### 5.7 M1 acceptance

- A tank on the floor: hover it and press E, or walk up and press E, or
  click it from across the room (you walk there and pick it up). All three
  work.
- Standing in a cleaning trigger next to a tank, E picks up the tank. With
  nothing carried and facing the spill, holding E cleans.
- Space never makes the player jump.
- Opening the fuse board (via its panel `IInteractable`, see M3) blends the
  camera in, clicks toggle switches, and Esc blends back with movement
  restored.

---

## 6. M2 — The AI hands out the work: ShiftDirector, WorkOrders, Days

This is the "AI reinitializes tasks between days" feature, done so it also
fixes the metrics.

### 6.1 The design, in one paragraph

Today the world decides what's broken (whatever the drain empties) and the
AI only grades afterwards. **Flip it.** At the start of each day the AI
**plans** the shift:

- It picks tasks from a registry, weighted toward categories you **failed
  yesterday**. The AI's line: *"Allocation adjusted: Oxygen workload
  increased."* It isn't punishing you; it's optimizing.
- It schedules **incidents** for mid-shift (deadline orders).
- Things you left broken **carry over** as tomorrow's first orders.

It briefs you on the terminal before you accept. It grades you against
**its own work orders**: assigned vs. resolved, by priority, quality and
lateness. It doesn't grade against how many rooms you walked into.

### 6.2 Data types — `Progression/ShiftTypes.cs` (new)

```csharp
using System.Collections.Generic;
using UnityEngine;

public enum ShiftTaskCategory { Power, Oxygen, Sanitation, Maintenance, Inspection }
public enum WorkOrderPriority { Routine = 1, Priority = 2, Critical = 3 }   // value doubles as score weight
public enum WorkOrderState { Active, Operational, Completed, Failed, Expired }

[System.Serializable]
public class WorkOrder
{
    public string orderId;
    public string taskId;
    public string title;
    public string zoneId;
    public ShiftTaskCategory category;
    public WorkOrderPriority priority;
    public int difficulty;
    public int seed;                 // tasks use System.Random(seed) → reproducible shifts
    public bool carryOver;
    public float issuedAt;           // shift seconds
    public float deadline;           // shift seconds; <= 0 means "by end of shift"
    public WorkOrderState state = WorkOrderState.Active;
    public float quality;            // 0..1 reported by the task
    public int actionsUsed;
    public int parActions;
    public float resolvedAt = -1f;
    public string note;              // audit text the AI quotes back ("P-03 logged nominal")

    public bool IsOpen => state == WorkOrderState.Active || state == WorkOrderState.Operational;
    public bool WasLate => deadline > 0f && resolvedAt > deadline;
}

public struct TaskReport
{
    public WorkOrderState state;
    public float quality;
    public int actionsUsed;
    public int parActions;
    public string note;

    public static TaskReport Operational(float quality) =>
        new TaskReport { state = WorkOrderState.Operational, quality = quality };

    public static TaskReport Completed(float quality, int actions, int par, string note = null) =>
        new TaskReport { state = WorkOrderState.Completed, quality = quality, actionsUsed = actions, parActions = par, note = note };

    public static TaskReport Failed(string note = null) =>
        new TaskReport { state = WorkOrderState.Failed, note = note };
}

/// <summary>
/// Anything the ShiftDirector can assign. Implementations "break" or arm the world in
/// Assign, report progress via Reported, and on Revoke stop tracking WITHOUT fixing
/// the world — unresolved faults persist and surface as NeedsAttention next day.
/// </summary>
public interface IShiftTask
{
    string TaskId { get; }
    string DisplayName { get; }
    string ZoneId { get; }
    ShiftTaskCategory Category { get; }
    int UnlockDay { get; }
    bool IsAssigned { get; }
    bool NeedsAttention { get; }
    void Assign(WorkOrder order);
    void Revoke();
    event System.Action<IShiftTask, TaskReport> Reported;
}

public static class ShiftTaskRegistry
{
    private static readonly List<IShiftTask> tasks = new List<IShiftTask>();
    public static IReadOnlyList<IShiftTask> All => tasks;

    public static void Register(IShiftTask task)
    {
        if (task != null && !tasks.Contains(task))
            tasks.Add(task);
    }

    public static void Unregister(IShiftTask task) => tasks.Remove(task);

    // Survives "Enter Play Mode Options → no domain reload" without leaking last session's tasks.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => tasks.Clear();
}
```

### 6.3 Policy as data — `Progression/ShiftPolicy.cs` (new ScriptableObject)

This matches `PORTFOLIO_CONTEXT.md`'s "policy data (ScriptableObjects) vs
scoring code" split. It's also where the difficulty curve lives, so you
tune curves instead of editing code.

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Station/Shift Policy", fileName = "ShiftPolicy")]
public class ShiftPolicy : ScriptableObject
{
    [Header("Orders issued at shift start (by day)")]
    public AnimationCurve baseOrdersByDay = AnimationCurve.Linear(1, 2, 10, 5);

    [Header("Incidents during the shift (by day)")]
    public AnimationCurve incidentsByDay = AnimationCurve.Linear(1, 0, 10, 3);
    [Range(0f, 1f)] public float incidentEarliestFraction = 0.15f;
    [Range(0f, 1f)] public float incidentLatestFraction = 0.80f;
    [Range(0f, 1f)] public float criticalChance = 0.25f;
    public float priorityDeadlineSeconds = 150f;
    public float criticalDeadlineSeconds = 90f;

    [Header("Difficulty (by day), 1..5")]
    public AnimationCurve difficultyByDay = AnimationCurve.Linear(1, 1, 12, 4);

    [Header("Targeting")]
    [Tooltip("Extra pick weight per unresolved order of that category yesterday.")]
    public float failedCategoryWeightBonus = 1.5f;
    public bool autoIssueMaintenanceOrders = true;

    [Header("Overnight")]
    [Range(0f, 1f)] public float overnightStorageDrain = 0.15f;
    [Range(0f, 1f)] public float overnightConditionDecay = 0.05f;

    [Header("Employment")]
    public int noticesToTerminate = 3;
}
```

### 6.4 `Progression/ShiftDirector.cs` (new)

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The AI's hands. Plans each day's work orders (weighted toward yesterday's
/// failures), issues mid-shift incidents with deadlines, auto-issues maintenance
/// for anything that broke on its own, and returns the order list for evaluation.
/// AIManager stays the judge; this is the dispatcher.
/// </summary>
public class ShiftDirector : MonoBehaviour
{
    [SerializeField] private ShiftPolicy policy;
    [Tooltip("0 = fresh random plan each day. Non-zero = reproducible plans (seed + day) for tests and demo capture.")]
    [SerializeField] private int seed;

    private readonly List<WorkOrder> orders = new List<WorkOrder>();
    private readonly List<(IShiftTask task, WorkOrder order)> planned = new List<(IShiftTask, WorkOrder)>();
    private readonly HashSet<IShiftTask> plannedSet = new HashSet<IShiftTask>();
    private readonly Dictionary<IShiftTask, WorkOrder> openByTask = new Dictionary<IShiftTask, WorkOrder>();
    private readonly List<float> incidentTimes = new List<float>();
    private readonly Dictionary<ShiftTaskCategory, int> failuresLastShift = new Dictionary<ShiftTaskCategory, int>();

    private System.Random rng;
    private bool planReady;
    private bool running;
    private int day = 1;
    private int difficulty = 1;
    private int orderSerial;

    public IReadOnlyList<WorkOrder> Orders => orders;
    public IReadOnlyDictionary<ShiftTaskCategory, int> FailuresLastShift => failuresLastShift;
    public bool Running => running;

    public event System.Action<WorkOrder> OrderIssued;
    public event System.Action<WorkOrder> OrderUpdated;

    float Clock => StationManager.Instance != null ? StationManager.Instance.ShiftElapsedSeconds : 0f;

    /// <summary>Build tomorrow's plan (called when a day starts, before Accept Shift) so the briefing can show it.</summary>
    public void PlanShift(int currentDay, float shiftDurationSeconds)
    {
        if (policy == null)
        {
            Debug.LogError("[ShiftDirector] No ShiftPolicy assigned.");
            return;
        }

        RevokeAll();
        orders.Clear();
        planned.Clear();
        plannedSet.Clear();
        incidentTimes.Clear();
        orderSerial = 0;
        running = false;

        day = Mathf.Max(1, currentDay);
        rng = seed != 0 ? new System.Random(seed * 397 ^ day) : new System.Random();
        difficulty = Mathf.Clamp(Mathf.RoundToInt(policy.difficultyByDay.Evaluate(day)), 1, 5);

        // 1) Yesterday's mess first.
        IReadOnlyList<IShiftTask> all = ShiftTaskRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            IShiftTask t = all[i];
            if (t != null && t.NeedsAttention && t.UnlockDay <= day)
                Plan(t, WorkOrderPriority.Routine, carryOver: true);
        }

        // 2) Fresh routine orders.
        int baseOrders = Mathf.Max(0, Mathf.RoundToInt(policy.baseOrdersByDay.Evaluate(day)));
        for (int i = 0; i < baseOrders; i++)
        {
            IShiftTask pick = PickTask();
            if (pick == null)
                break;
            Plan(pick, WorkOrderPriority.Routine, carryOver: false);
        }

        // 3) Incident times (tasks are picked when they fire, from whatever's free then).
        int incidents = Mathf.Max(0, Mathf.RoundToInt(policy.incidentsByDay.Evaluate(day)));
        for (int i = 0; i < incidents; i++)
        {
            float f = Mathf.Lerp(policy.incidentEarliestFraction, policy.incidentLatestFraction, (float)rng.NextDouble());
            incidentTimes.Add(f * shiftDurationSeconds);
        }
        incidentTimes.Sort();

        planReady = true;
    }

    /// <summary>Called from StationManager.StartShift. Arms every planned task.</summary>
    public void BeginShift(int currentDay, float shiftDurationSeconds)
    {
        if (!planReady || day != Mathf.Max(1, currentDay))
            PlanShift(currentDay, shiftDurationSeconds);
        if (policy == null)
            return;

        running = true;
        foreach ((IShiftTask task, WorkOrder order) in planned)
        {
            order.issuedAt = Clock;
            Activate(task, order);
        }
        planned.Clear();
        plannedSet.Clear();
        planReady = false;
    }

    void Update()
    {
        if (!running)
            return;
        float now = Clock;

        while (incidentTimes.Count > 0 && incidentTimes[0] <= now)
        {
            incidentTimes.RemoveAt(0);
            IShiftTask pick = PickTask();
            if (pick == null)
                continue;
            bool critical = rng.NextDouble() < policy.criticalChance;
            WorkOrderPriority priority = critical ? WorkOrderPriority.Critical : WorkOrderPriority.Priority;
            float window = critical ? policy.criticalDeadlineSeconds : policy.priorityDeadlineSeconds;
            Activate(pick, CreateOrder(pick, priority, now + window, carryOver: false));
        }

        if (policy.autoIssueMaintenanceOrders)
        {
            IReadOnlyList<IShiftTask> all = ShiftTaskRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                IShiftTask t = all[i];
                if (t != null && !t.IsAssigned && t.NeedsAttention && t.UnlockDay <= day)
                    Activate(t, CreateOrder(t, WorkOrderPriority.Routine, 0f, carryOver: false));
            }
        }
    }

    /// <summary>For tasks that spawn follow-ups (e.g. an inspection anomaly → repair order).</summary>
    public bool IssueFollowUp(IShiftTask task, WorkOrderPriority priority)
    {
        if (!running || task == null || task.IsAssigned)
            return false;
        float window = priority == WorkOrderPriority.Critical ? policy.criticalDeadlineSeconds
                     : priority == WorkOrderPriority.Priority ? policy.priorityDeadlineSeconds : 0f;
        Activate(task, CreateOrder(task, priority, window > 0f ? Clock + window : 0f, carryOver: false));
        return true;
    }

    /// <summary>Called from StationManager.EndShift. Closes the books and returns a copy for ShiftMetrics.</summary>
    public List<WorkOrder> EndShift()
    {
        running = false;
        float now = Clock;
        for (int i = 0; i < orders.Count; i++)
        {
            WorkOrder o = orders[i];
            if (!o.IsOpen)
                continue;
            o.resolvedAt = now;                          // lets WasLate see open orders past their deadline
            if (o.state == WorkOrderState.Active)
                o.state = WorkOrderState.Expired;        // Operational keeps partial credit
            OrderUpdated?.Invoke(o);
        }

        RevokeAll();

        failuresLastShift.Clear();
        for (int i = 0; i < orders.Count; i++)
        {
            WorkOrder o = orders[i];
            if (o.state == WorkOrderState.Expired || o.state == WorkOrderState.Failed)
            {
                failuresLastShift.TryGetValue(o.category, out int n);
                failuresLastShift[o.category] = n + 1;
            }
        }
        return new List<WorkOrder>(orders);
    }

    // ---------- internals ----------

    void Plan(IShiftTask task, WorkOrderPriority priority, bool carryOver)
    {
        WorkOrder order = CreateOrder(task, priority, 0f, carryOver);
        planned.Add((task, order));
        plannedSet.Add(task);
    }

    WorkOrder CreateOrder(IShiftTask task, WorkOrderPriority priority, float deadline, bool carryOver)
    {
        orderSerial++;
        WorkOrder order = new WorkOrder
        {
            orderId = $"WO-{day:00}-{orderSerial:000}",
            taskId = task.TaskId,
            title = carryOver ? $"Carry-over: {task.DisplayName}" : task.DisplayName,
            zoneId = task.ZoneId,
            category = task.Category,
            priority = priority,
            difficulty = Mathf.Clamp(difficulty + (priority == WorkOrderPriority.Critical ? 1 : 0), 1, 5),
            seed = rng.Next(),
            carryOver = carryOver,
            issuedAt = Clock,
            deadline = deadline,
            state = WorkOrderState.Active,
        };
        orders.Add(order);
        OrderIssued?.Invoke(order);
        return order;
    }

    void Activate(IShiftTask task, WorkOrder order)
    {
        openByTask[task] = order;
        task.Reported += HandleReport;
        task.Assign(order);
    }

    void HandleReport(IShiftTask task, TaskReport report)
    {
        if (!openByTask.TryGetValue(task, out WorkOrder order))
            return;

        order.quality = Mathf.Clamp01(report.quality);
        order.actionsUsed = report.actionsUsed;
        order.parActions = report.parActions;
        if (!string.IsNullOrEmpty(report.note))
            order.note = report.note;

        if (report.state == WorkOrderState.Operational)
        {
            if (order.state == WorkOrderState.Active)
                order.state = WorkOrderState.Operational;
        }
        else if (report.state == WorkOrderState.Completed || report.state == WorkOrderState.Failed)
        {
            order.state = report.state;
            order.resolvedAt = Clock;
            openByTask.Remove(task);
            task.Reported -= HandleReport;
        }
        OrderUpdated?.Invoke(order);
    }

    void RevokeAll()
    {
        if (openByTask.Count == 0)
            return;
        List<IShiftTask> open = new List<IShiftTask>(openByTask.Keys);
        openByTask.Clear();
        for (int i = 0; i < open.Count; i++)
        {
            open[i].Reported -= HandleReport;
            open[i].Revoke();
        }
    }

    IShiftTask PickTask()
    {
        IReadOnlyList<IShiftTask> all = ShiftTaskRegistry.All;
        float total = 0f;
        for (int i = 0; i < all.Count; i++)
            if (Eligible(all[i])) total += Weight(all[i]);
        if (total <= 0f)
            return null;

        double roll = rng.NextDouble() * total;
        IShiftTask last = null;
        for (int i = 0; i < all.Count; i++)
        {
            if (!Eligible(all[i]))
                continue;
            last = all[i];
            roll -= Weight(all[i]);
            if (roll <= 0.0)
                return all[i];
        }
        return last;   // float rounding fallback
    }

    bool Eligible(IShiftTask t) => t != null && !t.IsAssigned && !plannedSet.Contains(t) && t.UnlockDay <= day;

    float Weight(IShiftTask t)
    {
        failuresLastShift.TryGetValue(t.Category, out int failures);
        return 1f + failures * policy.failedCategoryWeightBonus;
    }
}
```

**Known limitation [Certain]:** `failuresLastShift` isn't saved. Reload the
game and the first day's targeting is neutral. Add it to `CareerState` as a
list of `(category, count)` if that matters.

### 6.5 Adapter for existing hold tasks — `Progression/ManualShiftTask.cs` (new)

```csharp
using UnityEngine;

/// <summary>
/// Makes any TaskBehavior (CleaningTask, SpillCleaningTask…) assignable by the ShiftDirector.
/// The task and its visuals only exist in the world while there's something to do.
/// An unfinished task stays dirty overnight and comes back as a carry-over order.
/// </summary>
[RequireComponent(typeof(TaskBehavior))]
public class ManualShiftTask : MonoBehaviour, IShiftTask
{
    [SerializeField] private string displayName = "Clean spill";
    [SerializeField] private string zoneId = "hub";
    [SerializeField] private ShiftTaskCategory category = ShiftTaskCategory.Sanitation;
    [SerializeField] private int unlockDay = 1;
    [Tooltip("Shown only while dirty: spill decal, hazard sign, grime.")]
    [SerializeField] private GameObject[] dirtyVisuals;
    [SerializeField] private float operationalQuality = 0.75f;

    private TaskBehavior task;
    private WorkOrder order;
    private bool dirty;

    public string TaskId => task != null ? task.TaskId : name;
    public string DisplayName => displayName;
    public string ZoneId => zoneId;
    public ShiftTaskCategory Category => category;
    public int UnlockDay => unlockDay;
    public bool IsAssigned => order != null;
    public bool NeedsAttention => dirty && order == null;
    public event System.Action<IShiftTask, TaskReport> Reported;

    void Awake()
    {
        task = GetComponent<TaskBehavior>();
        SetLive(false);
    }

    void OnEnable()
    {
        ShiftTaskRegistry.Register(this);
        task.ReachedOperational += HandleOperational;
        task.ReachedPerfected += HandlePerfected;
    }

    void OnDisable()
    {
        ShiftTaskRegistry.Unregister(this);
        task.ReachedOperational -= HandleOperational;
        task.ReachedPerfected -= HandlePerfected;
    }

    public void Assign(WorkOrder newOrder)
    {
        order = newOrder;
        if (!dirty)
            task.ResetForNewShift();          // fresh mess; a carry-over keeps its partial progress
        dirty = true;
        task.DifficultyMultiplier = 1f + 0.25f * (newOrder.difficulty - 1);
        SetLive(true);
    }

    public void Revoke()
    {
        order = null;
        task.enabled = false;                  // visuals stay if still dirty
    }

    void HandleOperational(TaskBehavior _)
    {
        if (order != null)
            Reported?.Invoke(this, TaskReport.Operational(operationalQuality));
    }

    void HandlePerfected(TaskBehavior _)
    {
        dirty = false;
        SetLive(false);
        if (order != null)
        {
            order = null;
            Reported?.Invoke(this, TaskReport.Completed(1f, 1, 1));
        }
    }

    void SetLive(bool live)
    {
        task.enabled = live;
        for (int i = 0; i < dirtyVisuals.Length; i++)
        {
            if (dirtyVisuals[i] != null)
                dirtyVisuals[i].SetActive(live);
        }
    }
}
```

> Note: in `HandlePerfected` I clear `order` **before** invoking. The director
> keys on the task, not the order field, so the report still lands.
> `StationManager.ResetAllTaskBehaviorsForNewShift()` runs at every shift
> start and resets progress for carry-overs too. **[Probable]** That's fine
> for v1; remove the call once every TaskBehavior is behind a
> `ManualShiftTask`.

### 6.6 `ShiftMetrics.cs` additions

```csharp
[Header("Work orders (issued by ShiftDirector)")]
public List<WorkOrder> workOrders = new List<WorkOrder>();
public int wastedItems;

public float SecondsSinceActivity => Time.time - lastActivityTime;

// In StartShift(): workOrders.Clear(); wastedItems = 0;

public void SetWorkOrders(List<WorkOrder> orders) => workOrders = orders ?? new List<WorkOrder>();

public void RecordWaste() => wastedItems++;

public int CountWorkOrders(WorkOrderState state)
{
    int n = 0;
    foreach (WorkOrder o in workOrders)
        if (o.state == state) n++;
    return n;
}

/// <summary>
/// Priority-weighted credit: Completed = 0.7 + 0.3·quality, Operational = 0.45,
/// anything else 0; late resolution ×0.6. Critical orders weigh 3× routine.
/// </summary>
public float GetWorkOrderScore()
{
    float earned = 0f, possible = 0f;
    foreach (WorkOrder o in workOrders)
    {
        float weight = (int)o.priority;
        possible += weight;

        float credit = o.state == WorkOrderState.Completed ? 0.7f + 0.3f * o.quality
                     : o.state == WorkOrderState.Operational ? 0.45f
                     : 0f;
        if (o.WasLate)
            credit *= 0.6f;
        earned += weight * credit;
    }
    return possible > 0f ? earned / possible : 0f;
}
```

### 6.7 `AIManager.cs` additions

```csharp
[Header("Employment")]
[Tooltip("Scores below this line earn a formal notice. Rises with strictness (replaces toleranceThreshold).")]
[SerializeField] private float probationBase = 0.40f;

public float ProbationLine => 1f - (1f - probationBase) / Mathf.Max(1f, strictnessLevel);
public string StandardsLabel => GetStandardsLevel();

public void RestoreProgression(int shifts, float strictness)
{
    shiftsCompleted = Mathf.Max(0, shifts);
    strictnessLevel = Mathf.Clamp(strictness, 1f, 3f);
}

// CalculateCompletionScore — first lines:
if (metrics.workOrders != null && metrics.workOrders.Count > 0)
    return metrics.GetWorkOrderScore();
// ...legacy room/manual scoring stays as the fallback

// GenerateObservations — append:
foreach (WorkOrder o in metrics.workOrders)
{
    if (o.priority == WorkOrderPriority.Critical && o.state != WorkOrderState.Completed)
        observations.Add($"Warning: Critical order {o.orderId} ({o.title}) unresolved.");
    else if (o.WasLate)
        observations.Add($"Note: {o.orderId} resolved {o.resolvedAt - o.deadline:F0}s past deadline.");

    if (o.state == WorkOrderState.Completed && o.parActions > 0 && o.actionsUsed > o.parActions * 2)
        observations.Add($"Advisory: {o.orderId} required {o.actionsUsed} actions. Reference procedure: {o.parActions}.");

    if (!string.IsNullOrEmpty(o.note))
        observations.Add($"Audit {o.orderId}: {o.note}");
}
if (metrics.wastedItems > 0)
    observations.Add($"Advisory: {metrics.wastedItems} serviceable unit(s) disposed.");

public string GenerateBriefing(int day, IReadOnlyList<WorkOrder> orders, IReadOnlyDictionary<ShiftTaskCategory, int> failuresLastShift)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"OPERATOR BRIEFING — DAY {day:00}");
    sb.AppendLine($"Performance standards: {GetStandardsLevel()}");
    sb.AppendLine($"Probation line: {ProbationLine * 100f:F0}%");
    sb.AppendLine();
    sb.AppendLine($"ASSIGNED WORK ORDERS: {orders.Count}");
    foreach (WorkOrder o in orders)
        sb.AppendLine($"  {o.orderId}  {o.title}  [{o.priority}]");
    sb.AppendLine("Additional orders may be issued during the shift.");

    if (failuresLastShift != null)
    {
        foreach (var kvp in failuresLastShift)
        {
            if (kvp.Value > 0)
                sb.AppendLine($"Allocation adjusted: {kvp.Key} workload increased ({kvp.Value} unresolved).");
        }
    }
    return sb.ToString();
}
```

Delete `toleranceThreshold` (B4) and have `GetAIStatus()` print `ProbationLine`
instead.

### 6.8 Save state — `Progression/CareerState.cs` (new)

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class DayRecord
{
    public int day;
    public string classification;
    public float score;
    public int ordersIssued;
    public int ordersCompleted;
    public int incidents;               // contamination events that day
}

[System.Serializable]
public class StorageSnapshot
{
    public int level;
    public float maxAmount;
    public float amount;
}

[System.Serializable]
public class CareerState
{
    public int version = 1;
    public int day = 1;
    public int shiftsCompleted;
    public float strictness = 1f;
    public float points = 500f;
    public int notices;
    public bool terminated;
    public int workstationLevel = 1;
    public StorageSnapshot power = new StorageSnapshot();
    public StorageSnapshot oxygen = new StorageSnapshot();
    public List<StationCondition.Zone> zones = new List<StationCondition.Zone>();
    public List<DayRecord> history = new List<DayRecord>();
}

public static class CareerStore
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "career.json");

    public static bool Exists => File.Exists(SavePath);

    public static CareerState Load()
    {
        try
        {
            if (File.Exists(SavePath))
                return JsonUtility.FromJson<CareerState>(File.ReadAllText(SavePath)) ?? new CareerState();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CareerStore] Corrupt save, starting fresh: {e.Message}");
        }
        return new CareerState();
    }

    /// <summary>Write-then-swap so a crash mid-write never leaves a half-written save.</summary>
    public static void Save(CareerState state)
    {
        string tmp = SavePath + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(state, true));
        if (File.Exists(SavePath))
            File.Replace(tmp, SavePath, null);
        else
            File.Move(tmp, SavePath);
    }

    public static void Delete()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }
}
```

Not saved in v1: mask, oxygen-tank upgrades, health, in-world fault states
(half-done breaker boards reset on reload). **[Certain]** List them in the
README so nobody thinks it's a bug.

### 6.9 The day loop — `Progression/DayCycle.cs` (new)

"Reinitializing between days" happens here, in order:

1. **Close the books** (`StationManager.EndShift` → `ShiftDirector.EndShift`):
   open orders expire, and tasks are revoked but *not fixed*.
2. **Record** the day, and issue a formal notice if the score is under the
   probation line.
3. On **Continue**: day++, overnight drain, condition decay, player back to
   spawn, autosave.
4. **Plan** tomorrow (director) and show the **briefing** on the terminal
   before Accept.
5. On **Accept**: reset hold-tasks, arm the planned tasks, start the clock.

```csharp
using System.Collections;
using UnityEngine;

public class DayCycle : MonoBehaviour
{
    [SerializeField] private ShiftPolicy policy;
    [SerializeField] private ShiftDirector director;
    [SerializeField] private AIManager aiManager;
    [SerializeField] private ShiftTerminalUI terminal;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private bool loadSaveOnStart = true;

    private CareerState career;

    public int Day => career != null ? career.day : 1;
    public CareerState Career => career;
    public bool IsTerminated => career != null && career.terminated;

    IEnumerator Start()
    {
        career = loadSaveOnStart ? CareerStore.Load() : new CareerState();
        if (aiManager == null && StationManager.Instance != null)
            aiManager = StationManager.Instance.AIManager;

        // Let every task's Awake/Start (canister seating, grid building) finish before planning.
        yield return null;

        ApplyCareerToWorld();
        BeginDay();
    }

    /// <summary>Called by StationManager.EndShift after AI evaluation + progression increment.</summary>
    public void RecordShiftResult(ShiftEvaluation evaluation, ShiftMetrics metrics)
    {
        career.history.Add(new DayRecord
        {
            day = career.day,
            classification = evaluation.classification,
            score = evaluation.overallScore,
            ordersIssued = metrics.workOrders.Count,
            ordersCompleted = metrics.CountWorkOrders(WorkOrderState.Completed),
            incidents = metrics.contaminationEvents,
        });

        if (aiManager != null && evaluation.overallScore < aiManager.ProbationLine)
        {
            career.notices++;
            evaluation.observations.Add($"FORMAL NOTICE {career.notices}/{policy.noticesToTerminate} issued.");
            if (career.notices >= policy.noticesToTerminate)
                career.terminated = true;
        }

        CaptureWorldToCareer();
        CareerStore.Save(career);
    }

    /// <summary>Called by StationManager.ContinueToNextShift.</summary>
    public void AdvanceDay()
    {
        if (career.terminated)
        {
            // TODO(M2 polish): termination screen with career summary, then CareerStore.Delete() + reload scene.
            Debug.Log("[DayCycle] Contract terminated.");
            return;
        }

        career.day++;
        ApplyOvernight();
        CaptureWorldToCareer();
        CareerStore.Save(career);
        BeginDay();
    }

    void BeginDay()
    {
        StationManager sm = StationManager.Instance;
        if (director != null && sm != null)
        {
            director.PlanShift(career.day, sm.ShiftDurationSeconds);
            if (terminal != null && aiManager != null)
                terminal.SetShiftLabel(aiManager.GenerateBriefing(career.day, director.Orders, director.FailuresLastShift));
        }
        GameEvents.TriggerDayStarted(career.day);
    }

    void ApplyOvernight()
    {
        StationManager sm = StationManager.Instance;
        if (sm == null)
            return;

        Drain(sm.PowerStorage, policy.overnightStorageDrain);
        Drain(sm.OxygenStorage, policy.overnightStorageDrain);

        if (StationCondition.Instance != null)
            StationCondition.Instance.DecayAll(policy.overnightConditionDecay);

        if (playerSpawn != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerMovement pm = player.GetComponent<PlayerMovement>();
                if (pm != null) pm.Halt();
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.position = playerSpawn.position;
                player.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            }
        }
    }

    static void Drain(Storage storage, float fraction)
    {
        if (storage != null)
            storage.amount = Mathf.Max(0f, storage.amount - storage.maxAmount * fraction);
    }

    void ApplyCareerToWorld()
    {
        StationManager sm = StationManager.Instance;
        if (sm == null || career.history.Count == 0 && career.day == 1)
            return;   // fresh career: scene defaults win

        sm.Points = career.points;
        if (aiManager != null)
            aiManager.RestoreProgression(career.shiftsCompleted, career.strictness);
        Restore(sm.PowerStorage, career.power);
        Restore(sm.OxygenStorage, career.oxygen);
        if (sm.WorkStation != null)
            sm.WorkStation.RestoreLevel(career.workstationLevel);
        if (StationCondition.Instance != null && career.zones.Count > 0)
            StationCondition.Instance.Restore(career.zones);
    }

    void CaptureWorldToCareer()
    {
        StationManager sm = StationManager.Instance;
        if (sm == null)
            return;

        career.points = sm.Points;
        if (aiManager != null)
        {
            career.shiftsCompleted = aiManager.ShiftsCompleted;
            career.strictness = aiManager.StrictnessLevel;
        }
        career.power = Snapshot(sm.PowerStorage);
        career.oxygen = Snapshot(sm.OxygenStorage);
        if (sm.WorkStation != null)
            career.workstationLevel = sm.WorkStation.Level;
        if (StationCondition.Instance != null)
            career.zones = StationCondition.Instance.Capture();
    }

    static StorageSnapshot Snapshot(Storage s) =>
        s == null ? new StorageSnapshot() : new StorageSnapshot { level = s.level, maxAmount = s.maxAmount, amount = s.amount };

    static void Restore(Storage s, StorageSnapshot snap)
    {
        if (s == null || snap == null || snap.maxAmount <= 0f)
            return;
        s.level = snap.level;
        s.maxAmount = snap.maxAmount;
        s.amount = snap.amount;
    }
}
```

> ⚠️ `Storage.Start()` sets `amount = maxAmount`. If `DayCycle`'s first frame
> runs before `Storage.Start`, the restore gets overwritten. The
> `yield return null` above covers that **[Probable]**. The robust fix is to
> make `Storage` fill only when `amount < 0` (use −1 as a "fresh" sentinel).

### 6.10 `StationManager.cs` and `GameEvents.cs` patches

```csharp
// StationManager — fields/properties
[Header("Progression")]
[SerializeField] private ShiftDirector shiftDirector;
[SerializeField] private DayCycle dayCycle;

public float ShiftElapsedSeconds => shiftTimer;
public ShiftDirector ShiftDirector => shiftDirector;
public int CurrentDay => dayCycle != null ? dayCycle.Day : 1;

// StartShift(), right after ResetAllTaskBehaviorsForNewShift():
if (shiftDirector != null)
    shiftDirector.BeginShift(CurrentDay, shiftDuration);
GameEvents.TriggerShiftStarted();

// EndShift(), replace the middle section:
currentShift.EndShift();
if (shiftDirector != null)
    currentShift.SetWorkOrders(shiftDirector.EndShift());
shiftInProgress = false;
StopWork();
if (endShiftButton != null) endShiftButton.gameObject.SetActive(false);

ShiftEvaluation evaluation = aiManager.EvaluateShift(currentShift);
aiManager.IncrementShiftProgression();
if (dayCycle != null)
    dayCycle.RecordShiftResult(evaluation, currentShift);   // may append a FORMAL NOTICE observation
ShowPerformanceReview(evaluation);                          // show AFTER so the notice is on screen
GameEvents.TriggerShiftEnded();

// ContinueToNextShift(), at the end:
if (dayCycle != null)
    dayCycle.AdvanceDay();

// Update(): only accumulate idle during a shift (B14)
if (shiftInProgress && currentShift != null)
    currentShift.AccumulateIdleTime(Time.deltaTime);
```

```csharp
// GameEvents.cs — add
public static event System.Action OnShiftStarted;
public static event System.Action OnShiftEnded;
public static event System.Action<int> OnDayStarted;

public static void TriggerShiftStarted() => OnShiftStarted?.Invoke();
public static void TriggerShiftEnded() => OnShiftEnded?.Invoke();
public static void TriggerDayStarted(int day) => OnDayStarted?.Invoke(day);
```

> I moved `IncrementShiftProgression` **before** `ShowPerformanceReview`.
> The evaluation was already computed, so what's shown doesn't change, and
> the save captures the new strictness.

### 6.11 In-world order list — `UI/WorkOrderBoardUI.cs` (new)

```csharp
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Live list of today's orders with deadline countdowns. Put it on the HUD and/or a wall screen.</summary>
public class WorkOrderBoardUI : MonoBehaviour
{
    [SerializeField] private ShiftDirector director;
    [SerializeField] private TextMeshProUGUI listText;
    [SerializeField] private Color routineColor = new Color(0.8f, 0.85f, 0.9f);
    [SerializeField] private Color priorityColor = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color doneColor = new Color(0.4f, 0.9f, 0.5f);
    [SerializeField] private float refreshInterval = 0.25f;

    private readonly StringBuilder sb = new StringBuilder();
    private float nextRefresh;

    void OnEnable()
    {
        if (director == null) return;
        director.OrderIssued += OnOrderChanged;
        director.OrderUpdated += OnOrderChanged;
        Rebuild();
    }

    void OnDisable()
    {
        if (director == null) return;
        director.OrderIssued -= OnOrderChanged;
        director.OrderUpdated -= OnOrderChanged;
    }

    void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
            Rebuild();   // deadline countdowns
    }

    void OnOrderChanged(WorkOrder _) => Rebuild();

    void Rebuild()
    {
        nextRefresh = Time.unscaledTime + refreshInterval;
        if (director == null || listText == null)
            return;

        StationManager sm = StationManager.Instance;
        float now = sm != null ? sm.ShiftElapsedSeconds : 0f;

        sb.Clear();
        foreach (WorkOrder o in director.Orders)
        {
            Color c = !o.IsOpen ? doneColor
                    : o.priority == WorkOrderPriority.Critical ? criticalColor
                    : o.priority == WorkOrderPriority.Priority ? priorityColor
                    : routineColor;
            string mark = o.state == WorkOrderState.Completed ? "[x]"
                        : o.state == WorkOrderState.Operational ? "[~]"
                        : o.IsOpen ? "[ ]" : "[!]";
            string timer = o.IsOpen && o.deadline > 0f ? $"  {Mathf.Max(0f, o.deadline - now):0}s" : "";
            sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>')
              .Append(mark).Append(' ').Append(o.orderId).Append("  ").Append(o.title).Append(timer)
              .AppendLine("</color>");
        }
        listText.text = sb.Length > 0 ? sb.ToString() : "No active work orders.";
    }
}
```

### 6.12 M2 acceptance

- Three in-game days in a row. Each plan differs; day 3 has more orders
  and at least one incident with a live countdown.
- Leave a spill on day 1. It's "Carry-over: Clean spill" at the top of day
  2's briefing.
- Fail the oxygen orders on day 1. The day 2 briefing says "Allocation
  adjusted: Oxygen…" and oxygen tasks show up more often (check over a few
  seeds).
- Quit and reload: day number, money, strictness and history are restored.
- With `seed = 1234`, day 2 always produces the same plan.

---

## 7. M3 — Minigames v2: power and oxygen as production systems

### 7.1 The core change: from "refill a bucket" to "keep the plant running"

Today a solved puzzle calls `RoomController.FillStorage()`, which sets
`amount = max`. It's binary and happens once. Players learn "ignore rooms
until the alarm, then do a chore." Nothing about *how well* you did it
matters.

v2 turns each system into a **producer**:

- **Power:** a `PowerGenerator` feeds power storage at
  `outputPerSecond × efficiency`. The breaker panel's state **is** the
  efficiency. A tripped panel runs at 10%; half-fixed runs at about 55%.
- **Oxygen:** each healthy canister in the manifold feeds O₂. Empty
  canisters feed nothing; a leaking one **drains** the room.
- The **workstation** (income) consumes both. If production falls behind,
  storage drops below `reqAmount`, `CanWork()` fails and income stops.

Maintenance quality now directly sets income. Income pays for upgrades,
upgrades raise consumption, and the AI's efficiency subscore finally
measures something real. That's what "integral and substantial" means in
systems terms.

### 7.2 `Systems/PowerGenerator.cs` (new)

```csharp
using UnityEngine;

public class PowerGenerator : MonoBehaviour
{
    [SerializeField] private Storage targetStorage;
    [SerializeField] private float outputPerSecond = 8f;
    [SerializeField] [Range(0f, 1f)] private float efficiency = 1f;
    [Tooltip("Station is on standby between shifts: no production, no consumption.")]
    [SerializeField] private bool onlyDuringShift = true;

    public float Efficiency
    {
        get => efficiency;
        set => efficiency = Mathf.Clamp01(value);
    }

    public float CurrentOutput => outputPerSecond * efficiency;

    void Update()
    {
        if (targetStorage == null)
            return;
        StationManager sm = StationManager.Instance;
        if (onlyDuringShift && (sm == null || !sm.ShiftInProgress))
            return;

        targetStorage.amount = Mathf.Min(targetStorage.maxAmount, targetStorage.amount + CurrentOutput * Time.deltaTime);
    }
}
```

Gate `GeneralConsumption` on `ShiftInProgress` too, for the same reason.

### 7.3 Breaker panel — `Rooms MiniGames/Electricity/BreakerPanelTask.cs` (new, replaces `FuseBoard`)

**Design:**

| Difficulty | Coupling | Hints | Feels like |
|---|---|---|---|
| 1 | none: a lever flips only itself | lever body is red when wrong | tutorial; match the lamps |
| 2–3 | **neighbours**: a lever also flips left/right | lamps only | a real puzzle; order matters |
| 4–5 | **cross**: also flips up/down (Lights Out) | lamps only | hard; you plan before clicking |

- **Target:** a lamp above each lever (lit = should be ON). At difficulty
  ≥2 the lever colour no longer tells you the answer. The current FuseBoard
  colour-codes every switch, which gives it away.
- **Scramble from solved:** apply *k* distinct presses to a solved board.
  That guarantees it's solvable, and *k* is an upper bound for par. With
  coupling off, par is exact.
- **Partial efficiency:** generator efficiency is `lerp(min, 1, matched%)`,
  so progress pays out *during* the puzzle, not only at the end.
- **Quality** is `par / actions`. The AI quotes it: *"Advisory: WO-03-002
  required 14 actions. Reference procedure: 5."*

```csharp
using System.Collections.Generic;
using UnityEngine;

public class BreakerPanelTask : MonoBehaviour, IShiftTask, IInteractable, IFocusPanel
{
    private enum Coupling { None, Neighbors, Cross }

    [Header("Identity")]
    [SerializeField] private string taskId = "breaker_panel_power";
    [SerializeField] private string displayName = "Reset breaker panel";
    [SerializeField] private string zoneId = "power";
    [SerializeField] private int unlockDay = 1;

    [Header("Grid (authored once, never regenerated)")]
    [SerializeField] private BreakerSwitch switchPrefab;
    [SerializeField] private Transform switchContainer;
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 2;
    [SerializeField] private Vector2 spacing = new Vector2(0.3f, 0.35f);
    [SerializeField] private float forwardOffset = 0.05f;

    [Header("Rules")]
    [SerializeField] private int hintsUpToDifficulty = 1;
    [Range(0f, 1f)] [SerializeField] private float operationalMatch = 0.75f;

    [Header("Output")]
    [SerializeField] private PowerGenerator generator;
    [Range(0f, 1f)] [SerializeField] private float minEfficiency = 0.1f;

    [Header("Focus")]
    [SerializeField] private Transform focusAnchor;

    private BreakerSwitch[] switches;
    private bool[] current;
    private bool[] target;
    private Coupling coupling;
    private bool showHints;
    private bool faulted;
    private int actions;
    private int par;
    private bool reportedOperational;
    private WorkOrder order;

    // ----- IShiftTask -----
    public string TaskId => taskId;
    public string DisplayName => displayName;
    public string ZoneId => zoneId;
    public ShiftTaskCategory Category => ShiftTaskCategory.Power;
    public int UnlockDay => unlockDay;
    public bool IsAssigned => order != null;
    public bool NeedsAttention => faulted && order == null;
    public event System.Action<IShiftTask, TaskReport> Reported;

    // ----- IFocusPanel -----
    public Transform FocusAnchor => focusAnchor;
    public void OnFocusEntered() { }
    public void OnFocusExited() { }

    void Awake()
    {
        BuildGrid();
        for (int i = 0; i < current.Length; i++)
            current[i] = target[i] = true;
        RefreshAll();
    }

    void OnEnable() => ShiftTaskRegistry.Register(this);
    void OnDisable() => ShiftTaskRegistry.Unregister(this);

    public void Assign(WorkOrder newOrder)
    {
        order = newOrder;
        reportedOperational = false;

        int d = Mathf.Clamp(newOrder.difficulty, 1, 5);
        showHints = d <= hintsUpToDifficulty;

        if (!faulted)
        {
            // Fresh trip. A carry-over keeps yesterday's scrambled state and coupling.
            coupling = d >= 4 ? Coupling.Cross : d >= 2 ? Coupling.Neighbors : Coupling.None;
            Scramble(new System.Random(newOrder.seed), presses: 1 + d);
            actions = 0;
            faulted = true;
        }
        ApplyEfficiency();
        RefreshAll();
    }

    public void Revoke()
    {
        order = null;   // the fault (and the lost output) persists overnight
    }

    // ----- IInteractable (the panel housing) -----
    public Vector3 GetInteractionPoint(Vector3 from) => transform.position;

    public bool CanInteract(PlayerInteractor interactor)
    {
        StationManager sm = StationManager.Instance;
        return faulted && !interactor.IsCarrying && sm != null && sm.ShiftInProgress;
    }

    public string GetPrompt(PlayerInteractor interactor) => "Open breaker panel";

    public void Interact(PlayerInteractor interactor)
    {
        if (PanelFocusView.Instance != null)
            PanelFocusView.Instance.Enter(this);
    }

    // ----- Puzzle -----
    public void Press(int index)
    {
        if (!faulted || index < 0 || index >= current.Length)
            return;

        Toggle(index);
        actions++;
        RefreshAll();
        ApplyEfficiency();

        StationManager sm = StationManager.Instance;
        if (sm != null && sm.ShiftInProgress)
            sm.CurrentShift.NotifyPlayerActivity();

        float matched = MatchedFraction();
        if (matched >= 1f)
        {
            Resolve();
        }
        else if (!reportedOperational && matched >= operationalMatch && order != null)
        {
            reportedOperational = true;
            Reported?.Invoke(this, TaskReport.Operational(matched));
        }
    }

    void Resolve()
    {
        faulted = false;
        ApplyEfficiency();
        foreach (BreakerSwitch sw in switches)
            sw.ShowSolved();

        float quality = actions <= par ? 1f : Mathf.Clamp01((float)par / actions);
        if (order != null)
        {
            order = null;
            Reported?.Invoke(this, TaskReport.Completed(quality, actions, par));
        }

        if (PanelFocusView.Instance != null && PanelFocusView.Instance.IsFocused)
            PanelFocusView.Instance.Exit();
    }

    void Scramble(System.Random rng, int presses)
    {
        for (int i = 0; i < target.Length; i++)
        {
            target[i] = rng.NextDouble() > 0.5;
            current[i] = target[i];
        }

        // Distinct presses only: pressing the same lever twice cancels out and would inflate par.
        HashSet<int> pressed = new HashSet<int>();
        int guard = 0;
        while ((pressed.Count < presses || IsSolved()) && pressed.Count < current.Length && guard++ < 200)
        {
            int i = rng.Next(current.Length);
            if (pressed.Add(i))
                Toggle(i);
        }
        par = pressed.Count;   // exact for Coupling.None; an upper bound on optimal otherwise
    }

    void Toggle(int i)
    {
        Flip(i);
        if (coupling == Coupling.None)
            return;

        int r = i / columns, c = i % columns;
        if (c > 0) Flip(i - 1);
        if (c < columns - 1) Flip(i + 1);
        if (coupling == Coupling.Cross)
        {
            if (r > 0) Flip(i - columns);
            if (r < rows - 1) Flip(i + columns);
        }
    }

    void Flip(int i) => current[i] = !current[i];

    bool IsSolved()
    {
        for (int i = 0; i < current.Length; i++)
            if (current[i] != target[i]) return false;
        return true;
    }

    float MatchedFraction()
    {
        int ok = 0;
        for (int i = 0; i < current.Length; i++)
            if (current[i] == target[i]) ok++;
        return (float)ok / current.Length;
    }

    void ApplyEfficiency()
    {
        if (generator != null)
            generator.Efficiency = faulted ? Mathf.Lerp(minEfficiency, 1f, MatchedFraction()) : 1f;
    }

    void RefreshAll()
    {
        for (int i = 0; i < switches.Length; i++)
            switches[i].SetState(current[i], target[i], showHints && faulted);
    }

    void BuildGrid()
    {
        int count = columns * rows;
        switches = new BreakerSwitch[count];
        current = new bool[count];
        target = new bool[count];
        Transform parent = switchContainer != null ? switchContainer : transform;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int i = r * columns + c;
                Vector3 local = new Vector3(
                    (c - (columns - 1) * 0.5f) * spacing.x,
                    (r - (rows - 1) * 0.5f) * spacing.y,
                    forwardOffset);
                BreakerSwitch sw = Instantiate(switchPrefab, transform.TransformPoint(local), transform.rotation, parent);
                sw.name = $"Breaker_R{r}_C{c}";
                sw.Bind(this, i);
                switches[i] = sw;
            }
        }
    }
}
```

**[Probable] Lights Out caveat:** on some grid sizes, some press
combinations cancel out. The `IsSolved()` guard in the scramble loop handles
"scrambled back to solved." Par can overestimate the optimum for coupled
boards, which only makes the AI more lenient. If you want exact par for
coupled boards, solve the GF(2) system (at most 25 unknowns, trivial). It's
a nice interview detail, but not needed now.

### 7.4 `Rooms MiniGames/Electricity/BreakerSwitch.cs` (new, replaces `FuseSwitch`)

```csharp
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakerSwitch : MonoBehaviour, IFocusClickable
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Transform lever;
    [SerializeField] private Renderer targetLamp;            // material must have Emission enabled
    [SerializeField] private float onAngle = 35f;
    [SerializeField] private float offAngle = -35f;
    [SerializeField] private float leverDegreesPerSecond = 540f;

    [Header("Colours")]
    [SerializeField] private Color onColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color offColor = new Color(0.35f, 0.35f, 0.38f);
    [SerializeField] private Color hintWrongColor = new Color(0.9f, 0.25f, 0.2f);
    [SerializeField] private Color solvedColor = new Color(0.3f, 0.9f, 0.4f);
    [SerializeField] private Color lampOnColor = new Color(1f, 0.75f, 0.2f);
    [SerializeField] private Color lampOffColor = new Color(0.15f, 0.12f, 0.1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    // MaterialPropertyBlock: no per-toggle material instances (fixes the FuseSwitch leak).
    // It opts these renderers out of the SRP Batcher — irrelevant at a dozen levers.
    private MaterialPropertyBlock block;
    private BreakerPanelTask panel;
    private int index;
    private float leverTarget;

    public void Bind(BreakerPanelTask owner, int switchIndex)
    {
        panel = owner;
        index = switchIndex;
    }

    public void SetState(bool isOn, bool shouldBeOn, bool showHint)
    {
        leverTarget = isOn ? onAngle : offAngle;
        Paint(bodyRenderer, showHint && isOn != shouldBeOn ? hintWrongColor : isOn ? onColor : offColor, false);
        Paint(targetLamp, shouldBeOn ? lampOnColor : lampOffColor, shouldBeOn);
    }

    public void ShowSolved() => Paint(bodyRenderer, solvedColor, true);

    public void OnFocusClick()
    {
        if (panel != null)
            panel.Press(index);
        // Hook: AudioSource.PlayOneShot(clunk)
    }

    void Update()
    {
        if (lever == null)
            return;
        Quaternion goal = Quaternion.Euler(leverTarget, 0f, 0f);
        lever.localRotation = Quaternion.RotateTowards(lever.localRotation, goal, leverDegreesPerSecond * Time.unscaledDeltaTime);
    }

    void Paint(Renderer r, Color c, bool emissive)
    {
        if (r == null)
            return;
        if (block == null)
            block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(EmissionId, emissive ? c * 2f : Color.black);
        r.SetPropertyBlock(block);
    }
}
```

### 7.5 Oxygen manifold — replaces "throw tanks in a bin"

**I disagree with keeping the disposal-bin game as is.** Collecting N
identical tanks into a box has no decisions in it. It's a physics chore.
Redesign:

- The O₂ room has a **manifold with 4 sockets**. Each healthy canister
  feeds O₂ storage.
- Canisters **run dry naturally** over the shift. That's emergent work: the
  manifold raises `NeedsAttention` and the director issues a maintenance
  order.
- When the director assigns a fault, some canisters are **Empty**, and at
  difficulty ≥3 one is **Leaking**. A leaking canister drains the room
  until removed, so you have to prioritize.
- The loop: remove the bad canister → carry it to the **recycler** → take a
  full one from the **supply rack** (limited stock per shift, which the AI
  rations later) → insert it. You're slowed while carrying, and the
  contamination timer is still running.
- The AI notices mistakes. Inserting an Empty canister wastes a socket
  action; recycling a Full one counts as waste.

The same components will do filter swaps later, with `itemType = "filter"`.

**`Rooms MiniGames/Oxygen/OxygenCanister.cs` (new):**

```csharp
using UnityEngine;

public enum CanisterState { Full, Empty, Leaking }

public class OxygenCanister : CarryableItem
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private ParticleSystem leakParticles;
    [SerializeField] private float capacity = 1200f;
    [SerializeField] private Color fullColor = new Color(0.3f, 0.7f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.4f, 0.4f, 0.42f);
    [SerializeField] private Color leakingColor = new Color(1f, 0.45f, 0.2f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private float remaining;
    private CanisterState state;

    public CanisterState State => state;
    public float Fill01 => capacity > 0f ? remaining / capacity : 0f;
    public override string DisplayName => $"{state.ToString().ToLowerInvariant()} canister";

    protected override void Awake()
    {
        base.Awake();
        Refill();
    }

    public void Refill() { remaining = capacity; SetState(CanisterState.Full); }
    public void MarkEmpty() { remaining = 0f; SetState(CanisterState.Empty); }
    public void MarkLeaking() { remaining = Mathf.Max(remaining, capacity * 0.5f); SetState(CanisterState.Leaking); }

    /// <summary>Removes up to amount; flips to Empty when dry. Returns what was actually drawn.</summary>
    public float Draw(float amount)
    {
        float drawn = Mathf.Min(remaining, Mathf.Max(0f, amount));
        remaining -= drawn;
        if (remaining <= 0f && state != CanisterState.Empty)
            SetState(CanisterState.Empty);
        return drawn;
    }

    void SetState(CanisterState newState)
    {
        state = newState;
        if (leakParticles != null)
        {
            if (state == CanisterState.Leaking) leakParticles.Play();
            else leakParticles.Stop();
        }
        if (bodyRenderer == null)
            return;
        if (block == null) block = new MaterialPropertyBlock();
        bodyRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, state == CanisterState.Full ? fullColor : state == CanisterState.Empty ? emptyColor : leakingColor);
        bodyRenderer.SetPropertyBlock(block);
    }
}
```

**`Rooms MiniGames/Oxygen/CanisterDispenser.cs` + `CanisterRecycler.cs` (new):**

```csharp
using UnityEngine;

public class CanisterDispenser : MonoBehaviour, IInteractable
{
    [SerializeField] private OxygenCanister canisterPrefab;
    [SerializeField] private int stockPerShift = 6;
    [SerializeField] private TMPro.TextMeshPro stockLabel;

    private int stock;

    void OnEnable() { GameEvents.OnShiftStarted += Restock; Restock(); }
    void OnDisable() => GameEvents.OnShiftStarted -= Restock;

    void Restock()
    {
        stock = stockPerShift;
        if (stockLabel != null) stockLabel.text = $"STOCK {stock:00}";
    }

    public Vector3 GetInteractionPoint(Vector3 from) => transform.position;
    public bool CanInteract(PlayerInteractor interactor) => stock > 0 && !interactor.IsCarrying;
    public string GetPrompt(PlayerInteractor interactor) => $"Take full canister ({stock} left)";

    public void Interact(PlayerInteractor interactor)
    {
        stock--;
        if (stockLabel != null) stockLabel.text = $"STOCK {stock:00}";
        OxygenCanister c = Instantiate(canisterPrefab, transform.position, transform.rotation);
        interactor.BeginCarry(c);
    }
}

public class CanisterRecycler : MonoBehaviour, IInteractable
{
    public Vector3 GetInteractionPoint(Vector3 from) => transform.position;
    public bool CanInteract(PlayerInteractor interactor) => interactor.Carried is OxygenCanister;
    public string GetPrompt(PlayerInteractor interactor) => $"Recycle {interactor.Carried.DisplayName}";

    public void Interact(PlayerInteractor interactor)
    {
        if (!(interactor.ReleaseCarried() is OxygenCanister canister))
            return;

        StationManager sm = StationManager.Instance;
        if (canister.State == CanisterState.Full && sm != null && sm.ShiftInProgress)
            sm.CurrentShift.RecordWaste();   // the AI notices
        Destroy(canister.gameObject);
    }
}
```

**`Rooms MiniGames/Oxygen/OxygenManifoldTask.cs` (new, replaces `OxygenPuzzle`):**

```csharp
using UnityEngine;

public class OxygenManifoldTask : MonoBehaviour, IShiftTask
{
    [Header("Identity")]
    [SerializeField] private string taskId = "o2_manifold";
    [SerializeField] private string displayName = "Service O2 manifold";
    [SerializeField] private string zoneId = "oxygen";
    [SerializeField] private int unlockDay = 1;

    [Header("Hardware")]
    [SerializeField] private ItemSocket[] sockets;
    [SerializeField] private OxygenCanister canisterPrefab;
    [SerializeField] private Storage oxygenStorage;

    [Header("Flow")]
    [SerializeField] private float flowPerCanister = 1.5f;
    [SerializeField] private float leakDrainPerSecond = 4f;
    [SerializeField] private float leakBurnMultiplier = 3f;
    [SerializeField] private bool onlyDuringShift = true;

    [Header("Scoring")]
    [SerializeField] private float parSecondsPerSwap = 30f;
    [Range(0f, 1f)] [SerializeField] private float operationalHealthy = 0.75f;

    private WorkOrder order;
    private float assignedAt;
    private int actions;
    private int swapsNeeded;
    private bool reportedOperational;

    public string TaskId => taskId;
    public string DisplayName => displayName;
    public string ZoneId => zoneId;
    public ShiftTaskCategory Category => ShiftTaskCategory.Oxygen;
    public int UnlockDay => unlockDay;
    public bool IsAssigned => order != null;
    public bool NeedsAttention => order == null && CountHealthy() < sockets.Length;
    public event System.Action<IShiftTask, TaskReport> Reported;

    float Clock => StationManager.Instance != null ? StationManager.Instance.ShiftElapsedSeconds : 0f;

    void Awake()
    {
        // Seat in Awake so NeedsAttention is correct before DayCycle plans day 1.
        foreach (ItemSocket s in sockets)
        {
            if (s.IsEmpty)
                s.Seat(Instantiate(canisterPrefab));
            s.Inserted += OnSocketChanged;
            s.Removed += OnSocketChanged;
        }
    }

    void OnEnable() => ShiftTaskRegistry.Register(this);
    void OnDisable() => ShiftTaskRegistry.Unregister(this);

    void Update()
    {
        StationManager sm = StationManager.Instance;
        if (onlyDuringShift && (sm == null || !sm.ShiftInProgress))
            return;
        if (oxygenStorage == null)
            return;

        float dt = Time.deltaTime;
        foreach (ItemSocket s in sockets)
        {
            if (!(s.Seated is OxygenCanister c))
                continue;
            if (c.State == CanisterState.Full)
            {
                oxygenStorage.amount = Mathf.Min(oxygenStorage.maxAmount, oxygenStorage.amount + c.Draw(flowPerCanister * dt));
            }
            else if (c.State == CanisterState.Leaking)
            {
                c.Draw(flowPerCanister * leakBurnMultiplier * dt);
                oxygenStorage.amount = Mathf.Max(0f, oxygenStorage.amount - leakDrainPerSecond * dt);
            }
        }
    }

    public void Assign(WorkOrder newOrder)
    {
        order = newOrder;
        assignedAt = Clock;
        actions = 0;
        reportedOperational = false;

        System.Random rng = new System.Random(newOrder.seed);
        int faultsWanted = Mathf.Clamp(newOrder.difficulty, 1, sockets.Length);
        int faults = sockets.Length - CountHealthy();          // canisters that already ran dry count
        bool leakPlaced = newOrder.difficulty < 3;              // only difficulty 3+ gets a leak
        int start = rng.Next(sockets.Length);

        for (int k = 0; k < sockets.Length && faults < faultsWanted; k++)
        {
            ItemSocket s = sockets[(start + k) % sockets.Length];
            if (!(s.Seated is OxygenCanister c) || c.State != CanisterState.Full)
                continue;
            if (!leakPlaced) { c.MarkLeaking(); leakPlaced = true; }
            else c.MarkEmpty();
            faults++;
        }
        swapsNeeded = Mathf.Max(1, sockets.Length - CountHealthy());
    }

    public void Revoke() => order = null;   // empties/leaks persist overnight

    void OnSocketChanged(ItemSocket socket, CarryableItem item)
    {
        if (order == null)
            return;
        actions++;

        StationManager sm = StationManager.Instance;
        if (sm != null && sm.ShiftInProgress)
            sm.CurrentShift.NotifyPlayerActivity();

        int healthy = CountHealthy();
        float healthyFraction = (float)healthy / sockets.Length;

        if (healthy == sockets.Length)
        {
            float elapsed = Mathf.Max(1f, Clock - assignedAt);
            float parTime = swapsNeeded * parSecondsPerSwap;
            float quality = Mathf.Clamp01(parTime / elapsed);
            order = null;
            Reported?.Invoke(this, TaskReport.Completed(quality, actions, swapsNeeded * 2));
        }
        else if (!reportedOperational && healthyFraction >= operationalHealthy)
        {
            reportedOperational = true;
            Reported?.Invoke(this, TaskReport.Operational(healthyFraction));
        }
    }

    int CountHealthy()
    {
        int n = 0;
        foreach (ItemSocket s in sockets)
            if (s.Seated is OxygenCanister c && c.State == CanisterState.Full) n++;
        return n;
    }
}
```

### 7.6 Economy retune — `WorkStation.cs`

Split "how much is needed to run" (`Storage.reqAmount`) from "how much you
burn" (currently the same field), fix the runaway income, and **record
consumption** so the efficiency subscore means something (B12, B13).

```csharp
[Header("Economy")]
[SerializeField] private float basePoints = 3f;
[SerializeField] private float pointsGrowth = 1.6f;         // income ×1.6 per level
[SerializeField] private float basePowerPerSecond = 6f;
[SerializeField] private float baseOxygenPerSecond = 4f;
[SerializeField] private float consumptionGrowth = 1.35f;   // burn ×1.35 per level → upgrades pay, but need better maintenance

public float AddPoints => basePoints * Mathf.Pow(pointsGrowth, level - 1);
public float UpgradePerc => pointsGrowth;                   // keeps StationManager's store label working
float Burn(float basePerSecond) => basePerSecond * Mathf.Pow(consumptionGrowth, level - 1);

public void UpgradeWorkStation() => level++;
public void RestoreLevel(int savedLevel) => level = Mathf.Max(1, savedLevel);

public void Work()   // called once per second by StationManager's InvokeRepeating
{
    StationManager sm = StationManager.Instance;
    float power = Burn(basePowerPerSecond);
    float oxygen = Burn(baseOxygenPerSecond);
    if (sm.PowerStorage != null) sm.PowerStorage.amount -= power;
    if (sm.OxygenStorage != null) sm.OxygenStorage.amount -= oxygen;
    if (sm.ShiftInProgress) sm.CurrentShift.RecordResourcesConsumed(power, oxygen);
    sm.AddPoints(AddPoints);
}
```

> Remove `addPoints` / `upgradePerc` fields, and fix `StationManager`'s
> upgrade calls to **pay first, then upgrade**
> (`points -= workStation.UpgradeCost; workStation.UpgradeWorkStation();`),
> so correctness no longer depends on `Update` order (B15). Check that
> `sm.PowerStorage` / `OxygenStorage` are the same `Storage` objects the rooms
> use **[Probable, verify in scene]**. The old `Work()` iterated
> `Rooms[].myTank`.

### 7.7 Migration from v1 → v2 (don't big-bang it)

1. Build v2 alongside v1. Put `BreakerPanelTask` on a **new** panel object in
   the power room and `OxygenManifoldTask` + 4 sockets + rack + recycler in
   the O₂ room.
2. Disable the `FuseBoard` / `OxygenPuzzle` GameObjects (don't delete yet).
3. Play three days. When v2 is at parity, delete: `FuseBoard.cs`,
   `FuseSwitch.cs`, `OxygenPuzzle.cs` (+ `DisposalZoneTrigger`),
   `OxygenTank.cs`, `PlayerInteraction.cs`, and the prefabs. Search
   `ClickToMove` for leftover `OxygenTank`/`FuseSwitch` references.
4. `RoomController` stays: contamination timer, alert light, UI.
   `FillStorage()` becomes unused. Remove it in the cleanup commit.

### 7.8 M3 acceptance

- Trip the panel mid-shift: power-room lights flicker (M4), power storage
  starts falling, and the workstation stops once below `reqAmount`.
  Half-fixing slows the fall.
- Let a canister run dry naturally: within a second a "Service O₂ manifold"
  order appears on the board.
- Recycling a full canister produces an "Advisory: 1 serviceable unit(s)
  disposed." line.
- Workstation level 4 income is in the tens of $/s, not hundreds.

---

## 8. M4 — World feedback: seeing progress and decay

**Principle:** every number the AI grades should have a physical echo in the
room. The player should be able to guess their score by looking around
before the report types out.

| Signal | What the player sees | Driven by |
|---|---|---|
| Zone condition (0–1) | grime decals fade in, lights flicker, sparks/steam | `StationCondition` ← completed/expired orders, contamination, overnight decay |
| Power health | power-room lights dim/flicker immediately on a trip | `PowerGenerator.Efficiency` |
| Storage levels | physical gauge columns on the tanks, colour ramp | `Storage.amount` |
| AI strictness | **one more surveillance camera appears each few days**; cameras track you and go solid red when you idle | `ProgressionUnlock` + `SurveillanceCamera` |
| Career | wall board: DAY 07 · STANDARDS: HIGH · LAST REVIEW · DAYS WITHOUT INCIDENT · NOTICES 1/3 | `DayBoard` ← `CareerState` |
| History | bar chart of the last 10 days' scores on the terminal | `PerformanceHistoryUI` |
| Upgrades | workstation/storage models swap per level (authored variants) | `ProgressionUnlock` keyed to levels (trivial extension) |
| Unlocks | taped-off corridors open on day N as new task types come online | `ProgressionUnlock` |

The camera count is the strongest single visual. The difficulty curve
becomes something you can *see*, and it's on theme.

### 8.1 `World/StationCondition.cs` (new)

```csharp
using System.Collections.Generic;
using UnityEngine;

public class StationCondition : Singleton<StationCondition>
{
    [System.Serializable]
    public class Zone
    {
        public string zoneId = "hub";
        [Range(0f, 1f)] public float condition = 1f;
    }

    [SerializeField] private List<Zone> zones = new List<Zone>
    {
        new Zone { zoneId = "hub" }, new Zone { zoneId = "power" }, new Zone { zoneId = "oxygen" },
    };
    [SerializeField] private ShiftDirector director;
    [SerializeField] private float completedRestore = 0.15f;
    [SerializeField] private float expiredPenalty = 0.20f;
    [SerializeField] private float contaminationPenalty = 0.05f;

    public event System.Action<string, float> ConditionChanged;

    void OnEnable()
    {
        if (director != null) director.OrderUpdated += OnOrderUpdated;
        GameEvents.OnShiftEnded += OnShiftEnded;
    }

    void OnDisable()
    {
        if (director != null) director.OrderUpdated -= OnOrderUpdated;
        GameEvents.OnShiftEnded -= OnShiftEnded;
    }

    public float Get(string zoneId)
    {
        Zone z = Find(zoneId);
        return z != null ? z.condition : 1f;
    }

    public void Adjust(string zoneId, float delta)
    {
        Zone z = Find(zoneId);
        if (z == null)
            return;
        z.condition = Mathf.Clamp01(z.condition + delta);
        ConditionChanged?.Invoke(z.zoneId, z.condition);
    }

    public void DecayAll(float amount)
    {
        foreach (Zone z in zones)
            Adjust(z.zoneId, -amount);
    }

    public List<Zone> Capture()
    {
        List<Zone> copy = new List<Zone>(zones.Count);
        foreach (Zone z in zones)
            copy.Add(new Zone { zoneId = z.zoneId, condition = z.condition });
        return copy;
    }

    public void Restore(List<Zone> saved)
    {
        foreach (Zone s in saved)
        {
            Zone z = Find(s.zoneId);
            if (z != null) z.condition = s.condition;
        }
    }

    void OnOrderUpdated(WorkOrder o)
    {
        if (o.state == WorkOrderState.Completed)
            Adjust(o.zoneId, completedRestore * (0.5f + 0.5f * o.quality));
    }

    void OnShiftEnded()
    {
        if (director != null)
        {
            foreach (WorkOrder o in director.Orders)
                if (o.state == WorkOrderState.Expired || o.state == WorkOrderState.Failed)
                    Adjust(o.zoneId, -expiredPenalty);
        }

        StationManager sm = StationManager.Instance;
        if (sm != null && sm.CurrentShift != null && sm.CurrentShift.contaminationEvents > 0)
            Adjust("hub", -contaminationPenalty * sm.CurrentShift.contaminationEvents);
    }

    Zone Find(string zoneId)
    {
        foreach (Zone z in zones)
            if (z.zoneId == zoneId) return z;
        return null;
    }
}
```

### 8.2 Condition-driven visuals — `World/ConditionVisuals.cs` (new)

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class ConditionDrivenVisual : MonoBehaviour
{
    [SerializeField] protected string zoneId = "hub";
    [Tooltip("At or above this condition the zone looks pristine; the effect ramps in below it.")]
    [SerializeField] [Range(0f, 1f)] protected float cleanAbove = 0.8f;

    /// <summary>0 = pristine, 1 = worst.</summary>
    protected float Wear { get; private set; }

    protected virtual void Update()
    {
        StationCondition sc = StationCondition.Instance;
        float condition = sc != null ? sc.Get(zoneId) : 1f;
        Wear = Mathf.Clamp01(Mathf.InverseLerp(cleanAbove, 0f, condition));
        Apply(Wear);
    }

    protected abstract void Apply(float wear);
}

/// <summary>Flickers with zone wear AND with a power generator's lost efficiency (instant trip feedback).</summary>
[RequireComponent(typeof(Light))]
public class ConditionFlickerLight : ConditionDrivenVisual
{
    [SerializeField] private PowerGenerator powerSource;
    [SerializeField] private float flickerSpeed = 9f;
    [SerializeField] [Range(0f, 1f)] private float dropoutIntensity = 0.05f;

    private Light lamp;
    private float baseIntensity;
    private float seed;

    void Awake()
    {
        lamp = GetComponent<Light>();
        baseIntensity = lamp.intensity;
        seed = Random.value * 100f;
    }

    protected override void Apply(float wear)
    {
        float powerLoss = powerSource != null ? 1f - powerSource.Efficiency : 0f;
        float severity = Mathf.Max(wear, powerLoss);
        if (severity <= 0.001f)
        {
            lamp.intensity = baseIntensity;
            return;
        }

        float n = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        float factor = n < severity * 0.35f ? dropoutIntensity : 1f - severity * 0.5f * n;
        lamp.intensity = baseIntensity * factor;
    }
}

public class ConditionParticles : ConditionDrivenVisual
{
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private float maxRate = 20f;

    protected override void Apply(float wear)
    {
        if (particles == null)
            return;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = wear * maxRate;
        if (wear > 0.01f && !particles.isPlaying) particles.Play();
        else if (wear <= 0.01f && particles.isPlaying) particles.Stop();
    }
}

/// <summary>Needs the URP Decal renderer feature enabled on the active renderer asset.</summary>
public class ConditionGrime : ConditionDrivenVisual
{
    [SerializeField] private DecalProjector[] decals;

    protected override void Apply(float wear)
    {
        for (int i = 0; i < decals.Length; i++)
        {
            if (decals[i] != null)
                decals[i].fadeFactor = wear;
        }
    }
}
```

### 8.3 Progression props — `World/ProgressionUnlock.cs`, `World/SurveillanceCamera.cs` (new)

```csharp
using UnityEngine;

/// <summary>
/// Shows its targets only within a day range / above a strictness. Use for extra
/// surveillance cameras, new posters, opened corridors, upgraded models.
/// Don't list this component's own GameObject in targets (it would stop listening).
/// </summary>
public class ProgressionUnlock : MonoBehaviour
{
    [SerializeField] private int fromDay = 1;
    [Tooltip("0 = forever.")]
    [SerializeField] private int untilDay = 0;
    [SerializeField] private float minStrictness = 0f;
    [SerializeField] private GameObject[] targets;

    void OnEnable() => GameEvents.OnDayStarted += Refresh;
    void OnDisable() => GameEvents.OnDayStarted -= Refresh;

    void Start()
    {
        StationManager sm = StationManager.Instance;
        Refresh(sm != null ? sm.CurrentDay : 1);
    }

    void Refresh(int day)
    {
        StationManager sm = StationManager.Instance;
        float strictness = sm != null && sm.AIManager != null ? sm.AIManager.StrictnessLevel : 1f;
        bool on = day >= fromDay && (untilDay <= 0 || day <= untilDay) && strictness >= minStrictness;
        foreach (GameObject t in targets)
            if (t != null) t.SetActive(on);
    }
}

/// <summary>
/// Wall camera that turns to follow the operator. Its LED blinks normally and goes
/// solid when the operator has been idle — the AI noticed.
/// Model convention: pivot's forward (+Z) is the lens direction at rest.
/// </summary>
public class SurveillanceCamera : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private Light statusLed;
    [SerializeField] private float range = 12f;
    [SerializeField] private float maxYaw = 70f;
    [SerializeField] private Vector2 pitchLimits = new Vector2(-10f, 60f);
    [SerializeField] private float trackDegreesPerSecond = 90f;
    [SerializeField] private float idleStareAfter = 4f;

    private Transform target;
    private Quaternion restLocal;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
        if (pivot != null) restLocal = pivot.localRotation;
    }

    void LateUpdate()
    {
        if (pivot == null)
            return;

        Quaternion desired = restLocal;
        if (target != null && (target.position - pivot.position).sqrMagnitude <= range * range)
        {
            Vector3 worldDir = (target.position + Vector3.up) - pivot.position;
            Vector3 localDir = pivot.parent != null ? pivot.parent.InverseTransformDirection(worldDir) : worldDir;
            Vector3 e = Quaternion.LookRotation(localDir, Vector3.up).eulerAngles;
            float yaw = Mathf.Clamp(Mathf.DeltaAngle(0f, e.y), -maxYaw, maxYaw);
            float pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, e.x), pitchLimits.x, pitchLimits.y);
            desired = Quaternion.Euler(pitch, yaw, 0f);
        }
        pivot.localRotation = Quaternion.RotateTowards(pivot.localRotation, desired, trackDegreesPerSecond * Time.deltaTime);

        if (statusLed != null)
        {
            StationManager sm = StationManager.Instance;
            bool staring = sm != null && sm.ShiftInProgress && sm.CurrentShift.SecondsSinceActivity > idleStareAfter;
            statusLed.enabled = staring || Mathf.Repeat(Time.time, 1.2f) < 0.12f;
        }
    }
}
```

Suggested camera rollout **[Guess]**: day 1: 1 (hub); day 2: +1 (power);
day 4: +1 (oxygen); day 6: +1 (corridor); day 9: +2 (a "you are being
evaluated" wall bank).

### 8.4 Readouts — `World/WorldResourceGauge.cs`, `World/DayBoard.cs`, `UI/PerformanceHistoryUI.cs` (new)

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldResourceGauge : MonoBehaviour
{
    [SerializeField] private Storage storage;
    [Tooltip("Pivot at the bottom; scaled on Y.")]
    [SerializeField] private Transform fill;
    [SerializeField] private Renderer fillRenderer;
    [SerializeField] private Gradient colorByFill;
    [SerializeField] private float smoothing = 4f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private Vector3 baseScale;
    private float shown;

    void Awake()
    {
        if (fill != null) baseScale = fill.localScale;
    }

    void Update()
    {
        if (storage == null || fill == null)
            return;
        float target = storage.maxAmount > 0f ? Mathf.Clamp01(storage.amount / storage.maxAmount) : 0f;
        shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        fill.localScale = new Vector3(baseScale.x, baseScale.y * Mathf.Max(0.001f, shown), baseScale.z);

        if (fillRenderer != null && colorByFill != null)
        {
            if (block == null) block = new MaterialPropertyBlock();
            fillRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, colorByFill.Evaluate(shown));
            fillRenderer.SetPropertyBlock(block);
        }
    }
}

public class DayBoard : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;   // world-space TMP, not UGUI
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private ShiftPolicy policy;

    void OnEnable() => GameEvents.OnDayStarted += Refresh;
    void OnDisable() => GameEvents.OnDayStarted -= Refresh;

    void Refresh(int day)
    {
        if (text == null || dayCycle == null || dayCycle.Career == null)
            return;

        CareerState c = dayCycle.Career;
        StationManager sm = StationManager.Instance;
        string standards = sm != null && sm.AIManager != null ? sm.AIManager.StandardsLabel : "Standard";
        string lastReview = c.history.Count > 0 ? c.history[c.history.Count - 1].classification : "—";

        int sinceIncident = 0;
        for (int i = c.history.Count - 1; i >= 0 && c.history[i].incidents == 0; i--)
            sinceIncident++;

        text.text =
            $"DAY {day:00}\n" +
            $"STANDARDS: {standards.ToUpperInvariant()}\n" +
            $"LAST REVIEW: {lastReview}\n" +
            $"DAYS WITHOUT INCIDENT: {sinceIncident}\n" +
            $"NOTICES: {c.notices}/{(policy != null ? policy.noticesToTerminate : 3)}";
    }
}

/// <summary>Last N days as bars on the terminal. Container needs a HorizontalLayoutGroup (no child height control).</summary>
public class PerformanceHistoryUI : MonoBehaviour
{
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private RectTransform barContainer;
    [SerializeField] private Image barPrefab;
    [SerializeField] private int maxBars = 10;
    [SerializeField] private Gradient colorByScore;

    void OnEnable() => GameEvents.OnDayStarted += Rebuild;
    void OnDisable() => GameEvents.OnDayStarted -= Rebuild;

    void Rebuild(int _)
    {
        if (dayCycle == null || dayCycle.Career == null || barContainer == null || barPrefab == null)
            return;

        for (int i = barContainer.childCount - 1; i >= 0; i--)
            Destroy(barContainer.GetChild(i).gameObject);

        var history = dayCycle.Career.history;
        float height = barContainer.rect.height;
        for (int i = Mathf.Max(0, history.Count - maxBars); i < history.Count; i++)
        {
            Image bar = Instantiate(barPrefab, barContainer);
            bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(2f, history[i].score * height));
            if (colorByScore != null) bar.color = colorByScore.Evaluate(history[i].score);
        }
    }
}
```

### 8.5 Cheap juice (no new systems)

- **Order stamps:** when an order resolves, play a stamp SFX and flash the
  board line. Use a red "EXPIRED" stamp at end of shift.
- **Evaluation as CCTV:** render the evaluation panel over a
  scanline/vignette overlay, showing a still from `worldCamera` taken on
  `EndShift`. Adds a lot of mood for little code.
- **Contamination vignette:** screen-edge tint while `RoomController`'s
  timer is under 10 s. You already flash the timer text; mirror it on the
  screen.
- **Speaker-voice lines:** 10–20 one-line AI announcements triggered by
  events. "Critical order issued." "Operator idle: 4 seconds." "Allocation
  adjusted." Text-only is fine; TTS is a later option.

---

## 9. M5 — New task catalogue

Ranked by **value to the headline ÷ cost**. Everything reuses the M1
primitives.

| # | Task | Primitive | Category | Cost | Why it earns its place |
|---|---|---|---|---|---|
| 1 | **Inspection round** — walk to N gauges, log each as nominal or report an anomaly | Use + Secondary | Inspection | S | The best thematic fit: **judgment under surveillance**. Reporting an anomaly *creates more work for you* (a follow-up order). Logging a bad reading as nominal is faster, but the AI audits you afterwards. |
| 2 | **Spill / biofilm cleanup** with a visibly shrinking decal | Hold | Sanitation | S | Makes `CleaningTask` visual; persists overnight if unfinished |
| 3 | **Filter swap** (vent filter housing) | Carry + Socket | Maintenance | S | Copy of the manifold with `itemType = "filter"`. Generalize into `SocketServiceTask` when this lands (rule of two) |
| 4 | **Workstation quota** — the AI sets $X for the day | none (reads `moneyEarned`) | — | XS | Ties the income loop to the briefing and evaluation explicitly |
| 5 | **Pressure valve balancing** — a dial drifts; keep it in band, scored as % time in band | Panel | Maintenance | M | The only *continuous* task: it competes for your attention with everything else |
| 6 | **Seal breach** (incident) — fetch a patch kit, hold to weld; O₂ drains fast until done | Carry + Hold | Oxygen | M | A composition that proves the primitive story; a good Critical order |
| 7 | **Policy memos** — the AI issues a rule for the day ("No sprinting in corridors", "Log all readings within 60 s"); violations count as safety events | Terminal | — | M–L | **Standards rising, made mechanical.** Each memo is a `PolicyRule` asset with a violation check. This is the thing I'd build *after* the slice ships |
| 8 | **Parts delivery** — carry a crate from storage to a machine | Carry + Socket | Maintenance | S | Filler; good agent-hook candidate later |

### 9.1 Inspection round — full code (build this one first)

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InspectionGauge : MonoBehaviour, IInteractable, ISecondaryInteractable
{
    [SerializeField] private string gaugeLabel = "P-03";
    [SerializeField] private TextMeshPro readout;
    [SerializeField] private GameObject pendingMarker;          // floating chevron while on today's round
    [SerializeField] private Vector2 nominalRange = new Vector2(40f, 60f);
    [SerializeField] private Vector2 anomalyRange = new Vector2(78f, 95f);
    [SerializeField] private string unit = "kPa";

    private InspectionRoundTask round;
    private bool armed;
    private bool anomalous;
    private float reading;

    public string Label => gaugeLabel;
    public bool IsAnomalous => anomalous;

    void Awake() => Disarm();

    public void Arm(InspectionRoundTask owner, bool isAnomalous, System.Random rng)
    {
        round = owner;
        armed = true;
        anomalous = isAnomalous;
        Vector2 band = isAnomalous ? anomalyRange : nominalRange;
        reading = Mathf.Lerp(band.x, band.y, (float)rng.NextDouble());
        if (pendingMarker != null) pendingMarker.SetActive(true);
    }

    public void Disarm()
    {
        armed = false;
        if (pendingMarker != null) pendingMarker.SetActive(false);
    }

    void Update()
    {
        if (readout == null)
            return;
        // Tiny live jitter so it reads as an instrument, not a label. The band is printed; colour is NOT a hint.
        float shown = reading + Mathf.Sin(Time.time * 3.1f + gaugeLabel.GetHashCode()) * 0.4f;
        readout.text = $"{gaugeLabel}\n{shown:0.0} {unit}\nNOM {nominalRange.x:0}–{nominalRange.y:0}";
    }

    public Vector3 GetInteractionPoint(Vector3 from) => transform.position;
    public bool CanInteract(PlayerInteractor interactor) => armed && !interactor.IsCarrying;
    public string GetPrompt(PlayerInteractor interactor) => $"Log {gaugeLabel} nominal";
    public void Interact(PlayerInteractor interactor) => Resolve(false);
    public string GetSecondaryPrompt(PlayerInteractor interactor) => $"Report {gaugeLabel} anomaly";
    public void InteractSecondary(PlayerInteractor interactor) => Resolve(true);

    void Resolve(bool reportedAnomaly)
    {
        if (!armed)
            return;
        Disarm();
        if (round != null)
            round.OnGaugeLogged(this, reportedAnomaly);
    }
}

public class InspectionRoundTask : MonoBehaviour, IShiftTask
{
    [SerializeField] private string taskId = "inspection_round_hub";
    [SerializeField] private string displayName = "Inspection round";
    [SerializeField] private string zoneId = "hub";
    [SerializeField] private int unlockDay = 2;
    [SerializeField] private InspectionGauge[] gauges;
    [SerializeField] private int baseGaugeCount = 2;
    [SerializeField] [Range(0f, 1f)] private float anomalyChancePerDifficulty = 0.12f;

    [Tooltip("Must implement IShiftTask. Issued as a Priority order when an anomaly is reported. Honesty creates work.")]
    [SerializeField] private MonoBehaviour followUpTask;
    [SerializeField] private ShiftDirector director;

    private readonly List<string> discrepancies = new List<string>();
    private WorkOrder order;
    private int armedCount;
    private int logged;
    private int correct;

    public string TaskId => taskId;
    public string DisplayName => displayName;
    public string ZoneId => zoneId;
    public ShiftTaskCategory Category => ShiftTaskCategory.Inspection;
    public int UnlockDay => unlockDay;
    public bool IsAssigned => order != null;
    public bool NeedsAttention => false;
    public event System.Action<IShiftTask, TaskReport> Reported;

    void OnEnable() => ShiftTaskRegistry.Register(this);
    void OnDisable() => ShiftTaskRegistry.Unregister(this);

    public void Assign(WorkOrder newOrder)
    {
        order = newOrder;
        logged = correct = 0;
        discrepancies.Clear();

        System.Random rng = new System.Random(newOrder.seed);
        InspectionGauge[] shuffled = (InspectionGauge[])gauges.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        armedCount = Mathf.Clamp(baseGaugeCount + newOrder.difficulty - 1, 1, shuffled.Length);
        float anomalyChance = anomalyChancePerDifficulty * newOrder.difficulty;
        for (int i = 0; i < armedCount; i++)
            shuffled[i].Arm(this, rng.NextDouble() < anomalyChance, rng);
    }

    public void Revoke()
    {
        order = null;
        foreach (InspectionGauge g in gauges)
            g.Disarm();
    }

    public void OnGaugeLogged(InspectionGauge gauge, bool reportedAnomaly)
    {
        if (order == null)
            return;

        logged++;
        if (reportedAnomaly == gauge.IsAnomalous)
            correct++;
        else
            discrepancies.Add(gauge.IsAnomalous ? $"{gauge.Label} logged nominal; archive shows deviation" : $"{gauge.Label} false alarm");

        if (reportedAnomaly && gauge.IsAnomalous && director != null && followUpTask is IShiftTask followUp)
            director.IssueFollowUp(followUp, WorkOrderPriority.Priority);

        StationManager sm = StationManager.Instance;
        if (sm != null && sm.ShiftInProgress)
            sm.CurrentShift.NotifyPlayerActivity();

        if (logged >= armedCount)
        {
            float quality = (float)correct / armedCount;
            string note = discrepancies.Count > 0 ? string.Join("; ", discrepancies) + "." : null;
            order = null;
            Reported?.Invoke(this, TaskReport.Completed(quality, logged, armedCount, note));
        }
    }
}
```

### 9.2 Spill cleanup visual — `Tasks/SpillCleaningTask.cs` (new)

```csharp
using UnityEngine;

/// <summary>CleaningTask whose spill visibly shrinks with progress. Pair with ManualShiftTask; set resetProgressOnExit = false.</summary>
public class SpillCleaningTask : CleaningTask
{
    [SerializeField] private Transform spillVisual;
    [SerializeField] [Range(0f, 1f)] private float minScale = 0.05f;

    private Vector3 fullScale = Vector3.one;

    protected override void Awake()
    {
        base.Awake();
        if (spillVisual != null)
            fullScale = spillVisual.localScale;
    }

    protected override void OnProgressChanged(float normalizedProgress)
    {
        base.OnProgressChanged(normalizedProgress);
        if (spillVisual == null)
            return;
        spillVisual.localScale = fullScale * Mathf.Lerp(1f, minScale, normalizedProgress);
        if (normalizedProgress >= 1f)
            spillVisual.gameObject.SetActive(false);
    }

    public override void ResetForNewShift()
    {
        base.ResetForNewShift();
        if (spillVisual != null)
        {
            spillVisual.localScale = fullScale;
            spillVisual.gameObject.SetActive(true);
        }
    }
}
```

### 9.3 Policy memos — interface sketch only (post-slice)

```csharp
public abstract class PolicyRule : ScriptableObject
{
    [TextArea] public string memoText;              // "Sprinting in corridors is prohibited."
    public int fromDay = 3;
    public abstract void Begin();                    // subscribe to what you watch
    public abstract void End();
    public abstract int Violations { get; }          // → ShiftMetrics.contaminationEvents-style safety hits
}
// e.g. NoSprintRule polls PlayerMovement.IsSprinting inside zones tagged "Corridor".
```

---

## 10. M6 — Tests, docs, hygiene

### 10.1 Put scoring under test (a strong portfolio signal)

Pull the pure math out of `AIManager` into a static class, so tests don't
need a scene:

```csharp
public static class ScoreMath
{
    public static float AdjustedThreshold(float baseThreshold, float strictness) =>
        1f - (1f - baseThreshold) / Mathf.Max(1f, strictness);
}
```

`Assets/Editor/Tests/ScoreMathTests.cs` **[Probable]**: with
`com.unity.test-framework` installed, EditMode tests in the predefined
editor assembly can see `Assembly-CSharp` without an asmdef. If Test Runner
doesn't pick them up, give gameplay code an asmdef. You'll want one
eventually anyway.

```csharp
using System.Collections.Generic;
using NUnit.Framework;

public class ScoreMathTests
{
    [Test]
    public void TopTierStaysReachableAtMaxStrictness() =>
        Assert.Less(ScoreMath.AdjustedThreshold(0.95f, 3f), 1f);

    [Test]
    public void ThresholdsRiseMonotonically()
    {
        float previous = 0f;
        for (float s = 1f; s <= 3f; s += 0.05f)
        {
            float t = ScoreMath.AdjustedThreshold(0.70f, s);
            Assert.GreaterOrEqual(t, previous);
            previous = t;
        }
    }

    [Test]
    public void LateCriticalScoresBelowOnTimeCritical()
    {
        var onTime = new ShiftMetrics { workOrders = new List<WorkOrder> {
            new WorkOrder { priority = WorkOrderPriority.Critical, state = WorkOrderState.Completed, quality = 1f, deadline = 90f, resolvedAt = 60f } } };
        var late = new ShiftMetrics { workOrders = new List<WorkOrder> {
            new WorkOrder { priority = WorkOrderPriority.Critical, state = WorkOrderState.Completed, quality = 1f, deadline = 90f, resolvedAt = 120f } } };
        Assert.Greater(onTime.GetWorkOrderScore(), late.GetWorkOrderScore());
    }
}
```

Also worth a test: `ShiftDirector` with a fixed seed produces the same order
IDs, titles and tasks two runs in a row. Use fake `IShiftTask`s, which is
possible because the registry is static and the interface is plain C#.

### 10.2 Docs and repo hygiene

- `README.md`: "first-person" → "isometric". Add controls from §12.1.
  List what's saved.
- `CLAUDE.md`: remove the stale "CleaningTask / ShiftTerminalUI not in
  scene" claims (§0.6). Add a pointer to this file.
- Commit the `ProjectVersion.txt` your editor writes.
- Delete `PrototypeScene 2` (the `46c4196` message says a permission gate
  blocked the delete).
- Two solution files exist: `Project Skeleton.slnx` and
  `Project-Skeleton.slnx`. Both are generated by the IDE integration, so
  `.gitignore` `*.slnx` rather than chasing duplicates.
- `WallOccluder` logs on every fade start, and `ClickToMove` logs on every
  click. Put these behind a `verboseLogging` bool before recording a demo.

---

## 11. Deferred: the pluggable agent hook

Per `CLAUDE.md`, **one headline**: the evaluation game is the headline, and
the agent is depth. Build it after M2/M3, because the director makes it
almost free:

- `IShiftTask` + `WorkOrder` is already the contract an agent needs. Add
  `ShiftDirector.AssignTo(IWorkerAgent agent, WorkOrder order)` and an
  **AI-delegation** decision on the terminal: "Delegate WO-04-003 to
  MAINTENANCE UNIT 2 (−$40)."
- The evaluation then judges **your delegation**, not just your hands:
  *"Operator delegated 3 orders. Unit performance: 0.82. Operator
  performance: 0.64. Recommendation under review."* This is the
  "human vs. machine under the same judge" story from
  `PORTFOLIO_CONTEXT.md`, and it lands naturally.
- Fix `NPCTaskActor` first. Route it with `NavMesh.CalculatePath` + corner
  following, the same way `PlayerMovement.SetClickMovePath` does. It
  currently walks through walls (B19).
- A seeded director (§6.4) gives reproducible agent-vs-player comparisons,
  which makes a good profiler/metrics screenshot.

---

## 12. Appendices

### 12.1 Keybindings — current conflicts → proposed

| Key | Today | Proposed |
|---|---|---|
| **E** | `PlayerInteraction` pickup/drop/switch · `TaskBehavior` hold · `CamerasManager` enter PC | `PlayerInteractor` only: use / hold-to-work on the **focused** target; also exits focus view |
| **R** | — | secondary action (report anomaly) |
| **G** | — | drop carried item |
| **Q** | throw **and** debug drain ×2000 | unbound |
| **Z** | debug freeze drains | unbound |
| **F9 / F10** | — | debug drain fast / freeze (Editor + dev builds only) |
| **Space** | jump **and** task soft-cancel | task soft-cancel only (jump removed) |
| **Esc** | `CamerasManager` exit PC | exit focus view / PC |
| **LMB** | `ClickToMove` + `PlayerInteraction` both | `ClickToMove` only: walk, or walk-then-use |
| **Shift** | sprint | sprint (a policy memo may forbid it 😈) |

### 12.2 Layers — see §5.0

### 12.3 Starting tuning **[Guess — tune by playing]**

| Knob | Value | Reasoning |
|---|---|---|
| Shift length | 600 s → **300 s** for playtests | 10 min is long for iterating; the docs originally said 5 |
| Power generator | 8/s at 100% | Covers WS L1 (6/s) plus lights, with ~25% headroom |
| Manifold | 4 × 1.5/s = 6/s | Covers WS L1 O₂ (4/s) plus breathing (0.3–0.5/s) |
| Canister capacity | 1200 | At 1.5/s ≈ 13 min; with staggered start fills, 1–2 run dry per 5-min shift |
| Workstation L1 | 3 $/s, burns 6 power + 4 O₂ | Upgrade to L2 costs ~2.5 min of income |
| Growth | income ×1.6, burn ×1.35 per level | Upgrades pay, but raise the maintenance bar |
| Storage | 500 max, 50 req | Unchanged; ~1 min of buffer at full burn with zero production |
| Day 1 plan | 2 routine, 0 incidents, difficulty 1 | Tutorial day |
| Day 5 plan | ~3–4 routine, 2 incidents, difficulty 2 | Coupled breakers start |
| Day 10 plan | 5 routine, 3 incidents, difficulty 3–4 | Leaks, cross coupling, tight deadlines |
| Notices to terminate | 3 | Stakes without instant loss |

### 12.4 Scene wiring checklists (need the Editor)

**M1**
- [ ] Rename layers 6→Wall, 7→Ground; add 8 Usable.
- [ ] Player: add `PlayerInteractor` (holdPoint = `HeldItemTransform`, usable = Usable, dropSurface = Ground, prompt = the existing prompt Text). Disable `PlayerInteraction`.
- [ ] `ClickToMove`: assign `interactor`, `usableLayer`.
- [ ] `PlayerTaskActor`: assign `interactor`.
- [ ] Scene: new `FocusCamera` (disabled), plus a `PanelFocusView` object (world cam = PlayerCamera; disableWhileFocused = PlayerMovement, ClickToMove, PlayerInteractor, CamerasManager).
- [ ] Each PC camPos: add `TerminalInteractable` + a collider on Usable.

**M2**
- [ ] Create `ShiftPolicy` asset (Assets/Settings/ShiftPolicy.asset).
- [ ] AIManager object: add `ShiftDirector` (policy) + `DayCycle` (policy, director, aiManager, terminal = the `ShiftTerminalUI`, playerSpawn).
- [ ] `StationManager`: assign `shiftDirector`, `dayCycle`.
- [ ] `cleaning_task_01`: add `ManualShiftTask`, move the sphere/cylinder visuals into `dirtyVisuals`.
- [ ] HUD: `WorkOrderBoardUI` with a TMP text.
- [ ] Give `ShiftTerminalUI.shiftLabel` a text box big enough for the briefing (it's unassigned now **[Certain]**).

**M3**
- [ ] Power room: panel housing (collider on Usable) + `BreakerPanelTask` + `focusAnchor` + `PowerGenerator` (target = power Storage).
- [ ] `BreakerSwitch` prefab: body, lever child, lamp child (emissive material), collider.
- [ ] O₂ room: manifold with 4 `ItemSocket`s (seat transforms), `OxygenManifoldTask`, `CanisterDispenser`, `CanisterRecycler`, `OxygenCanister` prefab (Rigidbody, collider on Usable, `CarryableItem` fields).
- [ ] Disable `FuseBoard` / `OxygenPuzzle` objects.

**M4**
- [ ] `StationCondition` object (director assigned). URP renderer: enable the Decal feature.
- [ ] `ConditionFlickerLight` on room lights (power room → powerSource = generator).
- [ ] Surveillance camera prefab (pivot + LED light) × 5, each under a `ProgressionUnlock`.
- [ ] Gauges on both storage tanks; `DayBoard` in the hub; `PerformanceHistoryUI` on the terminal.

### 12.5 Commit plan (one concern per commit)

M0:
1. `fix(interaction): measure reach from player body, not iso camera`
2. `fix(ai): apply strictness once; normalize weights; ordered tiers`
3. `fix(ui): keep cursor free after evaluation`
4. `fix(consumption): move debug drains to F9/F10 behind dev guard; honour serialized drain`
5. `fix(fuseboard): exact error count, never pre-solved`
6. `chore: commit editor-updated ProjectVersion.txt`

M1:
7. `feat(interaction): IInteractable + PlayerInteractor`
8. `feat(interaction): CarryableItem + ItemSocket`
9. `feat(interaction): PanelFocusView`
10. `refactor(input): ClickToMove owns LMB; PlayerTaskActor honours focus; remove jump`
11. `refactor(cameras): terminal via TerminalInteractable`

M2:
12. `feat(progression): WorkOrder / IShiftTask / registry / ShiftPolicy`
13. `feat(progression): ShiftDirector`
14. `feat(progression): ManualShiftTask adapter + TaskBehavior events`
15. `feat(ai): score against work orders; probation line; briefing`
16. `feat(progression): DayCycle + CareerStore save`
17. `feat(ui): WorkOrderBoardUI`

M3:
18. `feat(power): PowerGenerator`
19. `feat(power): BreakerPanelTask + BreakerSwitch`
20. `feat(oxygen): canisters, dispenser, recycler, manifold task`
21. `fix(economy): workstation growth curve + recorded consumption; pay-then-upgrade`
22. `chore: remove FuseBoard/OxygenPuzzle/OxygenTank/PlayerInteraction` (only after parity)

M4–M6: one commit per visual component / task / test file.

---

### Open questions for you (decide before M2)

1. **Setting pivot** (space vs underwater/underground). M4 visuals (decals,
   particles, signage) are where reskin cost lands. **I'd decide before
   M4, not before M2.** M0–M3 are setting-agnostic.
2. **Fail state.** Three notices → contract terminated → career resets.
   Yes/no? Without it the loop has no stakes. With it you need an end
   screen (small).
3. **Shift length** for the slice: 300 s or 600 s?
4. **Throwing:** I've recommended cutting it (§5.2). If you want it back,
   say so before M1 lands. `CarryableItem.PlaceAt` would get an impulse
   variant.
