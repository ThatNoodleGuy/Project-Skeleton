# Project Skeleton — Milestone 0

This Unity project is reset to **Milestone 0** from the portfolio plan: a **fixed-timestep simulation loop** with **pause** and **step one tick**, and **no agents, fields, HUD, or JSON** yet.

## Folders (for upcoming milestones)

| Folder | Planned use |
|--------|-------------|
| `SimulationCore/` | Clock + simulation driver (started here) |
| `Spatial/` | Uniform grid / neighbor queries (Milestone 1) |
| `Cells/` | Data-oriented agents (Milestone 2) |
| `Molecules/` | Scalar field (Milestone 3) |
| `Visualization/` | Debug draw / heatmap later (Milestone 8) |
| `Config/` | JSON/XML scenarios (Milestone 9) |

## How to verify (acceptance)

1. Open `Assets/Scenes/SampleScene` and press **Play**.
2. A GameObject **Simulation (Milestone 0)** appears with **`SimulationControllerBehaviour`** (or add that component yourself on any GameObject and disable duplicate roots).
3. In the Inspector, **`Simulation Tick`** increases at ~50 ticks per real second when **`Paused`** is unchecked (`Fixed Delta Time` default `0.02` s).
4. Check **`Paused`** — tick stops increasing.
5. Right‑click **`SimulationControllerBehaviour`** → **Step one fixed tick** — tick advances by 1 while paused.

## Next steps

Implement **Milestone 1** in `Spatial/` (grid + pooled lists + neighbor query), then wire it from `SimulateOneFixedTick` when you are ready.
