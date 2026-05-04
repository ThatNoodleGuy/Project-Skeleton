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