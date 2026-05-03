using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource playerAudio;
    [SerializeField] private AudioClip flashlightClick;
    
    [Header("Light Settings")]
    [SerializeField] private float startingLightIntensity = 1f;
    [SerializeField] private float higherLightIntensity = 10f;

    private Light light;

    // Public property if other scripts need to check light state
    public bool IsHighIntensity => light != null && light.intensity == higherLightIntensity;
    public float CurrentIntensity => light != null ? light.intensity : 0f;

    private void Start()
    {
        light = GetComponentInChildren<Light>();
        if (light != null)
        {
            light.intensity = startingLightIntensity;
        }
        else
        {
            Debug.LogWarning("Flashlight: No Light component found in children!");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleIntensity();
        }
    }

    private void ToggleIntensity()
    {
        if (light == null) return;
        
        // Play sound
        if (playerAudio != null && flashlightClick != null)
        {
            playerAudio.PlayOneShot(flashlightClick);
        }

        // Toggle flashlight intensity
        if (light.intensity == startingLightIntensity)
        {
            light.intensity = higherLightIntensity;
        }
        else
        {
            light.intensity = startingLightIntensity;
        }
    }

    /// <summary>
    /// Programmatically set the flashlight to high intensity
    /// </summary>
    public void SetHighIntensity()
    {
        if (light != null)
        {
            light.intensity = higherLightIntensity;
        }
    }

    /// <summary>
    /// Programmatically set the flashlight to low intensity
    /// </summary>
    public void SetLowIntensity()
    {
        if (light != null)
        {
            light.intensity = startingLightIntensity;
        }
    }
}