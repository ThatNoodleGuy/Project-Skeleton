using UnityEngine;

/// <summary>
/// Hold-to-work manual task: progress 0→1, operational threshold (~75%), perfected at 100%.
/// Wires into ShiftMetrics only while a shift is active. Use a trigger collider + "Player" tag.
/// Subclass (e.g. CleaningTask) for VFX/UI; override hooks instead of duplicating progress logic.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TaskBehavior : MonoBehaviour
{
    [Header("Identity (ShiftMetrics)")]
    [Tooltip("Stable id for this instance for the whole shift. If empty, name + instance ID is used.")]
    [SerializeField] private string uniqueTaskId;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("Progress per second while interact key is held in range.")]
    [SerializeField] private float progressPerSecond = 0.35f;
    [Tooltip("Normalized progress (0-1) required for \"operational\" (~75% in design docs).")]
    [Range(0f, 1f)]
    [SerializeField] private float operationalThreshold = 0.75f;
    [Tooltip("Doc parity: hold Space to pause progress (soft cancel). Leave interact key held.")]
    [SerializeField] private KeyCode cancelKey = KeyCode.Space;
    [SerializeField] private bool useSoftCancel = true;

    [Header("State")]
    [SerializeField] [Range(0f, 1f)] private float progress;
    [SerializeField] private bool taskCompleted;

    private Collider col;
    private bool isPlayerInRange;
    private bool hasLoggedAttemptThisShift; // first hold-in-range this shift for metrics edge cases

    /// <summary>0–1 progress; read-only for UI elsewhere if needed.</summary>
    public float Progress => progress;
    public bool IsComplete => taskCompleted;
    public float OperationalThreshold => operationalThreshold;

    protected virtual void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[TaskBehavior] {name}: collider should be IsTrigger for range detection.");
    }

    protected virtual void Update()
    {
        if (taskCompleted)
            return;

        if (!isPlayerInRange || !CanReceiveInput())
            return;

        bool cancelHeld = useSoftCancel && Input.GetKey(cancelKey);
        bool interactHeld = Input.GetKey(interactKey);

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

    /// <summary>Override if you need UI pause, cutscenes, etc.</summary>
    protected virtual bool CanReceiveInput()
    {
        return true;
    }

    private void TryRecordAttempt()
    {
        StationManager sm = StationManager.Instance;
        if (sm == null || !sm.ShiftInProgress)
            return;

        // ShiftMetrics counts distinct task ids; still call once per "session start" for clarity/logging.
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

    /// <summary>Progress updated but task not necessarily complete.</summary>
    protected virtual void OnProgressChanged(float normalizedProgress) { }

    /// <summary>Crossed operational threshold this shift (once per id in ShiftMetrics).</summary>
    protected virtual void OnTaskOperational() { }

    /// <summary>Reached 100%.</summary>
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
        // Optional: abandon mid-bar — reset local progress so they must redo work next visit.
        if (!taskCompleted)
        {
            progress = 0f;
            hasLoggedAttemptThisShift = false;
        }
    }

    /// <summary>
    /// Call when a new shift starts (e.g. from <see cref="StationManager.StartShift"/>) so tasks can be completed again.
    /// </summary>
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