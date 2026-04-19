using UnityEngine;

namespace ImmuneSimulation.SimulationCore
{
    /// <summary>Creates a minimal simulation root when the loaded scene has none (Milestone 0).</summary>
    public static class MilestoneZeroBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindAnyObjectByType<SimulationControllerBehaviour>() != null)
                return;
            var go = new GameObject("Simulation (Milestone 0)");
            go.AddComponent<SimulationControllerBehaviour>();
        }
    }
}
