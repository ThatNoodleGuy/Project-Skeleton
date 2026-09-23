using UnityEngine;

/// <summary>
/// Hold-to-work manual task: progress 0→1, operational threshold (~75%), perfected at 100%.
/// Wires into ShiftMetrics only while a shift is active. Use a trigger collider; range is
/// granted to the "Player"-tagged object (raw key input) or to any collider carrying an
/// ITaskActor component (e.g. NPCTaskActor), whichever enters.
/// Subclass (e.g. CleaningTask) for VFX/UI; override hooks instead of duplicating progress logic.
/// </summary>
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
    [Tooltip("Explicit override: if set, this actor is always used regardless of what enters the trigger. Leave empty to auto-resolve per entering collider (player key input, or any ITaskActor component such as NPCTaskActor).")]
    [SerializeField] private MonoBehaviour taskActorBehaviour;

    [Header("State")]
    [SerializeField] [Range(0f, 1f)] private float progress;
    [SerializeField] private bool taskCompleted;

    private ITaskActor forcedActor;
    private ITaskActor activeActor;
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

        forcedActor = taskActorBehaviour as ITaskActor;
    }

    protected virtual void Update()
    {
        if (taskCompleted)
            return;
        if (!isPlayerInRange || !CanReceiveInput())
            return;

        ITaskActor actor = forcedActor ?? activeActor;

        bool interactHeld = actor != null ? actor.WantsInteractHold(this) : Input.GetKey(interactKey);
        bool cancelHeld = useSoftCancel &&
                          (actor != null ? actor.WantsCancelHold(this) : Input.GetKey(cancelKey));

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

    // Single-occupant assumption: if a second actor enters while one is already in
    // range, whichever leaves first clears range for both. Matches this prototype's
    // existing single-player-in-room simplification elsewhere (e.g. RoomController).
    private void OnTriggerEnter(Collider other)
    {
        ITaskActor enteringActor = other.GetComponent<ITaskActor>();
        if (enteringActor == null && !other.CompareTag("Player"))
            return;

        activeActor = enteringActor;
        isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        ITaskActor exitingActor = other.GetComponent<ITaskActor>();
        if (exitingActor == null && !other.CompareTag("Player"))
            return;

        isPlayerInRange = false;
        activeActor = null;
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