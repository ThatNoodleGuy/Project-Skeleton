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

    [Header("Isometric Camera")]
    [SerializeField] private Transform cameraTransform; // read-only: used for camera-relative WASD, never written to

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Click-to-Move")]
    [SerializeField] private float clickMoveStopDistance = 0.2f;
    private Vector3[] clickMovePath;
    private int clickMovePathIndex;
    private bool clickMoveActive;

    [Header("Movement Settings")]
    [SerializeField] private float defaultMovementSpeed = 4f;  // ADDED!
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float airControl = 0.3f;
    private float currentMovementSpeed;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetVelocity = Vector3.zero;
    private Vector3 inputDirection = Vector3.zero;
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

        // Camera lookup (read-only reference for camera-relative movement)
        if (cameraTransform == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("PlayerCamera");
            if (cameraObj != null)
            {
                cameraTransform = cameraObj.transform;
            }
            else
            {
                cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (cameraObj != null)
                {
                    cameraTransform = cameraObj.transform;
                }
            }
        }

        if (cameraTransform == null)
        {
            Debug.LogError("PlayerMovement: Could not find camera!");
        }

        // CRITICAL FIX: Initialize speeds!
        currentMovementSpeed = defaultMovementSpeed;

        Debug.Log($"[PlayerMovement] Initialized - Movement Speed: {currentMovementSpeed}");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleMovementInput();
        HandleFacing();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
    }

    /// <summary>
    /// Called by ClickToMove with a NavMesh corner path to the clicked point.
    /// WASD input always takes priority over an active click-move path.
    /// corners[0] is the start position (per NavMesh.CalculatePath convention);
    /// walking begins toward corners[1].
    /// </summary>
    public void SetClickMovePath(Vector3[] corners)
    {
        if (corners == null || corners.Length < 2)
        {
            clickMoveActive = false;
            return;
        }

        clickMovePath = corners;
        clickMovePathIndex = 1;
        clickMoveActive = true;
    }

    /// <summary>
    /// Get movement input (WASD relative to camera, or an active click-move target)
    /// and calculate target velocity.
    /// </summary>
    private void HandleMovementInput()
    {
        // Check if sprinting
        IsSprinting = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = IsSprinting ? currentMovementSpeed * sprintMultiplier : currentMovementSpeed;

        // Get input
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector3 wasdDirection = Vector3.zero;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            wasdDirection = camForward * vertical + camRight * horizontal;
        }

        if (wasdDirection.sqrMagnitude > 0.0001f)
        {
            // WASD always overrides an active click-move target
            clickMoveActive = false;
            inputDirection = wasdDirection.normalized;
        }
        else if (clickMoveActive)
        {
            Vector3 toCorner = clickMovePath[clickMovePathIndex] - transform.position;
            toCorner.y = 0f;

            if (toCorner.magnitude < clickMoveStopDistance)
            {
                clickMovePathIndex++;
                if (clickMovePathIndex >= clickMovePath.Length)
                {
                    clickMoveActive = false;
                    inputDirection = Vector3.zero;
                }
                else
                {
                    toCorner = clickMovePath[clickMovePathIndex] - transform.position;
                    toCorner.y = 0f;
                    inputDirection = toCorner.normalized;
                }
            }
            else
            {
                inputDirection = toCorner.normalized;
            }
        }
        else
        {
            inputDirection = Vector3.zero;
        }

        targetVelocity = inputDirection * targetSpeed;

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x,
                Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(Physics.gravity.y)),
                rb.linearVelocity.z);
        }

        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f || clickMoveActive)
        {
            if (StationManager.Instance != null && StationManager.Instance.ShiftInProgress)
            {
                StationManager.Instance.CurrentShift.NotifyPlayerActivity();
            }
        }
    }

    /// <summary>
    /// Turn the player body to face the current movement direction.
    /// </summary>
    private void HandleFacing()
    {
        if (inputDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
