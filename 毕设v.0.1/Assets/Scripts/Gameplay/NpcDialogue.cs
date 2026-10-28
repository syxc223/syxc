using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// NPC dialogue system: one lobby NPC offers exactly one level. P1 (Host) talks
/// to the NPC and accepts its mission, P2 confirms, then the configured level
/// scene loads. Only P1 can interact with NPCs to start a dialogue.
/// </summary>
public class NpcDialogue : NetworkBehaviour
{
    [Header("NPC Config")]
    [SerializeField] string _npcName = "时空使者";
    [SerializeField] int _missionId = 1;

    [Header("Level Config")]
    [SerializeField] string _levelSceneName = "Level1";
    [SerializeField] string _levelTitle = "第一关：消失的钥匙";
    [SerializeField] string _levelDesc = "翻遍房间，找到离开的方法";

    [Header("Dialogue Lines")]
    [SerializeField] string[] _dialogueLines = {
        "欢迎来到时空试炼之殿。",
        "你们两位将被送入同一个房间——里面只有一扇大门，和门旁的一行字。",
        "『只有找到正确的钥匙才能离开。』",
        "房间里有一张桌子、一幅壁画、几个箱子和一口老柜子……随便你们翻，翻个底朝天也行。",
        "找不到答案的时候，就再读一遍门旁那行字——一个字都不要放过。",
        "两人都抵达出口，试炼才算完成。现在——",
        "请选择你们要挑战的任务。"
    };

    [Header("Dialogue UI")]
    [SerializeField] GameObject _dialoguePanel;
    [SerializeField] TMP_Text _speakerName;
    [SerializeField] TMP_Text _dialogueText;
    [SerializeField] Button _nextButton;

    [Header("Mission Selection")]
    [SerializeField] GameObject _missionSelectPanel;
    [SerializeField] Button _mission1Button;
    [SerializeField] TMP_Text _mission1Desc;

    [Header("Confirm UI")]
    [SerializeField] GameObject _confirmPanel;
    [SerializeField] TMP_Text _confirmText;
    [SerializeField] Button _confirmYesButton;
    [SerializeField] Button _confirmNoButton;

    [Header("Interaction")]
    [SerializeField] float _interactRange = 5f;
    [SerializeField] Transform _npcSprite;
    [SerializeField] GameObject _interactHint;

    // All lobby NPCs, so only one dialogue/mission flow can be open at a time
    // (the per-NPC UI canvases are screen-space and would overlap otherwise).
    static readonly List<NpcDialogue> _instances = new List<NpcDialogue>();

    int _dialogueIndex;
    bool _inDialogue;
    bool _networkReady;
    Transform _localPlayer;

    // Network state
    NetworkVariable<int> _selectedMission = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<int> _confirmState = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // 0 = none, 1 = waiting for P2, 2 = confirmed, 3 = rejected

    void Awake()
    {
        _instances.Add(this);

        if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
        if (_missionSelectPanel != null) _missionSelectPanel.SetActive(false);
        if (_confirmPanel != null) _confirmPanel.SetActive(false);
        if (_interactHint != null) _interactHint.SetActive(false);
        WireButtons();

        // The DialogueCanvas that holds all three panels is authored inactive in the
        // scene; without activating it the panels can never render or receive clicks.
        if (_dialoguePanel != null && _dialoguePanel.transform.parent != null)
            _dialoguePanel.transform.parent.gameObject.SetActive(true);
    }

    public override void OnDestroy()
    {
        _instances.Remove(this);
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        _networkReady = true;
        _inDialogue = false;
        _selectedMission.OnValueChanged += OnMissionSelected;
        _confirmState.OnValueChanged += OnConfirmStateChanged;

        // Only P1 (Host) starts the dialogue — P2 never sees the interact hint.
        if (!IsServer && _interactHint != null)
            _interactHint.SetActive(false);

        // Fallback reset (scene reload already resets NetworkVariables to defaults)
        if (IsServer)
        {
            _selectedMission.Value = -1;
            _confirmState.Value = 0;
        }
    }

    public override void OnNetworkDespawn()
    {
        _networkReady = false;
        _selectedMission.OnValueChanged -= OnMissionSelected;
        _confirmState.OnValueChanged -= OnConfirmStateChanged;
    }

    static bool AnyMissionPending()
    {
        foreach (var npc in _instances)
        {
            if (!npc._networkReady) continue;
            if (npc._inDialogue || npc._selectedMission.Value != -1)
                return true;
        }
        return false;
    }

    /// <summary>True when no other NPC is closer to the local player (in range ties
    /// are resolved by the pending-flow guard above).</summary>
    bool IsNearestNpc(float distToThis)
    {
        foreach (var npc in _instances)
        {
            if (npc == this || !npc._networkReady) continue;
            if (Vector3.Distance(_localPlayer.position, npc.transform.position) < distToThis)
                return false;
        }
        return true;
    }

    void Update()
    {
        // Wait until network is fully initialized
        if (!_networkReady) return;
        // Only P1 (Host) can interact
        if (!IsServer) return;

        FindLocalPlayer();
        if (_localPlayer == null)
        {
            SetInteractHint(false);
            return;
        }

        float dist = Vector3.Distance(_localPlayer.position, transform.position);
        bool canInteract = dist <= _interactRange && !_inDialogue
            && _selectedMission.Value == -1 && !ChatSystem.IsChatOpen
            && IsNearestNpc(dist) && !AnyMissionPending();

        SetInteractHint(canInteract);

        if (canInteract && Input.GetKeyDown(KeyCode.E))
            StartDialogue();
    }

