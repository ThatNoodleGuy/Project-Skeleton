using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private float pickupRadius = 0.5f;  // More forgiving pickup
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showRaycastInGame = true;  // Show during gameplay
    [SerializeField] private bool debugVisualization = true;  // Show in editor
    
    [Header("Held Object")]
    [SerializeField] private Transform holdPosition;  // Optional: dedicated hold transform
    
    [Header("UI Feedback")]
    [SerializeField] private UnityEngine.UI.Text interactionPrompt;
    
    private Camera playerCamera;
    private OxygenTank heldTank;
    private OxygenTank currentLookTarget;  // What we're currently looking at
    
    // Properties
    public bool IsHoldingObject => heldTank != null;
    public OxygenTank HeldTank => heldTank;

    void Start()
    {
        // Method 1: Try to find by tag first (most reliable for your setup)
        GameObject cameraObj = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (cameraObj != null)
        {
            playerCamera = cameraObj.GetComponent<Camera>();
        }
        
        // Method 2: Try to find PlayerCamera component if tag didn't work
        if (playerCamera == null)
        {
            #if UNITY_6000_0_OR_NEWER
                PlayerCamera playerCameraComponent = FindAnyObjectByType<PlayerCamera>();
            #else
                PlayerCamera playerCameraComponent = FindObjectOfType<PlayerCamera>();
            #endif
            
            if (playerCameraComponent != null)
            {
                playerCamera = playerCameraComponent.GetComponent<Camera>();
            }
        }
        
        // Method 3: Fallback to Camera.main
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            Debug.LogWarning("PlayerInteraction: Using Camera.main as fallback.");
        }
        
        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteraction: Could not find camera! Tag your camera as 'PlayerCamera' or 'MainCamera'");
        }
        
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        // Safely release any held object when disabled
        if (IsHoldingObject)
        {
            heldTank.ForceRelease();
            heldTank = null;
        }
    }

    void Update()
    {
        if (!IsHoldingObject)
        {
            CheckForInteractable();
        }
        else
        {
            // Update prompt for held object
            ShowInteractionPrompt("E to Drop | Q to Throw");
        }
        
        // Draw debug rays during gameplay
        if (showRaycastInGame)
        {
            DrawDebugRays();
        }
        
        HandleInput();
    }
    
    /// <summary>
    /// Check if player is looking at an interactable object using sphere cast for more forgiving detection
    /// </summary>
    void CheckForInteractable()
    {
        if (playerCamera == null) 
        {
            HideInteractionPrompt();
            return;
        }
        
        currentLookTarget = FindClosestInteractable();
        
        // Check for FuseSwitch interaction (new!)
        FuseSwitch fuseSwitch = FindClosestFuseSwitch();
        
        if (currentLookTarget != null)
        {
            ShowInteractionPrompt("Press E to Pick Up");
        }
        else if (fuseSwitch != null && fuseSwitch.CanInteract())
        {
            ShowInteractionPrompt("Press E to Toggle Switch");
        }
        else
        {
            HideInteractionPrompt();
        }
    }
    
    /// <summary>
    /// Find the closest interactable object in front of the player
    /// Uses both raycast and sphere cast for better detection
    /// </summary>
    OxygenTank FindClosestInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        OxygenTank closestTank = null;
        float closestDistance = interactionRange;
        
        // Method 1: Direct raycast (most accurate for precise aiming)
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            OxygenTank tank = hit.collider.GetComponent<OxygenTank>();
            if (tank != null && tank.CanInteract(transform.position))
            {
                closestTank = tank;
                closestDistance = hit.distance;
            }
        }
        
        // Method 2: Sphere cast (more forgiving, especially for small objects or ground-level items)
        RaycastHit[] hits = Physics.SphereCastAll(ray, pickupRadius, interactionRange, interactableLayer);
        
        foreach (RaycastHit sphereHit in hits)
        {
            OxygenTank tank = sphereHit.collider.GetComponent<OxygenTank>();
            
            if (tank != null && tank.CanInteract(transform.position))
            {
                // Prefer closer objects
                if (sphereHit.distance < closestDistance)
                {
                    closestTank = tank;
                    closestDistance = sphereHit.distance;
                }
            }
        }
        
        // Method 3: Fallback for objects at player's feet
        // Check for objects in a small sphere around the player (helpful when looking straight down)
        Collider[] nearbyColliders = Physics.OverlapSphere(
            playerCamera.transform.position + playerCamera.transform.forward * 0.5f, 
            pickupRadius * 2f, 
            interactableLayer
        );
        
        foreach (Collider col in nearbyColliders)
        {
            OxygenTank tank = col.GetComponent<OxygenTank>();
            if (tank != null && tank.CanInteract(transform.position))
            {
                float distance = Vector3.Distance(playerCamera.transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestTank = tank;
                    closestDistance = distance;
                }
            }
        }
        
        return closestTank;
    }
    
    /// <summary>
    /// Find the closest fuse switch the player is looking at
    /// </summary>
    FuseSwitch FindClosestFuseSwitch()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        // Simple raycast for switches (they're usually on walls, don't need sphere cast)
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            FuseSwitch fuseSwitch = hit.collider.GetComponent<FuseSwitch>();
            if (fuseSwitch != null)
            {
                return fuseSwitch;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Handle player input for interaction
    /// </summary>
    void HandleInput()
    {
        // Pick up / Drop with E
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!IsHoldingObject)
            {
                // Try to pick up oxygen tank
                if (currentLookTarget != null)
                {
                    TryPickUp();
                }
                // Or try to interact with fuse switch
                else
                {
                    TryInteractWithSwitch();
                }
            }
            else
            {
                DropHeldObject();
            }
        }
        
        // Alternative: Left click to pick up (only if not holding)
        if (Input.GetMouseButtonDown(0) && !IsHoldingObject)
        {
            if (currentLookTarget != null)
            {
                TryPickUp();
            }
            else
            {
                TryInteractWithSwitch();
            }
        }
        
        // Throw with Q or right click
        if (IsHoldingObject)
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
            {
                ThrowHeldObject();
            }
        }
    }
    
    /// <summary>
    /// Try to interact with a fuse switch
    /// </summary>
    void TryInteractWithSwitch()
    {
        FuseSwitch fuseSwitch = FindClosestFuseSwitch();
        if (fuseSwitch != null && fuseSwitch.CanInteract())
        {
            fuseSwitch.Interact();
        }
    }
    
    /// <summary>
    /// Try to pick up the object we're looking at
    /// </summary>
    void TryPickUp()
    {
        if (currentLookTarget != null)
        {
            PickUpObject(currentLookTarget);
        }
    }
    
    /// <summary>
    /// Pick up a specific tank
    /// </summary>
    void PickUpObject(OxygenTank tank)
    {
        if (tank == null) return;
        
        heldTank = tank;
        
        // Use custom hold position if set, otherwise use camera with offset
        Transform holdPoint;
        if (holdPosition != null)
        {
            holdPoint = holdPosition;
        }
        else
        {
            // Create a virtual hold point in front of the camera
            holdPoint = playerCamera.transform;
        }
        
        heldTank.PickUp(holdPoint);
        
        // Clear the look target since we're now holding it
        currentLookTarget = null;
    }
    
    /// <summary>
    /// Drop the held object gently
    /// </summary>
    void DropHeldObject()
    {
        if (heldTank == null) return;
        
        heldTank.Drop();
        heldTank = null;
        
        // Prompt will be updated in next CheckForInteractable call
    }
    
    /// <summary>
    /// Throw the held object with force
    /// </summary>
    void ThrowHeldObject()
    {
        if (heldTank == null) return;
        
        heldTank.Throw();
        heldTank = null;
        
        // Prompt will be updated in next CheckForInteractable call
    }
    
    /// <summary>
    /// Show interaction UI prompt
    /// </summary>
    void ShowInteractionPrompt(string message)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.text = message;
            if (!interactionPrompt.gameObject.activeSelf)
            {
                interactionPrompt.gameObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Hide interaction UI prompt
    /// </summary>
    void HideInteractionPrompt()
    {
        if (interactionPrompt != null && interactionPrompt.gameObject.activeSelf)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Public method to check if player can currently interact with something
    /// </summary>
    public bool CanInteractWithSomething()
    {
        return !IsHoldingObject && currentLookTarget != null;
    }
    
    /// <summary>
    /// Draw debug rays during gameplay (visible in Scene view while playing)
    /// </summary>
    void DrawDebugRays()
    {
        if (playerCamera == null) return;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        // Check if we're hitting something
        RaycastHit hit;
        bool isHitting = Physics.Raycast(ray, out hit, interactionRange, interactableLayer);
        
        if (isHitting)
        {
            // Draw ray to hit point in green
            Debug.DrawLine(ray.origin, hit.point, Color.green);
            
            // Draw remaining distance in yellow
            Debug.DrawLine(hit.point, ray.origin + ray.direction * interactionRange, Color.yellow);
            
            // Draw a cross at hit point
            Vector3 hitPoint = hit.point;
            float crossSize = 0.1f;
            Debug.DrawLine(hitPoint - Vector3.right * crossSize, hitPoint + Vector3.right * crossSize, Color.red);
            Debug.DrawLine(hitPoint - Vector3.up * crossSize, hitPoint + Vector3.up * crossSize, Color.red);
            Debug.DrawLine(hitPoint - Vector3.forward * crossSize, hitPoint + Vector3.forward * crossSize, Color.red);
            
            // Check if hit object is a valid tank
            OxygenTank tank = hit.collider.GetComponent<OxygenTank>();
            if (tank != null && tank.CanInteract(transform.position))
            {
                // Draw sphere around valid target in bright green
                DrawDebugCircle(tank.transform.position, 0.3f, Color.green, 12);
            }
            
            // Check if hit object is a fuse switch
            FuseSwitch fuseSwitch = hit.collider.GetComponent<FuseSwitch>();
            if (fuseSwitch != null && fuseSwitch.CanInteract())
            {
                // Draw sphere around valid switch in cyan
                DrawDebugCircle(fuseSwitch.transform.position, 0.2f, Color.cyan, 12);
            }
        }
        else
        {
            // No hit - draw full ray in red
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red);
        }
        
        // Draw sphere cast visualization
        Vector3 sphereStart = ray.origin;
        Vector3 sphereEnd = ray.origin + ray.direction * interactionRange;
        Color sphereColor = currentLookTarget != null ? Color.green : new Color(1f, 1f, 0f, 0.5f);
        
        // Draw circles along the ray to show sphere cast
        for (int i = 0; i <= 5; i++)
        {
            float t = i / 5f;
            Vector3 pos = Vector3.Lerp(sphereStart, sphereEnd, t);
            DrawDebugCircle(pos, pickupRadius, sphereColor, 8);
        }
        
        // Highlight current target if we have one
        if (currentLookTarget != null)
        {
            Debug.DrawLine(playerCamera.transform.position, currentLookTarget.transform.position, Color.cyan);
            DrawDebugCircle(currentLookTarget.transform.position, 0.5f, Color.cyan, 16);
        }
    }
    
    /// <summary>
    /// Helper method to draw a circle in 3D space
    /// </summary>
    void DrawDebugCircle(Vector3 center, float radius, Color color, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.right * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Debug.DrawLine(prevPoint, newPoint, color);
            prevPoint = newPoint;
        }
        
        // Also draw vertical circle
        prevPoint = center + Vector3.up * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Debug.DrawLine(prevPoint, newPoint, color);
            prevPoint = newPoint;
        }
    }
    
    /// <summary>
    /// Draw interaction visualization in editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!debugVisualization || playerCamera == null) return;
        
        // Draw main interaction ray
        Gizmos.color = currentLookTarget != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
        
        // Draw sphere cast radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 spherePos = playerCamera.transform.position + playerCamera.transform.forward * (interactionRange * 0.5f);
        Gizmos.DrawWireSphere(spherePos, pickupRadius);
        
        // Draw endpoint sphere
        Gizmos.color = Color.cyan;
        Vector3 endPoint = playerCamera.transform.position + playerCamera.transform.forward * interactionRange;
        Gizmos.DrawWireSphere(endPoint, pickupRadius);
        
        // Draw nearby detection sphere
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(
            playerCamera.transform.position + playerCamera.transform.forward * 0.5f, 
            pickupRadius * 2f
        );
        
        // Highlight current target
        if (currentLookTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentLookTarget.transform.position, 0.5f);
            Gizmos.DrawLine(playerCamera.transform.position, currentLookTarget.transform.position);
        }
    }
}