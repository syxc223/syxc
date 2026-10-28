using MFramework.Event;
using UnityEngine;

public class PuzzleGate : MonoBehaviour, IEventReceiver<PuzzleProgressChangedEvent>, IEventReceiver<PuzzleSolvedEvent>
{
    [Header("Gate")]
    [SerializeField] private PuzzleCondition[] openConditions;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;
    [SerializeField] private Renderer gateRenderer;
    [SerializeField] private Color closedColor = new(0.55f, 0.25f, 0.15f, 1f);
    [SerializeField] private Color openColor = new(0.25f, 0.85f, 0.35f, 0.35f);

    private bool _isOpen;
    private bool _isConfigured;

    public void Configure(PuzzleCondition[] conditions, Collider collider)
    {
        openConditions = conditions;
        blockingCollider = collider;
        if (gateRenderer == null)
            gateRenderer = GetComponent<Renderer>();
        _isConfigured = true;
        RefreshGateState();
    }

    private void Awake()
    {
        if (blockingCollider == null)
            blockingCollider = GetComponent<Collider>();
        if (gateRenderer == null)
            gateRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (!_isConfigured)
            RefreshGateState();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PuzzleProgressChangedEvent>(this);
        EventBus.Subscribe<PuzzleSolvedEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PuzzleProgressChangedEvent>(this);
        EventBus.Unsubscribe<PuzzleSolvedEvent>(this);
    }

    public void OnEvent(PuzzleProgressChangedEvent evt) => RefreshGateState();
    public void OnEvent(PuzzleSolvedEvent evt) => RefreshGateState();

    private void RefreshGateState()
    {
        var service = PuzzleProgressService.Instance;
        if (service == null)
            return;

        bool shouldOpen = service.EvaluateConditions(openConditions);
        if (_isOpen == shouldOpen)
            return;

        _isOpen = shouldOpen;
        ApplyGateVisuals(_isOpen);
    }

    private void ApplyGateVisuals(bool isOpen)
    {
        if (blockingCollider != null)
            blockingCollider.enabled = !isOpen;

        if (closedVisual != null)
            closedVisual.SetActive(!isOpen);

        if (openVisual != null)
            openVisual.SetActive(isOpen);

        if (gateRenderer != null)
            gateRenderer.material.color = isOpen ? openColor : closedColor;
    }
}