    void SetInteractHint(bool show)
    {
        if (_interactHint != null && _interactHint.activeSelf != show)
            _interactHint.SetActive(show);
    }

    void FindLocalPlayer()
    {
        // Use NetworkManager to find local player object
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (playerObj != null)
                _localPlayer = playerObj.transform;
        }
    }

    void StartDialogue()
    {
        _inDialogue = true;
        _dialogueIndex = 0;
        if (_speakerName != null) _speakerName.text = _npcName;
        if (_dialoguePanel != null) _dialoguePanel.SetActive(true);
        SetInteractHint(false);
        ShowDialogueLine();
    }

    void ShowDialogueLine()
    {
        if (_dialogueText != null && _dialogueIndex < _dialogueLines.Length)
        {
            _dialogueText.text = _dialogueLines[_dialogueIndex];
        }
        else
        {
            // Dialogue finished → show mission select
            if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
            ShowMissionSelect();
        }
    }

    void ShowMissionSelect()
    {
        if (_mission1Desc != null)
            _mission1Desc.text = $"{_levelTitle}\n{_levelDesc}";
        if (_missionSelectPanel != null) _missionSelectPanel.SetActive(true);
    }

    // Called by the accept-mission button
    public void OnMissionSelected(int missionId)
    {
        SelectMissionServerRpc(missionId);
    }

    [ServerRpc(RequireOwnership = false)]
    void SelectMissionServerRpc(int missionId)
    {
        _selectedMission.Value = missionId;
        _confirmState.Value = 1; // waiting for P2
    }

    void OnMissionSelected(int oldVal, int newVal)
    {
        if (newVal < 0) return;

        if (IsServer)
        {
            // Host: hide mission select, wait for P2 confirm
            if (_missionSelectPanel != null) _missionSelectPanel.SetActive(false);
        }
        else
        {
            // P2: show confirm dialog
            if (_missionSelectPanel != null) _missionSelectPanel.SetActive(false);
            if (_confirmPanel != null) _confirmPanel.SetActive(true);
            if (_confirmText != null)
                _confirmText.text = $"P1 选择了 {_levelTitle}\n是否确认开始？";
        }
    }

    void OnConfirmStateChanged(int oldVal, int newVal)
    {
        if (IsServer)
        {
            if (newVal == 1)
                _confirmTimeoutRoutine = StartCoroutine(ConfirmTimeout());
            else if (_confirmTimeoutRoutine != null)
            {
                StopCoroutine(_confirmTimeoutRoutine);
                _confirmTimeoutRoutine = null;
            }
        }

        if (newVal == 2)
        {
            // Confirmed → load this NPC's level
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
            if (_dialoguePanel != null) _dialoguePanel.SetActive(false);

            if (IsServer)
            {
                // Show "loading" message to P1
                if (_dialoguePanel != null)
                {
                    _dialoguePanel.SetActive(true);
                    if (_dialogueText != null)
                        _dialogueText.text = "P2 已确认！正在加载关卡...";
                }

                GameManager.Instance.LoadLevel(_levelSceneName, _levelTitle);
            }
        }
        else if (newVal == 3)
        {
            // Rejected → close all panels
            if (_confirmPanel != null) _confirmPanel.SetActive(false);

            if (IsServer)
            {
                // P1: close all panels, return to NPC interaction
                if (_missionSelectPanel != null) _missionSelectPanel.SetActive(false);
                if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
                _inDialogue = false;
                _selectedMission.Value = -1;
                GameHUD.Instance?.ShowToast("P2 拒绝了任务，请重新与NPC交互选择");
            }
            else
            {
                // P2: confirm panel closed, wait for P1 to re-select
                GameHUD.Instance?.ShowToast("已拒绝任务，等待P1重新选择");
            }
        }
        else if (newVal == 0)
        {
            // Cancelled (timeout or disconnect)
            if (IsServer)
            {
                if (_confirmPanel != null) _confirmPanel.SetActive(false);
                ShowMissionSelect();
                GameHUD.Instance?.ShowToast("任务确认已取消");
            }
            else
            {
                if (_confirmPanel != null) _confirmPanel.SetActive(false);
                GameHUD.Instance?.ShowToast("任务确认已取消或超时");
            }
        }
    }

    const float CONFIRM_TIMEOUT = 30f;
    Coroutine _confirmTimeoutRoutine;

    IEnumerator ConfirmTimeout()
    {
        yield return new WaitForSeconds(CONFIRM_TIMEOUT);
        if (_confirmState.Value == 1)
            _confirmState.Value = 0;
    }

    // Called by P2 confirm button
    public void OnConfirmYes()
    {
        ConfirmMissionServerRpc(true);
    }

    public void OnConfirmNo()
    {
        ConfirmMissionServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    void ConfirmMissionServerRpc(bool confirmed)
    {
        _confirmState.Value = confirmed ? 2 : 3;
    }

    /// <summary>
    /// Called by GameManager when a client disconnects (server only):
    /// cancel a pending mission confirmation.
    /// </summary>
    public void OnPlayerDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        if (!NetworkObject.IsSpawned) return;
        if (_confirmState.Value == 1)
            _confirmState.Value = 0;
    }

    // Wire up button listeners
    public void WireButtons()
    {
        if (_nextButton != null)
            _nextButton.onClick.AddListener(() => { _dialogueIndex++; ShowDialogueLine(); });

        if (_mission1Button != null)
            _mission1Button.onClick.AddListener(() => OnMissionSelected(_missionId));

        if (_confirmYesButton != null)
            _confirmYesButton.onClick.AddListener(OnConfirmYes);

        if (_confirmNoButton != null)
            _confirmNoButton.onClick.AddListener(OnConfirmNo);
    }
}
