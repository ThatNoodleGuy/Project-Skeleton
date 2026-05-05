using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralConsumption : MonoBehaviour
{
    [SerializeField] private bool usePassiveO2;
    [SerializeField] private bool usePassivePower;
    [SerializeField] private GameObject[] lights;
    StationManager stationManager;
    [SerializeField] private float breatheDrain;
    [SerializeField] private float powerDrain;

    private float valueToDrainFast = 100f;
    private float valueToStop = 0;
    private float valueToDrainSlow = 0.05f;

    private void Start()
    {
        lights = GameObject.FindGameObjectsWithTag("RoomLight");
        stationManager = StationManager.Instance;
    }

    private void Update()
    {
        StationManager sm = StationManager.Instance;
        if (sm == null || sm.PowerStorage == null || sm.OxygenStorage == null)
            return;

        if (usePassiveO2)
        {
            Breath();
        }

        if (usePassivePower)
        {
            UsePower();
        }

        if (sm.PowerStorage.amount <= 0)
        {
            LightsOff();
        }
        else
        {
            LightsOn();
        }

        if (Input.GetKey(KeyCode.Q))
        {
            breatheDrain = valueToDrainFast;
            powerDrain = valueToDrainFast;
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            breatheDrain = valueToStop;
            powerDrain = valueToStop;
        }
        else
        {
            breatheDrain = valueToDrainSlow;
            powerDrain = valueToDrainSlow;
        }
    }

public void Breath()
{
    float oxygenThisFrame = Time.deltaTime * breatheDrain;
    StationManager sm = StationManager.Instance;
    if (sm == null || sm.OxygenStorage == null) return;
    sm.OxygenStorage.amount -= oxygenThisFrame;
    RecordShiftResourcesIfActive(0f, oxygenThisFrame);
}

public void UsePower()
{
    StationManager sm = StationManager.Instance;
    if (sm == null || sm.PowerStorage == null) return;
    float powerThisFrame = Time.deltaTime * powerDrain * lights.Length;
    sm.PowerStorage.amount -= powerThisFrame;
    RecordShiftResourcesIfActive(powerThisFrame, 0f);
}

void RecordShiftResourcesIfActive(float powerConsumed, float oxygenConsumed)
{
    StationManager sm = StationManager.Instance;
    if (sm != null && sm.ShiftInProgress)
        sm.CurrentShift.RecordResourcesConsumed(powerConsumed, oxygenConsumed);
}

    public void LightsOn()
    {
        foreach (var item in lights)
        {
            item.SetActive(true);
        }
    }

    public void LightsOff()
    {
        foreach (var item in lights)
        {
            item.SetActive(false);
        }
    }
}