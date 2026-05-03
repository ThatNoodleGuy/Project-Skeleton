using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratePickups : MonoBehaviour
{
	public RoomController oxygenRoomPC;
	public GameObject prefab;
	public GameObject[] prefabArr;
	public int randomRange;
	public int maxSpawn;
	public Collider spawnArea;
	public GameObject[] objectNum;
	public int objectNumCount = 0;
	public int points;
	public int pointsTarget;
	float xPos;
	float yPos;
	float zPos;

	public void Update()
	{
		maxSpawn = oxygenRoomPC.myTank.level + 3;
		pointsTarget = oxygenRoomPC.myTank.level * 3;

		if (!oxygenRoomPC.HasRecource())
		{
			oxygenRoomPC.AlertMassage(ErrorList());
			if (objectNumCount < maxSpawn)
			{
				PlacePrefab();
			}
		}

		objectNum = GameObject.FindGameObjectsWithTag("GasTank");
		objectNumCount = objectNum.Length;

		if (points >= pointsTarget)
		{
			oxygenRoomPC.alertMsg.text = oxygenRoomPC.baseMsg;
		}
	}

	void PlacePrefab()
	{
		xPos = Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x);
		yPos = Random.Range(spawnArea.bounds.min.y, spawnArea.bounds.max.y);
		zPos = Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z);
		randomRange = Random.Range(0, prefabArr.Length);
		Instantiate(prefabArr[randomRange], new Vector3(xPos, yPos, zPos), Quaternion.identity);
		objectNumCount++;
	}

	public void PrefabDrop()
	{
		while (objectNumCount < maxSpawn)
		{
			PlacePrefab();
		}
		objectNum = GameObject.FindGameObjectsWithTag("GasTank");
		objectNumCount = objectNum.Length;
	}

	public List<string> ErrorList()
	{
		List<string> names = new List<string>();
		string title = "Alert!! Can't fill storage!\n";
		string currentPointsString = "Current points: ";
		string targetPointsString = "\nTarget points: ";
		names.Add(title);
		names.Add(currentPointsString + points.ToString());
		names.Add(targetPointsString + pointsTarget.ToString());

		return names;
	}

	public void DestroyPrefabs()
	{
		objectNum = GameObject.FindGameObjectsWithTag("GasTank");

		foreach (var item in objectNum)
		{
			Destroy(item.gameObject);
		}
		objectNum = new GameObject[0];

		points = 0;
	}
}
