using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  Oxygen Puzzle - Collect oxygen tanks into disposal zone
/// Clean, position-independent system
/// </summary>
public class OxygenPuzzle : MonoBehaviour
{
    [Header("Room Reference")]
    [SerializeField] private RoomController oxygenRoomController;
    
    [Header("Puzzle Setup")]
    [SerializeField] private GameObject oxygenTankPrefab;
    [SerializeField] private Transform tankContainer;
    [SerializeField] private BoxCollider spawnZone;  // Area to spawn tanks
    [SerializeField] private BoxCollider disposalZone;  // Area to dispose tanks
    
    [Header("Puzzle Settings")]
    [SerializeField] private int baseTankCount = 5;
    [SerializeField] private int tanksPerLevel = 2;
    [SerializeField] private int maxTanks = 15;
    
    [Header("Visual Feedback")]
    [SerializeField] private Material tankMaterial;
    [SerializeField] private Renderer disposalZoneRenderer;
    [SerializeField] private Color activeZoneColor = Color.green;
    [SerializeField] private Color inactiveZoneColor = Color.gray;
    
    private List<OxygenTank> activeTanks = new List<OxygenTank>();
    private int targetTankCount = 0;
    private int disposedCount = 0;
    private bool puzzleActive = false;

    void Start()
    {
        // Setup disposal zone
        if (disposalZone != null)
        {
            disposalZone.isTrigger = true;
            
            // Add trigger handler
            DisposalZoneTrigger trigger = disposalZone.gameObject.GetComponent<DisposalZoneTrigger>();
            if (trigger == null)
            {
                trigger = disposalZone.gameObject.AddComponent<DisposalZoneTrigger>();
            }
            trigger.Initialize(this);
        }
        
        UpdateDisposalZoneVisual(false);
    }

    void Update()
    {
        // Show puzzle when room needs resources
        if (oxygenRoomController != null && !oxygenRoomController.HasRecource() && !puzzleActive)
        {
            GeneratePuzzle();
            puzzleActive = true;
        }
        
        // Clear puzzle when player leaves
        if (oxygenRoomController != null && oxygenRoomController.isGoingOut && puzzleActive)
        {
            ClearPuzzle();
            puzzleActive = false;
        }
    }
    
    /// <summary>
    /// Generate oxygen tanks based on room level
    /// </summary>
    void GeneratePuzzle()
    {
        ClearPuzzle();
        
        // Calculate tank count
        int level = oxygenRoomController?.myTank?.level ?? 1;
        targetTankCount = Mathf.Clamp(baseTankCount + (level - 1) * tanksPerLevel, baseTankCount, maxTanks);
        disposedCount = 0;
        
        // Spawn tanks in random positions
        if (spawnZone != null && oxygenTankPrefab != null)
        {
            for (int i = 0; i < targetTankCount; i++)
            {
                Vector3 randomPos = GetRandomPositionInZone(spawnZone);
                
                GameObject tankObj = Instantiate(oxygenTankPrefab, randomPos, Random.rotation, tankContainer != null ? tankContainer : transform);
                tankObj.name = $"OxygenTank_{i}";
                
                OxygenTank tank = tankObj.GetComponent<OxygenTank>();
                if (tank != null)
                {
                    tank.Initialize(i, tankMaterial);
                    activeTanks.Add(tank);
                }
            }
        }
        
        UpdateDisposalZoneVisual(true);
        Debug.Log($"[OxygenPuzzle] Generated {targetTankCount} tanks");
    }
    
    /// <summary>
    /// Clear all tanks
    /// </summary>
    void ClearPuzzle()
    {
        foreach (var tank in activeTanks)
        {
            if (tank != null)
                Destroy(tank.gameObject);
        }
        
        activeTanks.Clear();
        disposedCount = 0;
        UpdateDisposalZoneVisual(false);
    }
    
    /// <summary>
    /// Get random position within a box collider
    /// </summary>
    Vector3 GetRandomPositionInZone(BoxCollider zone)
    {
        Vector3 center = zone.transform.TransformPoint(zone.center);
        Vector3 size = zone.size;
        
        float x = Random.Range(-size.x / 2f, size.x / 2f);
        float y = Random.Range(-size.y / 2f, size.y / 2f);
        float z = Random.Range(-size.z / 2f, size.z / 2f);
        
        return center + zone.transform.TransformVector(new Vector3(x, y, z));
    }
    
    /// <summary>
    /// Called when a tank enters disposal zone
    /// </summary>
    public void OnTankDisposed(OxygenTank tank)
    {
        if (!activeTanks.Contains(tank)) return;
        
        disposedCount++;
        activeTanks.Remove(tank);
        Destroy(tank.gameObject);

        Debug.Log($"[OxygenPuzzle] Tank disposed: {disposedCount}/{targetTankCount}");
        
        // Check if puzzle complete
        if (disposedCount >= targetTankCount)
        {
            OnPuzzleSolved();
        }
    }
    
    /// <summary>
    /// Called when puzzle is solved
    /// </summary>
    void OnPuzzleSolved()
    {
        // Fill room storage
        if (oxygenRoomController != null)
        {
            oxygenRoomController.FillStorage();
        }
        
        UpdateDisposalZoneVisual(false);
        Debug.Log("[OxygenPuzzle] Puzzle solved!");
    }
    
    /// <summary>
    /// Update disposal zone visual feedback
    /// </summary>
    void UpdateDisposalZoneVisual(bool active)
    {
        if (disposalZoneRenderer != null)
        {
            Material mat = disposalZoneRenderer.material;
            mat.color = active ? activeZoneColor : inactiveZoneColor;
            
            // Make semi-transparent
            Color c = mat.color;
            c.a = 0.3f;
            mat.color = c;
        }
    }
    
    /// <summary>
    /// Draw zones in editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw spawn zone
        if (spawnZone != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = spawnZone.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(spawnZone.center, spawnZone.size);
        }
        
        // Draw disposal zone
        if (disposalZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = disposalZone.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(disposalZone.center, disposalZone.size);
        }
    }
}

/// <summary>
/// Helper component for disposal zone trigger
/// </summary>
public class DisposalZoneTrigger : MonoBehaviour
{
    private OxygenPuzzle puzzle;
    
    public void Initialize(OxygenPuzzle puzzleRef)
    {
        puzzle = puzzleRef;
    }
    
    void OnTriggerEnter(Collider other)
    {
        OxygenTank tank = other.GetComponent<OxygenTank>();
        if (tank != null && puzzle != null)
        {
            puzzle.OnTankDisposed(tank);
        }
    }
}