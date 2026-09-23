using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Secondary movement input: click the ground to walk there, routed along the
/// baked NavMesh. Shares a single raycast with PlayerInteraction's click-to-pickup:
/// if the nearest hit is on the Interactable layer, this script defers to that
/// instead of issuing a move command.
/// </summary>
public class ClickToMove : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float maxRayDistance = 100f;

    void Start()
    {
        if (cam == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("PlayerCamera");
            if (camObj != null)
                cam = camObj.GetComponent<Camera>();
        }

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (cam == null)
            Debug.LogError("ClickToMove: Could not find camera!");
        if (playerMovement == null)
            Debug.LogError("ClickToMove: Could not find PlayerMovement!");
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        if (cam == null || playerMovement == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        int combinedMask = groundLayer | interactableLayer;

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, combinedMask))
        {
            Debug.Log("[ClickToMove] Click raycast hit nothing on Ground or Interactable layers.");
            return;
        }

        // Layer alone isn't a reliable signal here — floor colliders in this scene
        // also happen to be on the Interactable layer. Check for an actual
        // interactable component instead (mirrors PlayerInteraction's own targets).
        bool hitInteractable = hit.collider.GetComponentInParent<OxygenTank>() != null
            || hit.collider.GetComponentInParent<FuseSwitch>() != null;
        if (hitInteractable)
            return; // PlayerInteraction's own click-pickup raycast owns this click

        NavMeshPath path = new NavMeshPath();
        bool calculated = NavMesh.CalculatePath(transform.position, hit.point, NavMesh.AllAreas, path);
        if (calculated && path.status != NavMeshPathStatus.PathInvalid)
        {
            playerMovement.SetClickMovePath(path.corners);
            Debug.Log($"[ClickToMove] Path to {hit.point} — status: {path.status}, corners: {path.corners.Length}");
        }
        else
        {
            Debug.LogWarning($"[ClickToMove] No valid path to {hit.point} (calculated: {calculated}, status: {path.status}). Is the player's current position on the baked NavMesh?");
        }
    }
}
