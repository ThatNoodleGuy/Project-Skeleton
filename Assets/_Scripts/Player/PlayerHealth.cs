using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages player health, including damage from oxygen deprivation.
/// FIXED: Removed GameManager reference (script was deleted)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("UI")]
    public Slider healthBar;
    
    [Header("Health Settings")]
    public int healthAmount = 100;
    public float currentHealth;
    public float decreaseHealthBy = 1f;
    
    // Calculated value
    [HideInInspector] public float missingHealth;
    
    // Component reference
    private PlayerOxygen playerOxygen;
    
    void Start()
    {
        currentHealth = healthAmount;
        playerOxygen = GetComponent<PlayerOxygen>();
        
        if (playerOxygen == null)
        {
            Debug.LogWarning("PlayerHealth: No PlayerOxygen component found!");
        }
    }
    
    void Update()
    {
        // Calculate missing health for heal cost
        missingHealth = healthAmount - currentHealth;
        
        // Take damage when out of oxygen
        if (playerOxygen != null && playerOxygen.CurrentOxygen <= 0)
        {
            LooseHP();
        }
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
        
        // Clamp health
        currentHealth = Mathf.Clamp(currentHealth, 0, healthAmount);
        
        // Check for death
        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }
    
    /// <summary>
    /// Take instant damage
    /// </summary>
    public void takeDamage(float amount)
    {
        currentHealth -= amount;
    }
    
    /// <summary>
    /// Gradually lose health (per frame)
    /// </summary>
    public void LooseHP()
    {
        currentHealth -= decreaseHealthBy * Time.deltaTime;
    }
    
    /// <summary>
    /// Heal to full health
    /// </summary>
    public void HealToFull()
    {
        currentHealth = healthAmount;
    }
    
    /// <summary>
    /// Heal specific amount
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, healthAmount);
    }
    
    /// <summary>
    /// Called when player dies
    /// </summary>
    void OnDeath()
    {
        // TODO: Implement death logic
        // For now, just log
        Debug.Log("Player died!");
        
        // Could reload scene, show game over, etc.
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}