using UnityEngine;

/// <summary>
/// Fixed-angle isometric camera: follows target with a constant world-space offset
/// and (optionally) looks at it, instead of being parented to the player (which
/// would otherwise inherit the player's movement-facing rotation).
/// </summary>
public class IsometricCameraRig : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -4f);
    [SerializeField] private float followDamping = 8f;
    [SerializeField] private bool lookAtTarget = true;

    void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followDamping * Time.deltaTime);

        if (lookAtTarget)
        {
            Vector3 lookDir = target.position - transform.position;
            if (lookDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }
    }
}
