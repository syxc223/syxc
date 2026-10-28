using UnityEngine;

/// <summary>Emits a laser beam that can hit receivers and redirectors.</summary>
public class LaserEmitter : MonoBehaviour
{
    [SerializeField] float maxRange = 30f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] Color beamColor = Color.red;
    [SerializeField] float beamWidth = 0.08f;
    [SerializeField] bool startActive = true;

    LineRenderer _lineRenderer;
    bool _isActive;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
            _lineRenderer = gameObject.AddComponent<LineRenderer>();

        _lineRenderer.startWidth = beamWidth;
        _lineRenderer.endWidth = beamWidth;
        _lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _lineRenderer.startColor = beamColor;
        _lineRenderer.endColor = beamColor;
        _lineRenderer.enabled = startActive;
        _isActive = startActive;
    }

    void Update()
    {
        if (!_isActive) return;
        CastLaser();
    }

    void CastLaser()
    {
        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;
        Vector3 currentPos = origin;
        Vector3 currentDir = dir;

        _lineRenderer.positionCount = 1;
        _lineRenderer.SetPosition(0, origin);

        int bounces = 0;
        int maxBounces = 10;

        while (bounces < maxBounces)
        {
            if (Physics.Raycast(currentPos, currentDir, out RaycastHit hit, maxRange, hitMask))
            {
                bounces++;
                _lineRenderer.positionCount++;
                _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, hit.point);

                // Check for receiver
                var receiver = hit.collider.GetComponent<LaserReceiver>();
                if (receiver != null)
                {
                    receiver.SetHit(true);
                    break;
                }

                // Check for redirector (mirror)
                var redirector = hit.collider.GetComponent<LaserRedirector>();
                if (redirector != null)
                {
                    currentPos = hit.point + redirector.GetReflectionNormal(hit) * 0.01f;
                    currentDir = redirector.GetReflectionDirection(currentDir, hit);
                    continue;
                }

                // Hit a wall, stop
                break;
            }
            else
            {
                _lineRenderer.positionCount++;
                _lineRenderer.SetPosition(_lineRenderer.positionCount - 1,
                    currentPos + currentDir * maxRange);
                break;
            }
        }

        // Reset all receivers we're NOT hitting
        var allReceivers = FindObjectsByType<LaserReceiver>(FindObjectsSortMode.None);
        foreach (var r in allReceivers)
        {
            if (r != null)
                r.CheckStillHit();
        }
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        _lineRenderer.enabled = active;
    }
}
