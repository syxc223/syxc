using UnityEngine;

/// <summary>
/// Per-scene camera bounds. Place one in a level scene and the local player's
/// follow camera will clamp its Z position inside these limits, preventing the
/// camera from crossing walls that sit behind it (north wall of Level1 etc.).
/// </summary>
public class CameraBounds : MonoBehaviour
{
    /// <summary>The bounds of the currently loaded scene (null = no clamping).</summary>
    public static CameraBounds Active { get; private set; }

    [Tooltip("相机 Z 允许的最小值（略小于此值时会顶在墙前并加大俯角）")]
    [SerializeField] float _minZ = -9999f;
    [Tooltip("相机 Z 允许的最大值")]
    [SerializeField] float _maxZ = 9999f;

    void OnEnable() => Active = this;
    void OnDisable() { if (Active == this) Active = null; }

    public float ClampZ(float z)
    {
        if (z < _minZ) z = _minZ;
        if (z > _maxZ) z = _maxZ;
        return z;
    }
}
