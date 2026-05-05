using UnityEngine;

public class PlayerTaskActor : MonoBehaviour, ITaskActor
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode cancelKey = KeyCode.Space;

    public bool WantsInteractHold(TaskBehavior task) => Input.GetKey(interactKey);
    public bool WantsCancelHold(TaskBehavior task) => Input.GetKey(cancelKey);
}