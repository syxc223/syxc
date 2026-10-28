using UnityEngine;

/// <summary>Marks surfaces (walls, floors) that can receive a portal.</summary>
[RequireComponent(typeof(Collider))]
public class PortalSurface : MonoBehaviour
{
    public bool CanAcceptPortal(Vector3 hitNormal, Vector3 rayDir)
    {
        // Accept if the surface normal faces toward the shooter
        return Vector3.Dot(hitNormal, -rayDir) > 0.1f;
    }
}
