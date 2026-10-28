using UnityEngine;

/// <summary>Component on objects that can be teleported through portals (players, cubes, etc.).</summary>
public class PortalableObject : MonoBehaviour
{
    [SerializeField] bool preserveMomentum = true;
    [SerializeField] float exitOffset = 0.5f;

    CharacterController _cc;
    Rigidbody _rb;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _rb = GetComponent<Rigidbody>();
    }

    /// <summary>Teleport this object from the given portal to the linked destination portal.</summary>
    public void Teleport(Portal fromPortal, Portal toPortal)
    {
        // Calculate relative position from source portal
        Vector3 relativePos = transform.position - fromPortal.transform.position;
        // Transform to local space of source portal
        Vector3 localPos = fromPortal.transform.InverseTransformDirection(relativePos);

        // Transform to world space of destination portal
        Vector3 worldPos = toPortal.transform.TransformDirection(localPos);
        Vector3 exitPos = toPortal.transform.position + worldPos
                         + toPortal.transform.forward * exitOffset;

        // Calculate exit velocity/direction
        Vector3 currentVelocity = GetCurrentVelocity();
        Vector3 exitVelocity = Vector3.zero;

        if (preserveMomentum && currentVelocity.sqrMagnitude > 0.01f)
        {
            // Transform velocity from source portal space to dest portal space
            Vector3 localVel = fromPortal.transform.InverseTransformDirection(currentVelocity);
            exitVelocity = toPortal.transform.TransformDirection(localVel);
        }

        // Apply teleport
        if (_cc != null)
        {
            // CharacterController needs special handling
            _cc.enabled = false;
            transform.position = exitPos;
            _cc.enabled = true;
        }
        else
        {
            transform.position = exitPos;
        }

        if (_rb != null)
        {
            _rb.velocity = exitVelocity;
        }
        else if (_cc != null && exitVelocity != Vector3.zero)
        {
            // For CharacterController, we'll store the exit velocity for the next Move call
            // This is handled by the player controller
            var playerMove = GetComponent<PlayerMove>();
            if (playerMove != null)
                playerMove.ApplyPortalVelocity(exitVelocity);
        }
    }

    Vector3 GetCurrentVelocity()
    {
        if (_rb != null) return _rb.velocity;
        var playerMove = GetComponent<PlayerMove>();
        if (playerMove != null) return playerMove.GetVelocity();
        return Vector3.zero;
    }
}
