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

    private Light lightObject;

    // Public property if other scripts need to check light state
    public bool IsHighIntensity => lightObject != null && lightObject.intensity == higherLightIntensity;
    public float CurrentIntensity => lightObject != null ? lightObject.intensity : 0f;

    private void Start()
    {
        lightObject = GetComponentInChildren<Light>();
        if (lightObject != null)
        {
            lightObject.intensity = startingLightIntensity;
        }
        else
        {
            Debug.LogWarning("Flashlight: No LightObject component found in children!");
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
        if (GetComponent<Light>() == null) return;
        
        // Play sound
        if (playerAudio != null && flashlightClick != null)
        {
            playerAudio.PlayOneShot(flashlightClick);
        }

        // Toggle flashlight intensity
        if (GetComponent<Light>().intensity == startingLightIntensity)
        {
            GetComponent<Light>().intensity = higherLightIntensity;
        }
        else
        {
            GetComponent<Light>().intensity = startingLightIntensity;
        }
    }

    /// <summary>
    /// Programmatically set the flashlight to high intensity
    /// </summary>
    public void SetHighIntensity()
    {
        if (GetComponent<Light>() != null)
        {
            GetComponent<Light>().intensity = higherLightIntensity;
        }
    }

    /// <summary>
    /// Programmatically set the flashlight to low intensity
    /// </summary>
    public void SetLowIntensity()
    {
        if (GetComponent<Light>() != null)
        {
            GetComponent<Light>().intensity = startingLightIntensity;
        }
    }
}