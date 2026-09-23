using UnityEngine;

/// <summary>
/// Minimal autonomous worker: walks straight toward its assigned TaskBehavior and
/// holds "interact" once in range. Exists to prove ITaskActor is genuinely
/// swappable — TaskBehavior's progress/metrics logic is untouched, this just
/// answers the same two questions PlayerTaskActor answers from input.
/// Needs a Collider and a Rigidbody (kinematic is fine) on this GameObject so
/// Unity fires OnTriggerEnter/Exit against the task's trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NPCTaskActor : MonoBehaviour, ITaskActor
{
    [SerializeField] private TaskBehavior assignedTask;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float arrivalDistance = 0.5f;

    public TaskBehavior AssignedTask
    {
        get => assignedTask;
        set => assignedTask = value;
    }

    private void Update()
    {
        if (assignedTask == null || assignedTask.IsComplete)
            return;

        Vector3 targetPosition = assignedTask.transform.position;
        Vector3 currentPosition = transform.position;

        if (Vector3.Distance(currentPosition, targetPosition) > arrivalDistance)
        {
            transform.position = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    public bool WantsInteractHold(TaskBehavior task)
    {
        if (task != assignedTask)
            return false;

        return Vector3.Distance(transform.position, task.transform.position) <= arrivalDistance;
    }

    public bool WantsCancelHold(TaskBehavior task) => false;
}
