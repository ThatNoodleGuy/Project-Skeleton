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
        goToPCScreenText = GameObject.Find("goToPCScreenText");
        SetPlayerCamera();
    }

    private void Update()
    {
        if (NearerstPos() != null)
        {
            goToPCScreenText.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                goToPCScreenText.SetActive(false);
                playerAudioSource.PlayOneShot(pcOn);
                SetCamera();
            }
        }
        else
        {
            goToPCScreenText.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPlayerCamera();
        }
    }

    public void SetPlayerCamera()
    {
        if (!playerCamera.gameObject.activeInHierarchy)
            playerAudioSource.PlayOneShot(pcOff);

        goToPCScreenText.SetActive(false);
        screenCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        player.GetComponent<PlayerMovement>().enabled = true;
        playerUI.SetActive(true);
        // stationManager.isHomeScreen = true;
    }

    public Transform NearerstPos()
    {
        Transform nearPos = null;
        float minDis = 3;

        foreach (var item in camPos)
        {
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
        playerCamera.gameObject.SetActive(false);
        screenCamera.gameObject.SetActive(true);

        if (NearerstPos() != null)
        {
            screenCamera.transform.position = NearerstPos().position;
            screenCamera.transform.rotation = NearerstPos().rotation;

        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        player.GetComponent<PlayerMovement>().enabled = false;//.gameObject.SetActive(false);
        playerUI.SetActive(false);
    }

}
