using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkStation : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [SerializeField] private int level = 1;
    [SerializeField] private float addPoints = 3f;
    [SerializeField] private float upgradePerc = 3.5f;
    [SerializeField] private float baseCost = 500f;
    
    // Runtime calculated values (private)
    private float upgradeCost;
    private StationManager stationManager;
    
    // Public properties (for other scripts to read)
    public int Level => level;
    public float AddPoints => addPoints;
    public float UpgradeCost => upgradeCost;
	public float UpgradePerc => upgradePerc;
	public float BaseCost => baseCost;
    
    private void Start()
    {
        stationManager = StationManager.Instance;
    }

    private void Update()
    {
        // Calculate upgrade cost based on level
        upgradeCost = baseCost * level;
    }

    public void UpgradeWorkStation()
    {
        addPoints *= upgradePerc * level;
        level++;
    }

    public void Work()
    {
        // Consume resources from all rooms
        foreach (var room in StationManager.Instance.Rooms)
        {
            if (room != null && room.myTank != null)
            {
                room.myTank.amount -= room.myTank.reqAmount * level;
            }
        }
        
        // Add points to player balance
        StationManager.Instance.AddPoints(addPoints);
    }

	public void SetLevel(int lvl)
	{
		level = lvl;
	}



}