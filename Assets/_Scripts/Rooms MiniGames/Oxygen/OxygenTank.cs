using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Individual oxygen tank - Can be picked up and moved
/// Uses physics for natural interaction
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class OxygenTank : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer tankRenderer;
    
    [Header("Physics Settings")]
    [SerializeField] private float pickupDistance = 2f;
    [SerializeField] private float holdDistance = 1.5f;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float throwUpwardForce = 2f;
    [SerializeField] private float holdSmoothSpeed = 15f;
    [SerializeField] private float rotationSmoothSpeed = 10f;
    
    private int tankIndex;
    private Rigidbody rb;
    private Collider col;
    private bool isHeld = false;
    private Transform holdPoint;
    
    // Store original physics state
    private bool originalUseGravity;
    private float originalDrag;
    private float originalAngularDrag;
    private CollisionDetectionMode originalCollisionMode;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        // Configure rigidbody
        if (rb != null)
        {
            rb.mass = 2f;
            rb.linearDamping = 1f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Store original values
            originalUseGravity = rb.useGravity;
            originalDrag = rb.linearDamping;
            originalAngularDrag = rb.angularDamping;
            originalCollisionMode = rb.collisionDetectionMode;
        }
    }
    
    void FixedUpdate()
    {
        if (isHeld && holdPoint != null)
        {
            UpdateHeldPosition();
        }
    }
    
    /// <summary>
    /// Initialize the tank
    /// </summary>
    public void Initialize(int index, Material material)
    {
        tankIndex = index;
        
        if (tankRenderer != null && material != null)
        {
            tankRenderer.material = material;
        }
    }
    
    /// <summary>
    /// Pick up the tank - FIXED: Proper kinematic setup
    /// </summary>
    public void PickUp(Transform camera)
    {
        if (isHeld) return;
        
        isHeld = true;
        holdPoint = camera;
        
        if (rb != null)
        {
            // CRITICAL: Make kinematic to prevent falling
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Keep continuous collision detection even when kinematic
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        
        // Disable collision with player layer
        if (col != null)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Default"), true);
        }
        
        Debug.Log($"[OxygenTank] Picked up - Kinematic: {rb.isKinematic}");
    }
    
    /// <summary>
    /// Drop the tank gently
    /// </summary>
    public void Drop()
    {
        if (!isHeld) return;
        
        Debug.Log("[OxygenTank] Dropping tank");
        ReleaseTank();
        
        // Give it a small downward velocity for natural drop
        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * 0.5f;
        }
    }
    
    /// <summary>
    /// Throw the tank forward with force
    /// </summary>
    public void Throw()
    {
        if (!isHeld || holdPoint == null) return;
        
        Debug.Log("[OxygenTank] Throwing tank");
        ReleaseTank();
        
        if (rb != null)
        {
            // Calculate throw direction with upward component
            Vector3 throwDirection = holdPoint.forward + Vector3.up * (throwUpwardForce / throwForce);
            throwDirection.Normalize();
            
            // Apply throw force
            rb.linearVelocity = throwDirection * throwForce;
            
            // Add some spin
            rb.angularVelocity = Random.insideUnitSphere * 2f;
        }
    }
    
    /// <summary>
    /// Force release - used for safety when script is disabled
    /// </summary>
    public void ForceRelease()
    {
        if (isHeld)
        {
            Drop();
        }
    }
    
    /// <summary>
    /// Release tank and restore physics - FIXED: Proper restoration
    /// </summary>
    private void ReleaseTank()
    {
        isHeld = false;
        holdPoint = null;
        
        if (rb != null)
        {
            // CRITICAL: Restore non-kinematic state
            rb.isKinematic = false;
            rb.useGravity = originalUseGravity;
            rb.linearDamping = originalDrag;
            rb.angularDamping = originalAngularDrag;
            rb.collisionDetectionMode = originalCollisionMode;
            
            Debug.Log($"[OxygenTank] Released - Kinematic: {rb.isKinematic}, UseGravity: {rb.useGravity}");
        }
        
        // Re-enable collisions with player
        if (col != null)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Default"), false);
        }
    }
    
    /// <summary>
    /// Smoothly update position when held - FIXED: Use MovePosition for kinematic
    /// </summary>
    void UpdateHeldPosition()
    {
        if (holdPoint == null || rb == null) return;
        
        // Calculate target position in front of camera
        Vector3 targetPos = holdPoint.position + holdPoint.forward * holdDistance;
        
        // Smooth position movement
        // CRITICAL: Use MovePosition for kinematic rigidbodies
        Vector3 newPos = Vector3.Lerp(
            rb.position,  // Use rb.position, not transform.position
            targetPos, 
            holdSmoothSpeed * Time.fixedDeltaTime
        );
        rb.MovePosition(newPos);
        
        // Smooth rotation to match hold point
        Quaternion newRot = Quaternion.Slerp(
            rb.rotation,  // Use rb.rotation, not transform.rotation
            holdPoint.rotation, 
            rotationSmoothSpeed * Time.fixedDeltaTime
        );
        rb.MoveRotation(newRot);
    }
    
    /// <summary>
    /// Check if player can interact with this tank
    /// </summary>
    public bool CanInteract(Vector3 playerPos)
    {
        if (isHeld) return false;
        
        float distance = Vector3.Distance(transform.position, playerPos);
        return distance <= pickupDistance;
    }
    
    /// <summary>
    /// Check if tank is currently being held
    /// </summary>
    public bool IsHeld()
    {
        return isHeld;
    }
    
    void OnDestroy()
    {
        // Clean up collision ignoring on destroy
        if (isHeld && col != null)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Default"), false);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (isHeld && holdPoint != null)
        {
            // Draw line to hold point
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, holdPoint.position);
            
            // Draw target position
            Vector3 targetPos = holdPoint.position + holdPoint.forward * holdDistance;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPos, 0.15f);
        }
        else
        {
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupDistance);
        }
    }
}