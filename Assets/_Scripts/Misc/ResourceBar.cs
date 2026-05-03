using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Universal resource bar that can display any storage type.
/// Replaces OxygenStorageBar.cs and StorageStatusBar.cs
/// </summary>
public class ResourceBar : MonoBehaviour
{
    public enum ResourceType { Power, Oxygen }
    
    [Header("Resource Settings")]
    [Tooltip("Which resource this bar displays")]
    public ResourceType resourceType = ResourceType.Power;
    
    [Header("UI References")]
    [Tooltip("The slider component that shows the fill level")]
    public Slider resourceSlider;
    
    [Tooltip("The fill image inside the slider")]
    public Image sliderFillImage;
    
    [Header("Color Settings")]
    [Tooltip("Color when resource is empty")]
    public Color emptyColor = Color.red;
    
    [Tooltip("Color when resource is full")]
    public Color fullColor = Color.green;
    
    // Private references
    private Storage targetStorage;
    private StationManager stationManager;
    
    void Start()
    {
        // Get station manager
        stationManager = StationManager.Instance;
        
        if (stationManager == null)
        {
            Debug.LogError($"ResourceBar on {gameObject.name}: StationManager.Instance is null!");
            return;
        }
        
        // Get the correct storage based on resource type
        switch (resourceType)
        {
            case ResourceType.Power:
                targetStorage = StationManager.Instance.PowerStorage;
                // Set default colors for power if not customized
                if (emptyColor == Color.red && fullColor == Color.green)
                {
                    emptyColor = Color.red;
                    fullColor = Color.yellow;
                }
                break;
                
            case ResourceType.Oxygen:
                targetStorage = StationManager.Instance.OxygenStorage;
                // Set default colors for oxygen if not customized
                if (emptyColor == Color.red && fullColor == Color.green)
                {
                    emptyColor = Color.gray;
                    fullColor = Color.cyan;
                }
                break;
        }
        
        if (targetStorage == null)
        {
            Debug.LogError($"ResourceBar on {gameObject.name}: Target storage is null for type {resourceType}!");
            return;
        }
        
        // Initialize slider
        if (resourceSlider != null)
        {
            resourceSlider.maxValue = 100f;
            resourceSlider.minValue = 0f;
        }
        else
        {
            Debug.LogWarning($"ResourceBar on {gameObject.name}: No slider assigned!");
        }
    }
    
    void Update()
    {
        // Early exit if references are null
        if (targetStorage == null || resourceSlider == null) return;
        
        // Update slider value (0-100)
        float percentage = targetStorage.amountPerc * 100f;
        resourceSlider.value = percentage;
        
        // Update fill color based on percentage
        if (sliderFillImage != null)
        {
            sliderFillImage.color = Color.Lerp(emptyColor, fullColor, percentage / 100f);
        }
    }
    
    /// <summary>
    /// Get current percentage (0-100) for external queries
    /// </summary>
    public float GetPercentage()
    {
        if (targetStorage == null) return 0f;
        return targetStorage.amountPerc * 100f;
    }
    
    /// <summary>
    /// Check if resource is critically low (below 20%)
    /// </summary>
    public bool IsCriticallyLow()
    {
        return GetPercentage() < 20f;
    }
    
    /// <summary>
    /// Check if resource is full (100%)
    /// </summary>
    public bool IsFull()
    {
        return GetPercentage() >= 100f;
    }
}