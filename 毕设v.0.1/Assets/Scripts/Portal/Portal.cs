using UnityEngine;

/// <summary>
/// A portal trigger zone. Linked to a partner portal.
/// When a PortalableObject enters, it teleports to the partner portal.
/// </summary>
public class Portal : MonoBehaviour
{
    [SerializeField] Color portalColor = Color.cyan;
    [SerializeField] float teleportCooldown = 0.3f;
    [SerializeField] float portalRadius = 1f;

    public Portal Partner { get; set; }
    public bool IsActive { get; set; }

    float _cooldownTimer;
    bool _justTeleported;

    void Update()
    {
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0)
                _justTeleported = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryTeleport(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryTeleport(other);
    }

    void TryTeleport(Collider other)
    {
        if (!IsActive || Partner == null || !Partner.IsActive) return;
        if (_justTeleported) return;

        var portalable = other.GetComponent<PortalableObject>();
        if (portalable == null) return;

        // Don't teleport if the partner just teleported us here
        if (Partner._justTeleported) return;

        portalable.Teleport(this, Partner);

        // Set cooldown on both portals
        _justTeleported = true;
        _cooldownTimer = teleportCooldown;
        Partner._justTeleported = true;
        Partner._cooldownTimer = teleportCooldown;

        // Play teleport sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfxAt(transform.position, "PortalEnter");
    }

    public void SetColor(Color color)
    {
        portalColor = color;
        var renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.name.Contains("Portal") || mat.name.Contains("portal"))
                {
                    mat.SetColor("_EmissionColor", color * 3f);
                    mat.SetColor("_BaseColor", color);
                }
            }
        }
    }

    public Color GetColor() => portalColor;
    public float GetRadius() => portalRadius;
}
