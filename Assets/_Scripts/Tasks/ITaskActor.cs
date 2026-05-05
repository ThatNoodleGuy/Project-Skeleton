using UnityEngine;

public interface ITaskActor
{
    bool WantsInteractHold(TaskBehavior task);
    bool WantsCancelHold(TaskBehavior task);
}