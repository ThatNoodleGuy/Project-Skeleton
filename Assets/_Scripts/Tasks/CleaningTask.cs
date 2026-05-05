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