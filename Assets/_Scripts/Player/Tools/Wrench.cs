using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wrench : MonoBehaviour
{
    [Header("Points")]
    [SerializeField] private int addPoints = 10;
    
    private Animator animator;

    // Public property for reading points value
    public int AddPoints => addPoints;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Wrench: No Animator found in children!");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Swing();
        }
    }

    private void Swing()
    {
        if (animator != null)
        {
            animator.SetTrigger("Swing");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Box"))
        {
            StationManager.Instance.AddPoints(addPoints);
            Destroy(collision.gameObject); // Destroy the box, not the wrench!
        }
    }
}