using UnityEngine;

/// <summary>A mirror that reflects laser beams.</summary>
[RequireComponent(typeof(Collider))]
public class LaserRedirector : MonoBehaviour
{
    /// <summary>Returns the reflection normal at the hit point.</summary>
    public Vector3 GetReflectionNormal(RaycastHit hit)
    {
        return hit.normal;
    }

    /// <summary>Calculates the reflected direction of the incoming laser.</summary>
    public Vector3 GetReflectionDirection(Vector3 incomingDir, RaycastHit hit)
    {
        return Vector3.Reflect(incomingDir, hit.normal);
    }
}
