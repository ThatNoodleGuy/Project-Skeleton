using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central station management system with shift-based gameplay
/// Manages resources, upgrades, and shift lifecycle
/// </summary>
public class StationManager : Singleton<StationManager>
{
    [Header("Resources")]
    [SerializeField] private float powerAmount;
    [SerializeField] private float oxygenAmount;
    [SerializeField] private float points = 500f;  // Starting money

    [Header("Storage References")]
    [SerializeField] private Storage powerStorage;
    [SerializeField] private Storage oxygenStorage;

    [Header("Station References")]
    [SerializeField] private WorkStation workStation;
    [SerializeField] private RoomController[] rooms;
    [SerializeField] private AIManager aiManager;

    // ===== SHIFT SYSTEM =====
    [Header("Shift System")]
    [SerializeField] private bool shiftInProgress = false;
    [SerializeField] private float shiftDuration = 600f;  // 10 minutes
    [SerializeField] private float shiftTimer = 0f;
    [SerializeField] private ShiftMetrics currentShift;
    
    [Header("Performance Review UI")]
    [SerializeField] private ShiftEvaluationUI evaluationUI;  // NEW: Single UI component
    
    [Header("Shift UI")]
    [SerializeField] private TextMeshProUGUI shiftTimerText;
    [SerializeField] private Button endShiftButton;
    // ===== END SHIFT SYSTEM =====

    [Header("Monitor UI (Main Screen)")]
    [SerializeField] private Button viewBtn;
    [SerializeField] private GameObject homeUI;
    [SerializeField] private GameObject storeUI;
    [SerializeField] private Button startBtnUI;
    [SerializeField] private GameObject workingIcon;
    [SerializeField] private TextMeshProUGUI scoreUI;
    [SerializeField] private TextMeshProUGUI powerTextUI;
    [SerializeField] private TextMeshProUGUI oxygenTextUI;
    [SerializeField] private TextMeshProUGUI workstationLvlMain;
    [SerializeField] private TextMeshProUGUI workstationCurrproductionMain;
    [SerializeField] private Slider powerSlider;
    [SerializeField] private Slider oxygenSlider;

    [Header("Store UI: Station")]
    [SerializeField] private TextMeshProUGUI powerStorageLvl;
    [SerializeField] private TextMeshProUGUI powerCurrAmount;
    [SerializeField] private TextMeshProUGUI powerNextLvlAmount;
    [SerializeField] private TextMeshProUGUI powerUpgradeCost;
    [SerializeField] private TextMeshProUGUI oxygenStorageLvl;
    [SerializeField] private TextMeshProUGUI oxygenCurrAmount;
    [SerializeField] private TextMeshProUGUI oxygenNextLvlAmount;
    [SerializeField] private TextMeshProUGUI oxygenUpgradeCost;
    [SerializeField] private TextMeshProUGUI workstationLvl;
    [SerializeField] private TextMeshProUGUI workstationCurrproduction;
    [SerializeField] private TextMeshProUGUI workstationNextLvlproduction;
    [SerializeField] private TextMeshProUGUI workStationUpgradeCost;

    [Header("Store UI: Player Gear")]
    [SerializeField] private TextMeshProUGUI healCostText;
    [SerializeField] private TextMeshProUGUI maskLvl;
    [SerializeField] private TextMeshProUGUI maskCostText;
    [SerializeField] private TextMeshProUGUI timeInRooms;
    [SerializeField] private TextMeshProUGUI oxygenBaloonCost;
    [SerializeField] private TextMeshProUGUI oxygenLvl;

    // State tracking
    [SerializeField] private bool isHomeScreen = true;
    private bool hasPlayedWorkstationOff;

    // Player references
    private Mask mask;
    private PlayerOxygen playerOxygen;
    private PlayerHealth playerHealth;

    // ===== PUBLIC PROPERTIES =====
    
    public float PowerAmount { get => powerAmount; set => powerAmount = value; }
    public float OxygenAmount { get => oxygenAmount; set => oxygenAmount = value; }
    public float Points { get => points; set => points = value; }
    
