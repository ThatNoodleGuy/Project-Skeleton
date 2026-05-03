using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Storage : MonoBehaviour
{
	//public string id;
	public int level;
	public float reqAmount;
	public float maxAmount;
	public float amount;
	[HideInInspector] public float amountPerc;
	public float upgradePerc = 1.2f;
	public float upgradeCost;
	public float baseCost = 200;

	public void UpgradeAndFillStorage()
	{
		maxAmount *= upgradePerc;
		amount = maxAmount;
		level++;
	}
	private void Start()
	{
		amount = maxAmount;
	}
	private void Update()
	{
		//calculate upgrade cost
		upgradeCost = baseCost * level;
		//calculate fill ratio between 0 and 1
		amountPerc = amount / maxAmount;

		//cant overload
		if (amount > maxAmount)
		{
			amount = maxAmount;
		}
		//cant go below 0
		if (amount <= 0)
		{
			amount = 0;
		}
	}
}
