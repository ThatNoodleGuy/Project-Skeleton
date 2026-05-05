using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
	AudioSource audioSource;
	public AudioClip openDoor;

	public bool isLocked;  // Made public so RoomController can access it
	Transform Player;
	public float checkDistance = 4;
	public float doorSpeed = 1.3f;
	bool hasPlayedOpenDoor;

	[Header("Point-based movement")]
	[SerializeField] private Transform closedPoint;
	[SerializeField] private Transform openPoint;
	[SerializeField] private bool autoCalculateOpenPoint = true;
	[SerializeField] private float openPadding = 0.5f;

	private Vector3 closedPosition;
	private Vector3 openPosition;

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireSphere(transform.position, checkDistance);

		if (closedPoint != null && openPoint != null)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(closedPoint.position, 0.1f);
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere(openPoint.position, 0.1f);
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(closedPoint.position, openPoint.position);
		}
	}

	void Start()
	{
		audioSource = GetComponent<AudioSource>();
		Player = FindAnyObjectByType<PlayerMovement>().gameObject.transform;
		InitializeDoorPoints();
		transform.position = closedPosition;
	}

	void Update()
	{
		if (isLocked)
		{
			CloseDoor();
			return;
		}

		if (CheckPlayer())
		{
			OpenDoor();
		}
		else
		{
			CloseDoor();
		}
	}

	public bool CheckPlayer()
	{
		float distance = Vector3.Distance(transform.position, Player.position);

		if (distance < checkDistance)
		{
			return true;
		}
		return false;
	}

	public void OpenDoor()
	{
		transform.position = Vector3.MoveTowards(transform.position, openPosition, doorSpeed * Time.deltaTime);

		if (!hasPlayedOpenDoor)
		{
			if (audioSource != null && openDoor != null)
				audioSource.PlayOneShot(openDoor);
		}
		hasPlayedOpenDoor = true;
	}

	public void CloseDoor()
	{
		transform.position = Vector3.MoveTowards(transform.position, closedPosition, doorSpeed * Time.deltaTime);

		if (Vector3.Distance(transform.position, closedPosition) <= 0.001f)
		{
			transform.position = closedPosition;
			hasPlayedOpenDoor = false;
		}
	}

	private void InitializeDoorPoints()
	{
		if (closedPoint != null)
		{
			closedPosition = closedPoint.position;
		}
		else
		{
			closedPosition = transform.position;
		}

		if (openPoint != null)
		{
			openPosition = openPoint.position;
			return;
		}

		if (!autoCalculateOpenPoint)
		{
			openPosition = closedPosition;
			return;
		}

		float openHeight = 2f;
		BoxCollider box = GetComponent<BoxCollider>();
		if (box != null)
		{
			openHeight = box.size.y * Mathf.Abs(transform.lossyScale.y);
		}

		openPosition = closedPosition + Vector3.up * (openHeight + openPadding);
	}

	// Helper methods for external control
	public void Lock() => isLocked = true;
	public void Unlock() => isLocked = false;
}