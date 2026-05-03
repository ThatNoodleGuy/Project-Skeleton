using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Individual fuse switch - toggles between on/off states
/// Interacted with via PlayerInteraction (like OxygenTank)
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class FuseSwitch : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private MeshRenderer switchRenderer;
    [SerializeField] private Transform leverHandle;  // Optional: for visual animation
    
    [Header("Animation (Optional)")]
    [SerializeField] private float toggleAnimationSpeed = 5f;
    [SerializeField] private float onRotation = 45f;
    [SerializeField] private float offRotation = -45f;
    
    // State
    private bool isOn = false;
    private bool isCorrectWhenOn = true;
    private int switchIndex;
    private FuseBoard parentBoard;
    
    // Materials
    private Material correctMaterial;
    private Material incorrectMaterial;
    private Material solvedMaterial;
    
    // Animation
    private bool isAnimating = false;
    private float targetRotation;

    void Awake()
    {
        // Get or add required components
        if (switchRenderer == null)
        {
            switchRenderer = GetComponent<MeshRenderer>();
        }
        
        // Ensure we have a collider for raycasting
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one * 0.15f;
        }
    }

    void Update()
    {
        // Animate lever rotation if we have a handle
        if (leverHandle != null && isAnimating)
        {
            AnimateLever();
        }
    }

    /// <summary>
    /// Initialize the switch
    /// </summary>
    public void Initialize(FuseBoard board, int index, bool startOn, bool correctWhenOn)
    {
        parentBoard = board;
        switchIndex = index;
        isOn = startOn;
        isCorrectWhenOn = correctWhenOn;
        
        // Set initial visual state
        UpdateVisuals();
        
        if (leverHandle != null)
        {
            targetRotation = isOn ? onRotation : offRotation;
            leverHandle.localRotation = Quaternion.Euler(targetRotation, 0, 0);
        }
    }
    
    /// <summary>
    /// Set materials for visual feedback
    /// </summary>
    public void SetMaterials(Material correct, Material incorrect)
    {
        correctMaterial = correct;
        incorrectMaterial = incorrect;
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Set the solved material (puzzle complete)
    /// </summary>
    public void SetSolvedMaterial(Material solved)
    {
        solvedMaterial = solved;
        
        if (switchRenderer != null && solvedMaterial != null)
        {
            switchRenderer.material = solvedMaterial;
        }
    }
    
    /// <summary>
    /// Set what state is considered "correct"
    /// </summary>
    public void SetCorrectState(bool correctWhenOn)
    {
        isCorrectWhenOn = correctWhenOn;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Called by PlayerInteraction when player clicks this switch
    /// </summary>
    public void Interact()
    {
        if (parentBoard != null && !parentBoard.CanInteract())
        {
            return; // Puzzle is solved or inactive
        }
        
        Toggle();
    }
    
    /// <summary>
    /// Toggle the switch on/off
    /// </summary>
    void Toggle()
    {
        isOn = !isOn;
        UpdateVisuals();
        
        // Start animation
        if (leverHandle != null)
        {
            isAnimating = true;
            targetRotation = isOn ? onRotation : offRotation;
        }
        
        // Notify parent board
        if (parentBoard != null)
        {
            parentBoard.OnSwitchToggled(this);
        }
        
        // Optional: Add sound effect here
        // AudioSource.PlayClipAtPoint(toggleSound, transform.position);
    }
    
    /// <summary>
    /// Check if switch is in the correct state
    /// </summary>
    public bool IsInCorrectState()
    {
        return isOn == isCorrectWhenOn;
    }
    
    /// <summary>
    /// Check if player can interact (for UI prompts)
    /// </summary>
    public bool CanInteract()
    {
        return parentBoard != null && parentBoard.CanInteract();
    }
    
    /// <summary>
    /// Update visual appearance based on state
    /// </summary>
    void UpdateVisuals()
    {
        if (switchRenderer == null) return;
        
        bool isCorrect = IsInCorrectState();
        
        if (isCorrect && correctMaterial != null)
        {
            switchRenderer.material = correctMaterial;
        }
        else if (!isCorrect && incorrectMaterial != null)
        {
            switchRenderer.material = incorrectMaterial;
        }
        
        // Optional: Change emission or other properties for on/off state
        Material mat = switchRenderer.material;
        if (mat.HasProperty("_EmissionColor"))
        {
            if (isOn)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", isCorrect ? Color.green * 0.5f : Color.red * 0.5f);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
        }
    }
    
    /// <summary>
    /// Animate the lever handle
    /// </summary>
    void AnimateLever()
    {
        if (leverHandle == null) return;
        
        float currentRotation = leverHandle.localEulerAngles.x;
        
        // Normalize to -180 to 180
        if (currentRotation > 180) currentRotation -= 360;
        
        // Lerp to target
        float newRotation = Mathf.LerpAngle(currentRotation, targetRotation, 
            toggleAnimationSpeed * Time.deltaTime);
        
        leverHandle.localRotation = Quaternion.Euler(newRotation, 0, 0);
        
        // Stop animating when close enough
        if (Mathf.Abs(newRotation - targetRotation) < 0.1f)
        {
            leverHandle.localRotation = Quaternion.Euler(targetRotation, 0, 0);
            isAnimating = false;
        }
    }
    
    /// <summary>
    /// Get current state for debugging
    /// </summary>
    public string GetStateInfo()
    {
        return $"Switch {switchIndex}: {(isOn ? "ON" : "OFF")} - {(IsInCorrectState() ? "CORRECT" : "INCORRECT")}";
    }
    
    /// <summary>
    /// Highlight when mouse hovers (optional)
    /// </summary>
    void OnMouseEnter()
    {
        if (CanInteract())
        {
            // Scale up slightly when hovering
            transform.localScale = Vector3.one * 1.1f;
        }
    }
    
    void OnMouseExit()
    {
        // Return to normal scale
        transform.localScale = Vector3.one;
    }
    
    /// <summary>
    /// Draw gizmo to show state in editor
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = IsInCorrectState() ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.15f);
        
        // Draw a line to show on/off state
        Vector3 lineStart = transform.position;
        Vector3 lineEnd = transform.position + transform.up * (isOn ? 0.1f : -0.1f);
        Gizmos.DrawLine(lineStart, lineEnd);
    }
}