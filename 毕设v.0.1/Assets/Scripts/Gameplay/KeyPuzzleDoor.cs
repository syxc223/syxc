using UnityEngine;

/// <summary>
/// The "locked" gate of the key puzzle room.
/// Before the team has searched the whole room it refuses to budge ("locked").
/// Once KeyPuzzleManager reveals the wall hint, interacting uncovers the truth —
/// the lock is only painted on, the door was never locked — and it swings open,
/// clearing the path to the two exit zones.
/// A plain MonoBehaviour: state lives in KeyPuzzleManager (poll pattern).
/// </summary>
[RequireComponent(typeof(Collider))]
public class KeyPuzzleDoor : MonoBehaviour, IRoomInteractable
{
    [Header("Identity")]
    [Tooltip("场景内唯一编号（与 KeyPuzzleManager._door 对应）")]
    [SerializeField] int _targetId = 100;
    [SerializeField] string _displayName = "大门";
    [SerializeField] float _interactRange = 3f;
    [SerializeField] float _toastDuration = 7f;

    [Header("Opening")]
    [Tooltip("门绕铰链打开的角度（负值=朝出口侧摆，符号以运行效果为准）")]
    [SerializeField] float _openAngle = -110f;
    [SerializeField] float _animSpeed = 2.5f;

    [Header("Lines")]
    [SerializeField, TextArea(2, 5)] string[] _lockedLines =
    {
        "门纹丝不动——门闩上挂着一把大锁，锁孔黑洞洞的。",
        "你拽了拽那把锁，纹丝不动。看来真的得找到钥匙才行。",
        "还是打不开……"
    };
    [SerializeField, TextArea(3, 6)] string _revealLine =
        "你凑近端详那把大锁——咦？锁孔里根本没有锁芯，这把“锁”只是画上去的！\n你轻轻一推，门“吱呀”一声开了。\n<size=70%>原来，这扇门从来就没有锁过。</size>";
    [SerializeField] string _alreadyOpenLine = "门已经开了，快走吧。";

    Vector3 _closedPos;
    Quaternion _closedRot;
    Vector3 _hingePoint;
    float _openT;      // 0..1 animation progress
    bool _localOpened; // manager state mirror

    public int TargetId => _targetId;
    public Transform InteractTransform => transform;
    public float InteractRange => _interactRange;
    public string DisplayName => _displayName;
    public string InteractVerb => "检查";
    public bool IsAvailable => true;
    public string SearchKey => ""; // the door itself never counts as a search
    public float ToastDuration => _toastDuration;
    public int LockedLineCount => _lockedLines.Length;
    public string RevealLine => _revealLine;
    public string AlreadyOpenLine => _alreadyOpenLine;

    public string GetLockedLine(int index) => _lockedLines[Mathf.Clamp(index, 0, _lockedLines.Length - 1)];

    void Awake()
    {
        _closedPos = transform.position;
        _closedRot = transform.rotation;

        // Hinge = the door's west edge (bounds.min.x); pivot height is irrelevant for Y rotation
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Bounds b = rend.bounds;
            _hingePoint = new Vector3(b.min.x, _closedPos.y, b.center.z);
        }
        else
        {
            _hingePoint = _closedPos;
        }
    }

    void OnEnable()
    {
        KeyPuzzleManager.Registry.Add(this);
        KeyPuzzleManager.ById[_targetId] = this;
    }

    void OnDisable()
    {
        KeyPuzzleManager.Registry.Remove(this);
        if (KeyPuzzleManager.ById.TryGetValue(_targetId, out var cur) && ReferenceEquals(cur, this))
            KeyPuzzleManager.ById.Remove(_targetId);
    }

    public void RequestInteract()
    {
        KeyPuzzleManager.Instance?.RequestDoorInteract(_targetId);
    }

    public string AdvanceLine() => _alreadyOpenLine; // unused; door lines come from the manager

    public void ShowInteractResult(ulong senderId, string line)
    {
        GameHUD.Instance?.ShowToast($"P{senderId + 1}：{line}", _toastDuration);
    }

    void Update()
    {
        var mgr = KeyPuzzleManager.Instance;
        bool opened = mgr != null && mgr.IsSpawned && mgr.DoorOpened;
        if (opened != _localOpened)
        {
            _localOpened = opened;
            OnOpenedTransition(opened);
        }

        if (!_localOpened || _openT >= 1f) return;

        // All clients animate the same parametric swing around the hinge point
        _openT = Mathf.MoveTowards(_openT, 1f, _animSpeed * Time.deltaTime);
        float angle = _openAngle * _openT;

        Quaternion delta = Quaternion.AngleAxis(angle, Vector3.up);
        transform.SetPositionAndRotation(
            _hingePoint + delta * (_closedPos - _hingePoint),
            delta * _closedRot);
    }

    void OnOpenedTransition(bool opened)
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = !opened;

        AudioManager.Instance?.PlaySfx(opened ? "DoorOpen" : "DoorClose");
    }
}
