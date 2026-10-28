using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Portal gun attached to each player.
/// Owner can shoot two linked portals onto PortalSurfaces.
/// Portal positions are synced via NetworkVariables.
/// </summary>
public class PortalGun : NetworkBehaviour
{
    [Header("Portal Colors")]
    [SerializeField] Color primaryColor = new Color(0.2f, 0.6f, 1f);   // Blue
    [SerializeField] Color secondaryColor = new Color(1f, 0.5f, 0.1f); // Orange

    [Header("Portal Settings")]
    [SerializeField] float maxRange = 50f;
    [SerializeField] LayerMask surfaceMask = ~0;
    [SerializeField] float portalSize = 2f;

    // Network-synced portal states
    readonly NetworkVariable<Vector3> _portalAPos = new();
    readonly NetworkVariable<Quaternion> _portalARot = new();
    readonly NetworkVariable<bool> _portalAActive = new();
    readonly NetworkVariable<Vector3> _portalBPos = new();
    readonly NetworkVariable<Quaternion> _portalBRot = new();
    readonly NetworkVariable<bool> _portalBActive = new();

    Portal _portalA;
    Portal _portalB;

    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
        CreatePortalObjects();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _portalAActive.OnValueChanged += (oldVal, active) =>
        {
            _portalA.IsActive = active;
            _portalA.gameObject.SetActive(active);
        };
        _portalBActive.OnValueChanged += (oldVal, active) =>
        {
            _portalB.IsActive = active;
            _portalB.gameObject.SetActive(active);
        };
        _portalAPos.OnValueChanged += (oldVal, pos) =>
        {
            _portalA.transform.position = pos;
        };
        _portalARot.OnValueChanged += (oldVal, rot) =>
        {
            _portalA.transform.rotation = rot;
        };
        _portalBPos.OnValueChanged += (oldVal, pos) =>
        {
            _portalB.transform.position = pos;
        };
        _portalBRot.OnValueChanged += (oldVal, rot) =>
        {
            _portalB.transform.rotation = rot;
        };

        // Apply current values
        _portalA.IsActive = _portalAActive.Value;
        _portalA.gameObject.SetActive(_portalAActive.Value);
        _portalA.transform.position = _portalAPos.Value;
        _portalA.transform.rotation = _portalARot.Value;
        _portalB.IsActive = _portalBActive.Value;
        _portalB.gameObject.SetActive(_portalBActive.Value);
        _portalB.transform.position = _portalBPos.Value;
        _portalB.transform.rotation = _portalBRot.Value;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
            ShootPortal(true);
        if (Input.GetMouseButtonDown(1))
            ShootPortal(false);
    }

    void ShootPortal(bool primary)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, surfaceMask))
        {
            var surface = hit.collider.GetComponent<PortalSurface>();
            if (surface == null)
            {
                Debug.Log($"[PortalGun] Hit '{hit.collider.gameObject.name}' has no PortalSurface");
                return;
            }
            if (!surface.CanAcceptPortal(hit.normal, ray.direction))
            {
                Debug.Log($"[PortalGun] Surface rejected portal (back face)");
                return;
            }

            // Calculate portal orientation: align to surface normal
            Quaternion portalRot = Quaternion.LookRotation(hit.normal, Vector3.up);

            // Position slightly offset from surface to avoid z-fighting
            Vector3 portalPos = hit.point + hit.normal * 0.05f;

            ShootPortalServerRpc(portalPos, portalRot, primary);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfxAt(hit.point, "PortalShoot");
        }
    }

    [ServerRpc]
    void ShootPortalServerRpc(Vector3 pos, Quaternion rot, bool primary)
    {
        if (primary)
        {
            _portalAPos.Value = pos;
            _portalARot.Value = rot;
            _portalAActive.Value = true;
        }
        else
        {
            _portalBPos.Value = pos;
            _portalBRot.Value = rot;
            _portalBActive.Value = true;
        }
    }

    void CreatePortalObjects()
    {
        _portalA = CreatePortalObject("Portal_A", primaryColor);
        _portalB = CreatePortalObject("Portal_B", secondaryColor);

        _portalA.Partner = _portalB;
        _portalB.Partner = _portalA;

        _portalA.gameObject.SetActive(false);
        _portalB.gameObject.SetActive(false);
    }

    Portal CreatePortalObject(string name, Color color)
    {
        var go = new GameObject(name);

        // Visual: a ring/quad facing forward
        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(go.transform, false);
        var meshFilter = visualGo.AddComponent<MeshFilter>();
        var meshRenderer = visualGo.AddComponent<MeshRenderer>();

        // Create a ring mesh
        meshFilter.mesh = CreateRingMesh(portalSize, portalSize * 0.3f);

        // Material with URP Lit shader, emission enabled
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "PortalMat";
        mat.SetColor("_BaseColor", color);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 3f);
        meshRenderer.material = mat;

        // Trigger collider
        var triggerCol = go.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(portalSize, portalSize, 0.3f);

        var portal = go.AddComponent<Portal>();
        portal.SetColor(color);

        return portal;
    }

    Mesh CreateRingMesh(float outerRadius, float innerRadius)
    {
        Mesh mesh = new Mesh();
        int segments = 32;
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            vertices[i * 2] = new Vector3(x * outerRadius, y * outerRadius, 0);
            vertices[i * 2 + 1] = new Vector3(x * innerRadius, y * innerRadius, 0);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int vi = i * 2;
            int ni = next * 2;

            triangles[i * 6] = vi;
            triangles[i * 6 + 1] = ni;
            triangles[i * 6 + 2] = ni + 1;

            triangles[i * 6 + 3] = vi;
            triangles[i * 6 + 4] = ni + 1;
            triangles[i * 6 + 5] = vi + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
