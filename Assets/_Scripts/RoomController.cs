using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls individual rooms with contamination timer and resource management
/// Integrated with shift tracking system
/// </summary>
public class RoomController : MonoBehaviour
{
    public enum RoomType { Power, Oxygen }
    
    [Header("Room Settings")]
    public RoomType roomType;
    public Door door;
    public float contaminationTimer = 30f;
    
    [Header("Resources")]
    public Storage myTank;
    
    [Header("UI")]
    public Text timerText;
    public Text ammountPerc;
    public Text storageLvlText;
    public Button fillStorage;
    public Text alertMsg;
    public string baseMsg = "No Errors, can fill storage.";
    
    [Header("Visual Effects")]
    public Light myAlertLight;
    public float spinningSpeed = 180f;
    
    // FIXED: Timer management
    private float currentTimer;
    private bool isInRoom;
    private bool timerExpired = false;
    private bool hasRecordedExpiration = false;
    
    // Visual effects
    private float spintLight = 180f;
    public float dir;
    
    // References
    [SerializeField] private StationManager stationManager;
    [SerializeField] private WorkStation workStation;

    // ===== PROPERTIES FOR EXTERNAL ACCESS =====
    public bool isGoingOut => !isInRoom;  // For puzzle spawners
    
    void Start()
    {
        stationManager = StationManager.Instance;
        workStation = StationManager.Instance.gameObject.GetComponent<WorkStation>();
        
        // Ensure we have a trigger collider
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null)
        {
            Debug.LogWarning($"{gameObject.name} needs a Collider for player detection!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name} collider should be set to IsTrigger!");
        }
    }

    void Update()
    {
        UpdateAlertLights();
        UpdateUI();
        UpdateTimer();
        FlashTimerWhenLow();
    }

    // ===== TRIGGER DETECTION (FIXED) =====
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInRoom = true;
            currentTimer = contaminationTimer;
            timerExpired = false;
            hasRecordedExpiration = false;
            
            // SHIFT TRACKING: Record room entry
            if (stationManager != null && stationManager.ShiftInProgress)
            {
                stationManager.CurrentShift.RecordRoomEntered(roomType);
            }
            
            Debug.Log($"[RoomController] Player entered {roomType} room");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool wasCompleted = HasRecource();
            
            isInRoom = false;
            currentTimer = 0;
            timerExpired = false;
            hasRecordedExpiration = false;

            // SHIFT TRACKING: Record room exit
            if (stationManager != null && stationManager.ShiftInProgress)
            {
                stationManager.CurrentShift.RecordRoomExited(roomType, wasCompleted);
            }
            
            Debug.Log($"[RoomController] Player exited {roomType} room (Completed: {wasCompleted})");
        }
    }
    
    // ===== TIMER SYSTEM (FIXED) =====
    
    /// <summary>
    /// Update contamination timer - FIXED VERSION
    /// </summary>
    void UpdateTimer()
    {
        // Only count down when player is in room
        if (isInRoom)
        {
            currentTimer -= Time.deltaTime;
            
            // Timer expired
            if (currentTimer <= 0 && !timerExpired)
            {
                currentTimer = 0;
                timerExpired = true;
                
                // Record contamination event ONCE
                if (!hasRecordedExpiration && stationManager != null && stationManager.ShiftInProgress)
                {
                    stationManager.CurrentShift.RecordContaminationEvent(roomType);
                    hasRecordedExpiration = true;
                }
                
                // Start damaging player
                StartCoroutine(DamagePlayerFromContamination());
            }
        }
        else
        {
            // Reset timer when not in room
            currentTimer = 0;
            timerExpired = false;
            hasRecordedExpiration = false;
        }
    }
    
    /// <summary>
    /// Damage player when contamination timer expires
    /// </summary>
    IEnumerator DamagePlayerFromContamination()
    {
        float damagePerSecond = 5f;  // Adjust as needed
        
        while (isInRoom && timerExpired)
        {
            float damage = damagePerSecond * Time.deltaTime;
            
            if (stationManager != null && stationManager.PlayerHealth != null)
            {
                stationManager.PlayerHealth.takeDamage(damage);
                
                // Track health loss for shift metrics
                if (stationManager.ShiftInProgress)
                {
                    stationManager.CurrentShift.RecordHealthLoss(damage);
                }
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Flash timer text when time is running low
    /// </summary>
    void FlashTimerWhenLow()
    {
        if (timerText == null || !isInRoom) 
        {
            if (timerText != null)
                timerText.color = Color.white;
            return;
        }
        
        if (currentTimer <= 10)
        {
            // Flash faster as time runs out
            float speed = currentTimer < 5 ? 8f : 4f;
            float flash = Mathf.PingPong(Time.time * speed, 1);
            timerText.color = Color.Lerp(Color.white, Color.red, flash);
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    // ===== RESOURCE MANAGEMENT =====
    
    /// <summary>
    /// Fill this room's storage (called by button)
    /// </summary>
    public void FillStorage()
    {
        if (myTank == null) return;

        myTank.amount = myTank.maxAmount;

        // SHIFT TRACKING: Record completion
        if (stationManager != null && stationManager.ShiftInProgress)
        {
            stationManager.CurrentShift.RecordRoomCompleted(roomType);
        }
        
        // Trigger events
        GameEvents.TriggerResourceFilled(roomType);
        GameEvents.TriggerResourceChanged();
        
        Debug.Log($"[RoomController] {roomType} storage filled!");
    }
    
    /// <summary>
    /// Check if this room has resources
    /// </summary>
    public bool HasRecource()
    {
        if (myTank == null) return false;
        return myTank.amount >= myTank.reqAmount;
    }

    // ===== UI UPDATES =====
    
    void UpdateUI()
    {
        // Timer display
        if (timerText != null)
        {
            if (isInRoom)
            {
                int minutes = Mathf.FloorToInt(currentTimer / 60f);
                int seconds = Mathf.FloorToInt(currentTimer % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                timerText.text = "--:--";
            }
        }
        
        // Storage level
        if (ammountPerc != null && myTank != null)
        {
            ammountPerc.text = (myTank.amountPerc * 100).ToString("0") + "%";
        }
        
        if (storageLvlText != null && myTank != null)
        {
            storageLvlText.text = "Lvl: " + myTank.level;
        }
    }

    /// <summary>
    /// Show alert message (used by puzzle spawners)
    /// </summary>
    public void AlertMassage(List<string> errors)
    {
        if (alertMsg == null) return;
        
        if (errors.Count > 0)
        {
            alertMsg.text = "";
            foreach (string err in errors)
            {
                alertMsg.text += err;
            }
        }
        else
        {
            alertMsg.text = baseMsg;
        }
    }

    // ===== VISUAL EFFECTS =====
    
    void UpdateAlertLights()
    {
        if (myAlertLight == null) return;
        
        if (!HasRecource())
        {
            // Spin light when resource is low
            spintLight += Time.deltaTime * spinningSpeed;
            myAlertLight.transform.rotation = Quaternion.Euler(spintLight, 0, 0);
            
            if (!myAlertLight.enabled)
                myAlertLight.enabled = true;
        }
        else
        {
            if (myAlertLight.enabled)
                myAlertLight.enabled = false;
        }
    }

    // void SetLock()
    // {
    //     if (door == null) return;
        
    //     // Lock door when resources are empty
    //     if (!HasRecource())
    //     {
    //         door.locked = true;
    //     }
    //     else
    //     {
    //         door.locked = false;
    //     }
    // }
}