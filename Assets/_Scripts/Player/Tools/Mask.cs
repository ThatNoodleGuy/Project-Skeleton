using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mask : MonoBehaviour
{
	public int level = 1;

	public int baseTime = 20;
	public int roomTimer;
	public int baseCost = 30;
	public int upgradeCost;

	private void Update()
	{
		roomTimer = baseTime + level - 1;
		upgradeCost = level + baseCost - 1;
	}

	public void UpgradeMask()
	{
		level++;
	}

}
