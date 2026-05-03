using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Fuse Board Puzzle - Manages puzzle state only
/// Player interaction handled by PlayerInteraction script
/// </summary>
public class FuseBoard : MonoBehaviour
{
    [Header("Room Reference")]
    [SerializeField] private RoomController powerRoomController;
    
    [Header("Puzzle Setup")]
    [SerializeField] private GameObject switchPrefab;
    [SerializeField] private Transform switchContainer;  // Parent for organization
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;  // Relative to this GameObject
    [SerializeField] private Vector2 switchSpacing = new Vector2(0.3f, 0.3f);
    [SerializeField] private float switchForwardOffset = 0.05f;  // NEW: Push switches forward from board
    
    [Header("Puzzle Settings")]
    [SerializeField] private int baseGridSize = 3;  // 3x3 at level 1
    [SerializeField] private int maxGridSize = 5;   // 5x5 max
    
    [Header("Visual Feedback")]
    [SerializeField] private Material correctMaterial;
    [SerializeField] private Material incorrectMaterial;
    [SerializeField] private Material solvedMaterial;
    
    private List<FuseSwitch> switches = new List<FuseSwitch>();
    private bool puzzleActive = false;
    private bool puzzleSolved = false;

    void Update()
    {
        // Show puzzle when room needs resources
        if (powerRoomController != null && !powerRoomController.HasRecource() && !puzzleActive)
        {
            GeneratePuzzle();
            puzzleActive = true;
            puzzleSolved = false;
        }
        
        // Clear puzzle when player leaves
        if (powerRoomController != null && powerRoomController.isGoingOut && puzzleActive)
        {
            ClearPuzzle();
            puzzleActive = false;
        }
    }
    
    /// <summary>
    /// Generate the puzzle based on room level
    /// FIXED: Spawns switches with forward offset
    /// </summary>
    void GeneratePuzzle()
    {
        ClearPuzzle();
        
        if (switchPrefab == null)
        {
            Debug.LogError("[FuseBoard] Switch prefab is not assigned!");
            return;
        }
        
        // Calculate grid size
        int level = powerRoomController?.myTank?.level ?? 1;
        int gridSize = Mathf.Clamp(baseGridSize + (level - 1) / 2, baseGridSize, maxGridSize);
        
        // Create switch grid
        int index = 0;
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                // Calculate local position (relative to this transform)
                // FIXED: Add forward offset to prevent spawning inside wall
                Vector3 localPos = gridOrigin + new Vector3(
                    col * switchSpacing.x - (gridSize - 1) * switchSpacing.x / 2f,  // Center horizontally
                    row * switchSpacing.y - (gridSize - 1) * switchSpacing.y / 2f,  // Center vertically
                    switchForwardOffset  // FIXED: Use forward offset instead of hardcoded 0
                );
                
                // Convert to world space
                Vector3 worldPos = transform.TransformPoint(localPos);
                
                // Instantiate switch
                GameObject switchObj = Instantiate(switchPrefab, worldPos, transform.rotation, 
                    switchContainer != null ? switchContainer : transform);
                switchObj.name = $"Switch_R{row}_C{col}";
                
                FuseSwitch fuseSwitch = switchObj.GetComponent<FuseSwitch>();
                if (fuseSwitch != null)
                {
                    // Random initial state
                    bool startOn = Random.value > 0.5f;
                    fuseSwitch.Initialize(this, index, startOn, true);  // Pass reference to this board
                    fuseSwitch.SetMaterials(correctMaterial, incorrectMaterial);
                    
                    switches.Add(fuseSwitch);
                }
                else
                {
                    Debug.LogError($"[FuseBoard] Switch prefab is missing FuseSwitch component! ({switchObj.name})");
                }
                
                index++;
            }
        }
        
        // Set some switches to incorrect (puzzle goal)
        int errorCount = Mathf.Clamp(level, 1, switches.Count / 2);
        List<int> incorrectIndices = new List<int>();
        
        while (incorrectIndices.Count < errorCount)
        {
            int randomIndex = Random.Range(0, switches.Count);
            if (!incorrectIndices.Contains(randomIndex))
            {
                incorrectIndices.Add(randomIndex);
                switches[randomIndex].SetCorrectState(false);
            }
        }
        
        Debug.Log($"[FuseBoard] Generated {gridSize}x{gridSize} puzzle with {errorCount} errors at forward offset {switchForwardOffset}");
    }
    
    /// <summary>
    /// Clear all switches
    /// </summary>
    void ClearPuzzle()
    {
        foreach (var sw in switches)
        {
            if (sw != null)
                Destroy(sw.gameObject);
        }
        
        switches.Clear();
    }
    
    /// <summary>
    /// Called by FuseSwitch when it's toggled (via PlayerInteraction)
    /// </summary>
    public void OnSwitchToggled(FuseSwitch toggledSwitch)
    {
        if (puzzleSolved) return;
        
        CheckSolution();
    }
    
    /// <summary>
    /// Check if puzzle is solved
    /// </summary>
    void CheckSolution()
    {
        bool allCorrect = true;
        
        foreach (var sw in switches)
        {
            if (sw != null && !sw.IsInCorrectState())
            {
                allCorrect = false;
                break;
            }
        }
        
        if (allCorrect)
        {
            OnPuzzleSolved();
        }
    }
    
    /// <summary>
    /// Called when puzzle is solved
    /// </summary>
    void OnPuzzleSolved()
    {
        puzzleSolved = true;
        
        // Visual feedback - all switches turn solved color
        foreach (var sw in switches)
        {
            if (sw != null && solvedMaterial != null)
            {
                sw.SetSolvedMaterial(solvedMaterial);
            }
        }

        // Fill the room storage
        if (powerRoomController != null)
        {
            powerRoomController.FillStorage();
        }
        
        Debug.Log("[FuseBoard] Puzzle solved!");
    }
    
    /// <summary>
    /// Check if puzzle is currently active and not solved
    /// </summary>
    public bool CanInteract()
    {
        return puzzleActive && !puzzleSolved;
    }
    
    /// <summary>
    /// Optional: Draw grid in editor for setup
    /// FIXED: Shows actual spawn positions including forward offset
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            int gridSize = baseGridSize;
            Gizmos.color = Color.yellow;
            
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    // FIXED: Include forward offset in visualization
                    Vector3 localPos = gridOrigin + new Vector3(
                        col * switchSpacing.x - (gridSize - 1) * switchSpacing.x / 2f,
                        row * switchSpacing.y - (gridSize - 1) * switchSpacing.y / 2f,
                        switchForwardOffset  // FIXED: Show actual spawn position
                    );
                    
                    Vector3 worldPos = transform.TransformPoint(localPos);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.15f);
                    
                    // Draw line from board to switch position
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, worldPos);
                    Gizmos.color = Color.yellow;
                }
            }
            
            // Draw board position and forward direction
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.2f);
        }
    }
}