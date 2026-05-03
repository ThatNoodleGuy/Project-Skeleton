using UnityEngine;

/// <summary>
/// Makes the tool holder follow the camera's rotation for proper first-person aiming.
/// Attach this to the GameObject that holds your tools (the one with SelectTool script).
/// </summary>
public class ToolFollowCamera : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("The player's camera transform. Leave empty to auto-find by tag 'PlayerCamera'")]
    [SerializeField] private Transform playerCamera;
    
    [Header("Follow Settings")]
    [Tooltip("Should the tools follow camera rotation?")]
    [SerializeField] private bool followRotation = true;
    
    [Tooltip("Smooth the rotation for better feel")]
    [SerializeField] private bool smoothRotation = false;
    
    [Tooltip("Rotation smoothing speed (only if smoothRotation is true)")]
    [SerializeField] private float rotationSpeed = 10f;
    
    void Start()
    {
        // Auto-find camera if not assigned
        if (playerCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("PlayerCamera");
            if (cameraObj != null)
            {
                playerCamera = cameraObj.transform;
            }
            else
            {
                Debug.LogError("ToolFollowCamera: Could not find PlayerCamera! Make sure your camera has the 'PlayerCamera' tag.");
            }
        }
    }
    
    void LateUpdate()
    {
        if (!followRotation || playerCamera == null)
            return;
        
        if (smoothRotation)
        {
            // Smooth rotation
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                playerCamera.rotation, 
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            // Instant rotation (recommended for FPS)
            transform.rotation = playerCamera.rotation;
        }
    }
}