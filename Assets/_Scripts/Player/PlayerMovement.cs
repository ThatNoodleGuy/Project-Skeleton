using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicBG;
    private AudioSource audioManager;

    [Header("Camera Settings")]
    [SerializeField] private float defaultCameraSpeed = 225f;  // ADDED!
    [SerializeField] private float minCameraAngle = -85f;
    [SerializeField] private float maxCameraAngle = 85f;
    private float currentCameraSpeed;
    private float mouseX;
    private float mouseY;
    private float cameraX;
    private Transform cameraTrn;

    [Header("Movement Settings")]
    [SerializeField] private float defaultMovementSpeed = 4f;  // ADDED!
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float airControl = 0.3f;
    private float currentMovementSpeed;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetVelocity = Vector3.zero;
    private float horizontal;
    private float vertical;
    private Rigidbody rb;

    [Header("Jump Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    private bool isGrounded;

    // Properties for external access
    public bool IsGrounded => isGrounded;
    public float CurrentSpeed => currentVelocity.magnitude;
    public bool IsSprinting { get; private set; }

    void Start()
    {
        audioManager = GetComponent<AudioSource>();
        
        // Get or add Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure Rigidbody
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Camera init
        cameraX = 0;
        GameObject cameraObj = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (cameraObj != null)
        {
            cameraTrn = cameraObj.transform;
        }
        else
        {
            cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
            {
                cameraTrn = cameraObj.transform;
            }
        }
        
        if (cameraTrn == null)
        {
            Debug.LogError("PlayerMovement: Could not find camera!");
        }

        // CRITICAL FIX: Initialize speeds!
        currentCameraSpeed = defaultCameraSpeed;
        currentMovementSpeed = defaultMovementSpeed;
        
        Debug.Log($"[PlayerMovement] Initialized - Movement Speed: {currentMovementSpeed}, Camera Speed: {currentCameraSpeed}");

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleCamera();
        HandleMovementInput();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
    }

    /// <summary>
    /// Handle camera rotation with smooth mouse input
    /// </summary>
    private void HandleCamera()
    {
        // Camera rotation (left & right) - horizontal rotation on player body
        mouseX = Input.GetAxis("Mouse X") * currentCameraSpeed;
        transform.Rotate(0, mouseX * Time.deltaTime, 0);

        // Camera rotation (up & down) - vertical rotation on camera
        mouseY = Input.GetAxis("Mouse Y") * currentCameraSpeed;
        cameraX -= mouseY * Time.deltaTime;
        cameraX = Mathf.Clamp(cameraX, minCameraAngle, maxCameraAngle);
        
        if (cameraTrn != null)
        {
            cameraTrn.localRotation = Quaternion.Euler(cameraX, 0, 0);
        }
    }

    /// <summary>
    /// Get movement input and calculate target velocity
    /// </summary>
    private void HandleMovementInput()
    {
        // Check if sprinting
        IsSprinting = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = IsSprinting ? currentMovementSpeed * sprintMultiplier : currentMovementSpeed;

        // Get input
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        // Calculate target direction relative to where player is looking
        Vector3 inputDirection = (transform.forward * vertical + transform.right * horizontal).normalized;
        targetVelocity = inputDirection * targetSpeed;

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 
                Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(Physics.gravity.y)), 
                rb.linearVelocity.z);
        }
    }

    /// <summary>
    /// Check if player is grounded using sphere cast for more reliable detection
    /// </summary>
    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position - new Vector3(0, groundCheckDistance * 0.5f, 0);
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Apply smooth movement with acceleration and deceleration
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        // Choose acceleration or deceleration
        float accelerationRate = targetVelocity.magnitude > 0.01f ? acceleration : deceleration;
        
        // Reduce control in air
        if (!isGrounded)
        {
            accelerationRate *= airControl;
        }
        
        // Smoothly interpolate towards target velocity
        currentVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, 
            accelerationRate * Time.fixedDeltaTime);
        
        // Apply the new velocity while preserving vertical velocity
        rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw ground check sphere
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position - new Vector3(0, groundCheckDistance * 0.5f, 0);
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}