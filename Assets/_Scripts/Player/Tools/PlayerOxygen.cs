using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerOxygen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider oxygenBar;
    
    [Header("Oxygen Settings")]
    [SerializeField] private float oxygenAmount = 100f;
    [SerializeField] private float decreaseOxygenBy = 1f;
    
    [Header("Upgrade Settings")]
    [SerializeField] private int level = 1;
    [SerializeField] private int baseUpgradeCost = 10;
    
    // Runtime state (private)
    private float currentOxygen;
    private float amountRatio;
    private int upgradeCost;
    private StationManager stationManager;
    
    // Public properties (only what's needed externally)
    public float CurrentOxygen => currentOxygen;
    public float OxygenAmount => oxygenAmount;
    public float OxygenRatio => amountRatio;
    public int Level => level;
    public int UpgradeCost => upgradeCost;

    void Start()
    {
        currentOxygen = oxygenAmount;
        stationManager = StationManager.Instance;
    }

    public void UpgardeMaxCapacity()
    {
        oxygenAmount++;
        level++;
    }

    void Update()
    {
        // Calculate upgrade cost
        upgradeCost = level + baseUpgradeCost - 1;
        
        // Calculate oxygen ratio
        amountRatio = currentOxygen / oxygenAmount * 100f;
        
        // Update UI bar
        if (oxygenBar != null)
        {
            oxygenBar.value = amountRatio;
        }
        
        // Clamp oxygen
        currentOxygen = Mathf.Clamp(currentOxygen, 0, oxygenAmount);

        // Handle oxygen depletion/regeneration based on station oxygen
        if (StationManager.Instance.OxygenStorage.amount < 0.001f)
        {
            // No oxygen in station - deplete player oxygen
            currentOxygen -= decreaseOxygenBy * Time.deltaTime;
        }
        else
        {
            // Oxygen available - regenerate player oxygen
            currentOxygen += decreaseOxygenBy * Time.deltaTime;
        }
    }

    /// <summary>
    /// Add oxygen to the player's tank
    /// </summary>
    public void AddOxygen(float amount)
    {
        currentOxygen = Mathf.Min(currentOxygen + amount, oxygenAmount);
    }

    /// <summary>
    /// Remove oxygen from the player's tank
    /// </summary>
    public void RemoveOxygen(float amount)
    {
        currentOxygen = Mathf.Max(currentOxygen - amount, 0f);
    }

    /// <summary>
    /// Check if player has oxygen remaining
    /// </summary>
    public bool HasOxygen()
    {
        return currentOxygen > 0f;
    }

    /// <summary>
    /// Refill oxygen to maximum
    /// </summary>
    public void RefillOxygen()
    {
        currentOxygen = oxygenAmount;
    }

    public void SetLevel(int level)
    {
        this.level = level;
    }
}