using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamerasManager : MonoBehaviour
{
    StationManager stationManager;
    public AudioSource playerAudioSource;
    public AudioClip pcOn;
    public AudioClip pcOff;

    public Camera playerCamera;
    public Camera screenCamera;
    public Transform player;
    public GameObject playerUI;
    public GameObject goToPCScreenText;
    public float checkDistance = 3;
    public Transform[] camPos = new Transform[3];

    private void Start()
    {
        stationManager = StationManager.Instance;
        if (goToPCScreenText == null)
            goToPCScreenText = FindByNameIncludingInactive("goToPCScreenText");
        SetPlayerCamera();
    }

    private void Update()
    {
        Transform nearest = NearerstPos();
        if (nearest != null)
        {
            SetPromptVisible(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                SetPromptVisible(false);
                if (playerAudioSource != null && pcOn != null)
                    playerAudioSource.PlayOneShot(pcOn);
                SetCamera();
            }
        }
        else
        {
            SetPromptVisible(false);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPlayerCamera();
        }
    }

    public void SetPlayerCamera()
    {
        if (playerCamera != null && !playerCamera.gameObject.activeInHierarchy && playerAudioSource != null && pcOff != null)
            playerAudioSource.PlayOneShot(pcOff);

        SetPromptVisible(false);
        if (screenCamera != null) screenCamera.gameObject.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = true;
        }
        if (playerUI != null) playerUI.SetActive(true);
        // stationManager.isHomeScreen = true;
    }

    public Transform NearerstPos()
    {
        Transform nearPos = null;
        float minDis = 3;

        foreach (var item in camPos)
        {
            if (item == null || player == null) continue;
            float distance = Vector3.Distance(item.transform.position, player.position);

            if (distance < checkDistance)
            {
                minDis = distance;
                nearPos = item;
            }

        }

        return nearPos;
    }

    public void SetCamera()
    {
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (screenCamera != null) screenCamera.gameObject.SetActive(true);

        Transform nearest = NearerstPos();
        if (nearest != null && screenCamera != null)
        {
            screenCamera.transform.position = nearest.position;
            screenCamera.transform.rotation = nearest.rotation;

        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }
        if (playerUI != null) playerUI.SetActive(false);
    }

    private void SetPromptVisible(bool visible)
    {
        if (goToPCScreenText != null)
            goToPCScreenText.SetActive(visible);
    }

    private static GameObject FindByNameIncludingInactive(string objectName)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            var go = all[i];
            if (go == null) continue;
            if (go.hideFlags != HideFlags.None) continue;
            if (!go.scene.IsValid()) continue;
            if (go.name == objectName)
                return go;
        }
        return null;
    }

}