    public Storage PowerStorage => powerStorage;
    public Storage OxygenStorage => oxygenStorage;
    public WorkStation WorkStation => workStation;
    public RoomController[] Rooms => rooms;
    
    public bool IsHomeScreen { get => isHomeScreen; set => isHomeScreen = value; }
    public bool HasPlayedWorkstationOff { get => hasPlayedWorkstationOff; set => hasPlayedWorkstationOff = value; }
    
    public Mask Mask => mask;
    public PlayerOxygen PlayerOxygen => playerOxygen;
    public PlayerHealth PlayerHealth => playerHealth;
    
    // Shift system properties
    public bool ShiftInProgress => shiftInProgress;
    public ShiftMetrics CurrentShift => currentShift;
    public AIManager AIManager => aiManager;

    void Start()
    {
        // Find player references
        #if UNITY_6000_0_OR_NEWER
            playerOxygen = FindAnyObjectByType<PlayerOxygen>();
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            mask = FindAnyObjectByType<Mask>();
        #else
            playerOxygen = FindObjectOfType<PlayerOxygen>();
            playerHealth = FindObjectOfType<PlayerHealth>();
            mask = FindObjectOfType<Mask>();
        #endif

        // Find AI Manager if not assigned
        if (aiManager == null)
        {
            #if UNITY_6000_0_OR_NEWER
                aiManager = FindAnyObjectByType<AIManager>();
            #else
                aiManager = FindObjectOfType<AIManager>();
            #endif
            
            if (aiManager == null)
            {
                Debug.LogWarning("[StationManager] No AIManager found! Creating one...");
                GameObject aiObj = new GameObject("AIManager");
                aiManager = aiObj.AddComponent<AIManager>();
            }
        }
        
        // Find ShiftEvaluationUI if not assigned
        if (evaluationUI == null)
        {
            #if UNITY_6000_0_OR_NEWER
                evaluationUI = FindAnyObjectByType<ShiftEvaluationUI>();
            #else
                evaluationUI = FindObjectOfType<ShiftEvaluationUI>();
            #endif
            
            if (evaluationUI == null)
            {
                Debug.LogError("[StationManager] ShiftEvaluationUI not found! Please assign it in inspector or add to scene.");
            }
        }

        // Subscribe to events
        GameEvents.OnResourceChanged += CheckWorkStatus;
        
        // Initialize shift metrics
        currentShift = new ShiftMetrics();
        
        // Hide end shift button initially
        if (endShiftButton != null)
            endShiftButton.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameEvents.OnResourceChanged -= CheckWorkStatus;
    }

    void Update()
    {
        // Update resources from storage
        if (powerStorage != null)
            powerAmount = powerStorage.amount;
        
        if (oxygenStorage != null)
            oxygenAmount = oxygenStorage.amount;

        // Update UI based on current view
        if (scoreUI != null)
            scoreUI.text = "Balance: " + points.ToString("#,###") + "$";

        if (isHomeScreen)
        {
            UpdateHomeUI();
        }
        else
        {
            UpdateStoreUI();
        }

        // Check work status
        if (!CanWork())
        {
            StopWork();
        }
        
        // Update shift timer
        if (shiftInProgress)
        {
            UpdateShiftTimer();
        }
    }
    
    // ===== SHIFT SYSTEM METHODS =====
    
    /// <summary>
    /// Start a new shift (Player accepts shift)
    /// THIS IS THE METHOD FOR THE START BUTTON!
    /// </summary>
    public void StartShift()
    {
        if (shiftInProgress)
        {
            Debug.LogWarning("[StationManager] Shift already in progress!");
            return;
        }
        
        // Initialize shift
        currentShift.StartShift();
        shiftInProgress = true;
        shiftTimer = 0f;
        
        // UI updates
        if (startBtnUI != null)
        {
            startBtnUI.interactable = false;
            // Change button text if it has text component
            TextMeshProUGUI btnText = startBtnUI.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText == null)
            {
                // Fallback to legacy Text
                Text legacyText = startBtnUI.GetComponentInChildren<Text>();
                if (legacyText != null)
                    legacyText.text = "Shift In Progress";
            }
            else
            {
                btnText.text = "Shift In Progress";
            }
        }
        
