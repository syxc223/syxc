using UnityEngine;

/// <summary>
/// A searchable object in the key puzzle room (柜子/桌子/壁画/箱子/NPC/文字…).
/// A plain MonoBehaviour: all networking relays through KeyPuzzleManager.
/// Co-op: targets can be assigned to one player only (_playerOnly) so the team has
/// to split up, and "key clue" targets can show their lines to the finder only
/// (_privateLines), forcing the two players to talk to each other.
/// </summary>
public class InvestigationTarget : MonoBehaviour, IRoomInteractable
{
    [Header("Identity")]
    [Tooltip("场景内唯一编号（用于 RPC 路由）")]
    [SerializeField] int _targetId;
    [SerializeField] string _displayName = "箱子";
    [Tooltip("按 E 提示中的动作，如：调查 / 对话 / 查看")]
    [SerializeField] string _interactVerb = "调查";
    [Tooltip("非空时以此名字作为发言者（NPC 名字）；留空则用 P1/P2 叙述")]
    [SerializeField] string _speakerPrefix = "";
    [SerializeField] bool _requiredForHint = true;

    [Header("Co-op Assignment")]
    [Tooltip("谁看得懂这个目标：0 = 仅 P1（Host），1 = 仅 P2（Client），2 = 两人皆可。\n分头调查是第一关的分工教学：读错对象会得到引导语。")]
    [SerializeField] int _playerOnly = 2;
    [Tooltip("结果只显示给调查者本人（关键线索用）——其他人必须听他转述才能知道")]
    [SerializeField] bool _privateLines = false;

    [Header("Interaction")]
    [SerializeField] float _interactRange = 2.5f;
    [SerializeField] float _toastDuration = 4.5f;

    [Header("Lines (progressive, last line repeats)")]
    [SerializeField, TextArea(2, 5)] string[] _lines;

    // Server-side progression index (clients get the resolved line via ClientRpc)
    int _serverLineIndex;

    public int TargetId => _targetId;
    public Transform InteractTransform => transform;
    public float InteractRange => _interactRange;
    public string DisplayName => _displayName;
    public string InteractVerb => _interactVerb;
    public bool IsAvailable => _lines != null && _lines.Length > 0;
    public bool IsRequiredForHint => _requiredForHint;
    /// <summary>0 = 仅 P1，1 = 仅 P2，2 = 两人皆可</summary>
    public int PlayerOnly => _playerOnly;
    /// <summary>true = 结果只显示给调查者本人（关键线索）</summary>
    public bool PrivateLines => _privateLines;
    /// <summary>playerIndex 与 _playerOnly 匹配（2 = 皆可）。</summary>
    public bool CanBeReadBy(int playerIndex) => _playerOnly == 2 || _playerOnly == playerIndex;
    public string SearchKey => $"{_displayName}#{_targetId}";

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
        KeyPuzzleManager.Instance?.RequestInvestigate(_targetId);
    }

    public string AdvanceLine()
    {
        int idx = Mathf.Clamp(_serverLineIndex, 0, _lines.Length - 1);
        if (_serverLineIndex < _lines.Length - 1)
            _serverLineIndex++;
        return _lines[idx];
    }

    public void ShowInteractResult(ulong senderId, string line)
    {
        string speaker = string.IsNullOrEmpty(_speakerPrefix)
            ? $"P{senderId + 1}"
            : _speakerPrefix;
        WorldDialogueBox.Show(speaker, line, _toastDuration);
        AudioManager.Instance?.PlaySfx("CubePickup");
    }
}
