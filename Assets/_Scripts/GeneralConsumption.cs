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
		if (usePassiveO2)
		{
			Breath();
		}

		if (usePassivePower)
		{
			UsePower();
		}

		if (StationManager.Instance.PowerStorage.amount <= 0)
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
		StationManager.Instance.OxygenStorage.amount -= Time.deltaTime * breatheDrain;
	}

	public void UsePower()
	{
		StationManager.Instance.PowerStorage.amount -= Time.deltaTime * powerDrain * lights.Length;
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