        if (endShiftButton != null)
            endShiftButton.gameObject.SetActive(true);
        
        Debug.Log("[StationManager] === SHIFT STARTED ===");
    }
    
    /// <summary>
    /// End current shift and show performance review
    /// </summary>
    public void EndShift()
    {
        if (!shiftInProgress)
        {
            Debug.LogWarning("[StationManager] No shift in progress!");
            return;
        }
        
        // Finalize shift
        currentShift.EndShift();
        shiftInProgress = false;
        
        // Stop any ongoing work
        StopWork();
        
        // Hide end shift button
        if (endShiftButton != null)
            endShiftButton.gameObject.SetActive(false);
        
        // Get AI evaluation
        ShiftEvaluation evaluation = aiManager.EvaluateShift(currentShift);
        
        // Show performance review using new UI
        ShowPerformanceReview(evaluation);
        
        // Increment AI progression
        aiManager.IncrementShiftProgression();
        
        Debug.Log($"[StationManager] === SHIFT ENDED === Classification: {evaluation.classification}");
        Debug.Log(currentShift.GetSummary());
    }
    
    /// <summary>
    /// Update shift timer display
    /// </summary>
    void UpdateShiftTimer()
    {
        shiftTimer += Time.deltaTime;
        
        // Auto-end shift when time expires
        if (shiftTimer >= shiftDuration)
        {
            EndShift();
            return;
        }
        
        // Update timer display
        if (shiftTimerText != null)
        {
            float remaining = shiftDuration - shiftTimer;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            shiftTimerText.text = $"Shift Time: {minutes:00}:{seconds:00}";
            
            // Flash when low
            if (remaining < 60f)
            {
                float flash = Mathf.PingPong(Time.time * 2f, 1f);
                shiftTimerText.color = Color.Lerp(Color.white, Color.yellow, flash);
            }
            else
            {
                shiftTimerText.color = Color.white;
            }
        }
        
        // Update end shift button text
        if (endShiftButton != null)
        {
            TextMeshProUGUI btnText = endShiftButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText == null)
            {
                // Fallback to legacy Text
                Text legacyText = endShiftButton.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    float remaining = shiftDuration - shiftTimer;
                    int minutes = Mathf.FloorToInt(remaining / 60f);
                    int seconds = Mathf.FloorToInt(remaining % 60f);
                    legacyText.text = $"End Shift ({minutes:00}:{seconds:00})";
                }
            }
            else
            {
                float remaining = shiftDuration - shiftTimer;
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                btnText.text = $"End Shift ({minutes:00}:{seconds:00})";
            }
        }
    }
    
    /// <summary>
    /// Show performance review screen using new UI system
    /// </summary>
    void ShowPerformanceReview(ShiftEvaluation evaluation)
    {
        if (evaluationUI == null)
        {
            Debug.LogError("[StationManager] ShiftEvaluationUI not assigned! Cannot show performance review.");
            // Fallback: just continue to next shift
            ContinueToNextShift();
            return;
        }
        
        // Show the evaluation UI (it handles everything)
        evaluationUI.ShowEvaluation(evaluation);
        
        Debug.Log("[StationManager] Performance review displayed");
    }
    
    /// <summary>
    /// Continue to next shift (Groundhog Day loop)
    /// Called by the Continue button in ShiftEvaluationUI
    /// </summary>
    public void ContinueToNextShift()
    {
        // The ShiftEvaluationUI handles hiding itself and unlocking cursor
        
        // Show home UI
        if (homeUI != null) 
            homeUI.SetActive(true);
        
        // Re-enable start button
        if (startBtnUI != null)
        {
            startBtnUI.interactable = true;
            TextMeshProUGUI btnText = startBtnUI.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText == null)
            {
                // Fallback to legacy Text
                Text legacyText = startBtnUI.GetComponentInChildren<Text>();
                if (legacyText != null)
                    legacyText.text = "Accept Shift";
            }
            else
            {
                btnText.text = "Accept Shift";
            }
        }
        
        // Reset shift timer display
        if (shiftTimerText != null)
        {
            shiftTimerText.text = "No Active Shift";
            shiftTimerText.color = Color.white;
        }
        
        // NOTE: Money and upgrades persist (player progression)
        // NOTE: Resources should reset or be at current state
        
        Debug.Log("[StationManager] === READY FOR NEXT SHIFT ===");
    }
    
    // ===== END SHIFT SYSTEM METHODS =====

    // ===== WORK MANAGEMENT =====

    public bool CanWork()
    {
        if (workStation == null) return false;

        // Check if all rooms have enough resources
        if (rooms != null)
        {
            foreach (RoomController room in rooms)
            {
                if (room != null && !room.HasRecource())
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void CheckWorkStatus()
    {
        if (!CanWork())
        {
            StopWork();
        }
    }

    /// <summary>
    /// Start the work station (Power button - starts production)
    /// </summary>
    public void PowerBtn()
    {
        if (CanWork())
        {
            hasPlayedWorkstationOff = false;
            
            InvokeRepeating(nameof(StartMining), 1, 1);
            
            if (startBtnUI != null)
                startBtnUI.interactable = false;
            if (workingIcon != null)
                workingIcon.SetActive(true);
        }
    }

    public void StartMining()
    {
        if (workStation != null)
            workStation.Work();
    }

    public void StopWork()
    {
        CancelInvoke(nameof(StartMining));
        
        if (startBtnUI != null && !shiftInProgress)
            startBtnUI.interactable = true;
            
        if (workingIcon != null)
            workingIcon.SetActive(false);

        hasPlayedWorkstationOff = true;
    }
    
    /// <summary>
    /// Add points to balance (called by WorkStation)
    /// </summary>
    public void AddPoints(float amount)
    {
        points += amount;
        
        // Track for shift metrics
        if (shiftInProgress && currentShift != null)
        {
            currentShift.RecordMoneyEarned(amount);
        }
    }

    // ===== UI METHODS =====

    public void SwitchViews()
    {
        isHomeScreen = !isHomeScreen;
    }

    void UpdateHomeUI()
    {
        if (homeUI != null) homeUI.SetActive(true);
        if (storeUI != null) storeUI.SetActive(false);

        if (powerStorage != null)
        {
            float powerPerc = powerStorage.amountPerc * 100f;
            
            if (powerSlider != null)
                powerSlider.value = powerPerc;
            
            if (powerTextUI != null)
                powerTextUI.text = powerPerc.ToString("0") + "%";
        }

        if (oxygenStorage != null)
        {
            float oxygenPerc = oxygenStorage.amountPerc * 100f;
            
            if (oxygenSlider != null)
                oxygenSlider.value = oxygenPerc;
            
            if (oxygenTextUI != null)
                oxygenTextUI.text = oxygenPerc.ToString("0") + "%";
        }

        if (workstationLvlMain != null && workStation != null)
            workstationLvlMain.text = "Work Station Lvl: " + workStation.Level;
        
        if (workstationCurrproductionMain != null && workStation != null)
            workstationCurrproductionMain.text = "Production Rate: " + workStation.AddPoints.ToString("0") + "$/sec";
    }

    void UpdateStoreUI()
    {
        if (homeUI != null) homeUI.SetActive(false);
        if (storeUI != null) storeUI.SetActive(true);

        // Power Storage
        if (powerStorage != null)
        {
            if (powerStorageLvl != null)
                powerStorageLvl.text = "Power\nLvl: " + powerStorage.level;
            if (powerUpgradeCost != null)
                powerUpgradeCost.text = "Cost: " + powerStorage.upgradeCost.ToString("0") + "$";
            if (powerCurrAmount != null)
                powerCurrAmount.text = "Capacity: " + powerStorage.maxAmount.ToString("0");
            if (powerNextLvlAmount != null)
                powerNextLvlAmount.text = "Upgrade: " + (powerStorage.maxAmount * powerStorage.upgradePerc).ToString("0");
        }

        // Oxygen Storage
        if (oxygenStorage != null)
        {
            if (oxygenStorageLvl != null)
                oxygenStorageLvl.text = "O2\nLvl: " + oxygenStorage.level;
            if (oxygenUpgradeCost != null)
                oxygenUpgradeCost.text = "Cost: " + oxygenStorage.upgradeCost.ToString("0") + "$";
            if (oxygenCurrAmount != null)
                oxygenCurrAmount.text = "Capacity: " + oxygenStorage.maxAmount.ToString("0");
            if (oxygenNextLvlAmount != null)
                oxygenNextLvlAmount.text = "Upgrade: " + (oxygenStorage.maxAmount * oxygenStorage.upgradePerc).ToString("0");
        }

        // Work Station
        if (workStation != null)
        {
            if (workstationLvl != null)
                workstationLvl.text = "Work Station\nLvl: " + workStation.Level;
            if (workStationUpgradeCost != null)
                workStationUpgradeCost.text = "Cost: " + workStation.UpgradeCost.ToString("0") + "$";
            if (workstationCurrproduction != null)
                workstationCurrproduction.text = "Production: " + workStation.AddPoints.ToString("0") + "$/sec";
            if (workstationNextLvlproduction != null)
                workstationNextLvlproduction.text = "Upgrade: " + (workStation.AddPoints * workStation.UpgradePerc).ToString("0") + "$/sec";
        }

        // Player Gear
        if (playerHealth != null && healCostText != null)
            healCostText.text = "Cost: " + playerHealth.missingHealth.ToString("0") + "$";

        if (mask != null)
        {
            if (maskCostText != null)
                maskCostText.text = "Cost: " + mask.upgradeCost.ToString("0") + "$";
            if (timeInRooms != null)
                timeInRooms.text = "Room Time: " + mask.roomTimer.ToString() + "s";
            if (maskLvl != null)
                maskLvl.text = "Gas Mask\nLvl: " + mask.level;
        }

        if (playerOxygen != null)
        {
            if (oxygenBaloonCost != null)
                oxygenBaloonCost.text = "Cost: " + playerOxygen.UpgradeCost + "$";
            if (oxygenLvl != null)
                oxygenLvl.text = "Oxygen Tank\nLvl: " + playerOxygen.Level;
        }
    }

    // ===== UPGRADE METHODS =====

    public void UpgradePowerStorage()
    {
        if (powerStorage != null && points >= powerStorage.upgradeCost)
        {
            powerStorage.UpgradeAndFillStorage();
            points -= powerStorage.upgradeCost;
            GameEvents.TriggerResourceChanged();
        }
    }

    public void UpgradeOxygenStorage()
    {
        if (oxygenStorage != null && points >= oxygenStorage.upgradeCost)
        {
            oxygenStorage.UpgradeAndFillStorage();
            points -= oxygenStorage.upgradeCost;
            GameEvents.TriggerResourceChanged();
        }
    }

    public void UpgradeWorkStation()
    {
        if (workStation != null && points >= workStation.UpgradeCost)
        {
            workStation.UpgradeWorkStation();
            points -= workStation.UpgradeCost;
        }
    }

    public void Heal()
    {
        if (playerHealth != null && points >= playerHealth.missingHealth)
        {
            playerHealth.HealToFull();
            points -= playerHealth.missingHealth;
        }
    }

    public void UpgradeBaloon()
    {
        if (playerOxygen != null && points >= playerOxygen.UpgradeCost)
        {
            playerOxygen.UpgardeMaxCapacity();
            points -= playerOxygen.UpgradeCost;
        }
    }

    public void UpgradeMask()
    {
        if (mask != null && points >= mask.upgradeCost)
        {
            mask.UpgradeMask();
            points -= mask.upgradeCost;
        }
    }
}