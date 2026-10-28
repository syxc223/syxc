using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// In-game chat system (League of Legends style public chat).
/// Press T to open chat input, Enter to send, Escape to cancel.
/// Messages are broadcast to all players via ChatNetwork and visible on screen.
/// Attach to the GameHUDCanvas (DontDestroyOnLoad).
/// </summary>
public class ChatSystem : MonoBehaviour
{
    public static ChatSystem Instance { get; private set; }

    /// <summary>True while the chat input field is focused — other scripts should suppress input.</summary>
    public static bool IsChatOpen { get; private set; }

    const int MaxMessages = 8;
    const int MaxMessageLength = 120;
    const float MessageVisibleDuration = 10f;
    const float FadeSpeed = 4f;

    TMP_FontAsset _font;
    GameObject _chatPanel;
    Transform _messageContainer;
    TMP_InputField _inputField;
    CanvasGroup _panelGroup;
    CanvasGroup _inputGroup;

    // Pinned history view — press Y to keep recent messages on screen
    bool _historyPinned;

    // Live preview of the message being typed (last row of the message list)
    GameObject _previewGO;
    TextMeshProUGUI _previewText;
    LayoutElement _previewLayout;

    struct ChatMsg
    {
        public string Text;
        public Color Color;
    }

    readonly List<ChatMsg> _messages = new();
    float _lastActivityTime = -999f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _font = TMP_Settings.defaultFontAsset;
        BuildUI();
    }

    void Update()
    {
        HandleInput();
        UpdateFade();
    }

    #region Input

    void HandleInput()
    {
        if (IsChatOpen)
        {
            // Escape cancels chat (onEndEdit handles Enter)
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseChat();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.T))
                OpenChat();

            // Y toggles the pinned history view of recent messages
            if (Input.GetKeyDown(KeyCode.Y))
                ToggleHistoryPin();
        }
    }

    /// <summary>
    /// Press Y: keep the recent-chat panel on screen for review; press again to unpine.
    /// </summary>
    void ToggleHistoryPin()
    {
        _historyPinned = !_historyPinned;
        _lastActivityTime = Time.time;
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    void OpenChat()
    {
        if (GameHUD.IsPaused) return; // don't open over the pause menu

        IsChatOpen = true;

        // The panel's CanvasGroup must be interactable, otherwise the TMP_InputField
        // (a child Selectable) can never gain focus and keyboard input is ignored.
        if (_panelGroup != null)
        {
            _panelGroup.interactable = true;
            _panelGroup.blocksRaycasts = true;
        }

        _inputField.text = "";
        _inputField.gameObject.SetActive(true);
        _inputField.ActivateInputField();
        UpdatePreview("");
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
        if (_inputGroup != null) _inputGroup.blocksRaycasts = true;
        _lastActivityTime = Time.time;
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    void CloseChat()
    {
        if (!IsChatOpen) return;
        IsChatOpen = false;
        _inputField.DeactivateInputField();
        _inputField.text = "";
        _inputField.gameObject.SetActive(false);
        UpdatePreview("");
        if (_inputGroup != null) _inputGroup.blocksRaycasts = false;
        if (_panelGroup != null)
        {
            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;
        }
        _lastActivityTime = Time.time;
    }

    void OnInputEndEdit(string text)
    {
        if (!IsChatOpen) return;

        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (enterPressed)
            TrySend();
        else
            CloseChat();
    }

    void TrySend()
    {
        string msg = _inputField.text.Trim();
        if (!string.IsNullOrEmpty(msg))
        {
            if (msg.Length > MaxMessageLength)
                msg = msg.Substring(0, MaxMessageLength);

            string sender = ApiClient.IsLoggedIn
                ? ApiClient.Username
                : $"P{(NetworkManager.Singleton?.LocalClientId ?? 0) + 1}";

            if (ChatNetwork.Instance != null)
            {
                ChatNetwork.Instance.SendChatServerRpc(sender, msg);
            }
            else
            {
                // Not in a gameplay scene (no ChatNetwork) — the partner can't receive
                GameHUD.Instance?.ShowToast("当前不在游戏房间，消息未发送");
            }
        }
        CloseChat();
    }

    #endregion

    #region Message Display

    public void OnMessageReceived(ulong clientId, string sender, string message)
    {
        AddMessage($"[P{clientId + 1}] {sender}: {message}", ColorForClient(clientId));
    }

    /// <summary>Color by player: P1 gaslight amber, P2 muted gaslight blue.</summary>
    static Color ColorForClient(ulong clientId)
    {
        return clientId == 0
            ? new Color(0.95f, 0.78f, 0.45f)
            : new Color(0.66f, 0.80f, 0.88f);
    }

    /// <summary>Live preview of the message being typed, shown as the last row of the list.</summary>
    void UpdatePreview(string text)
    {
        if (_previewGO == null || _previewText == null) return;

        string trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed) || !IsChatOpen)
        {
            _previewGO.SetActive(false);
            if (_previewLayout != null) _previewLayout.preferredHeight = 0;
            return;
        }

        if (trimmed.Length > MaxMessageLength)
            trimmed = trimmed.Substring(0, MaxMessageLength);

        ulong clientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        string sender = ApiClient.IsLoggedIn ? ApiClient.Username : $"P{clientId + 1}";

        _previewText.text = $"[P{clientId + 1}] {sender}: {trimmed}";
        var color = ColorForClient(clientId);
        color.a = 0.65f;
        _previewText.color = color;

        _previewGO.SetActive(true);
        _previewGO.transform.SetAsLastSibling();
        if (_previewLayout != null)
            _previewLayout.preferredHeight = _previewText.GetPreferredValues(_previewText.text, 380f, 0f).y + 4f;
    }

    /// <summary>
    /// Local system message (welcome hints, notices) — shown in yellow, not networked.
    /// </summary>
    public void ShowSystemMessage(string message)
    {
        AddMessage($"[系统] {message}", new Color(1f, 0.85f, 0.4f));
    }

    void AddMessage(string text, Color color)
    {
        _messages.Add(new ChatMsg { Text = text, Color = color });
        while (_messages.Count > MaxMessages)
            _messages.RemoveAt(0);

        _lastActivityTime = Time.time;
        RebuildMessages();
    }

    void RebuildMessages()
    {
        for (int i = _messageContainer.childCount - 1; i >= 0; i--)
        {
            var child = _messageContainer.GetChild(i);
            if (child.name == "Preview") continue; // preview row survives rebuilds
            Destroy(child.gameObject);
        }

        foreach (var msg in _messages)
        {
            var go = new GameObject("Msg", typeof(RectTransform));
            go.transform.SetParent(_messageContainer, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = msg.Text;
            tmp.color = msg.Color;
            tmp.fontSize = 14;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            var le = go.AddComponent<LayoutElement>();
            // Dynamic row height so wrapped messages don't overlap the next row
            le.preferredHeight = tmp.GetPreferredValues(msg.Text, 380f, 0f).y + 4f;
        }

        // Keep the preview as the last row
        if (_previewGO != null && _previewGO.activeSelf)
            _previewGO.transform.SetAsLastSibling();
    }

    void UpdateFade()
    {
        if (IsChatOpen)
        {
            if (_panelGroup != null)
                _panelGroup.alpha = Mathf.MoveTowards(_panelGroup.alpha, 1f, FadeSpeed * Time.deltaTime);
            return;
        }

        bool visible = _historyPinned || Time.time - _lastActivityTime < MessageVisibleDuration;
        float targetPanel = visible ? (_historyPinned ? 0.85f : 0.75f) : 0f;

        if (_panelGroup != null)
            _panelGroup.alpha = Mathf.MoveTowards(_panelGroup.alpha, targetPanel, FadeSpeed * Time.deltaTime);
        if (_inputGroup != null)
            _inputGroup.alpha = Mathf.MoveTowards(_inputGroup.alpha, 0f, FadeSpeed * Time.deltaTime);
    }

    #endregion

    #region UI Construction

    void BuildUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        // Root chat panel
        _chatPanel = new GameObject("ChatPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        _chatPanel.transform.SetParent(parent, false);
        _panelGroup = _chatPanel.GetComponent<CanvasGroup>();
        _panelGroup.alpha = 0f;
        _panelGroup.blocksRaycasts = false;
        _panelGroup.interactable = false;

        var panelRT = _chatPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(0, 0);
        panelRT.pivot = new Vector2(0, 0);
        panelRT.anchoredPosition = new Vector2(10, 10);
        panelRT.sizeDelta = new Vector2(420, 240);

        var panelImg = _chatPanel.GetComponent<Image>();
        panelImg.color = new Color(0.09f, 0.07f, 0.05f, 0.72f); // walnut, detective theme
        panelImg.raycastTarget = false;

        // Messages container
        var msgGO = new GameObject("Messages", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        msgGO.transform.SetParent(_chatPanel.transform, false);
        _messageContainer = msgGO.transform;

        var msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0, 1);
        msgRT.anchorMax = new Vector2(1, 1);
        msgRT.pivot = new Vector2(0.5f, 1);
        msgRT.anchoredPosition = new Vector2(0, -4);
        msgRT.sizeDelta = new Vector2(-8, -38);

        var vlg = msgGO.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2;
        vlg.padding = new RectOffset(6, 6, 4, 4);

        var csf = msgGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Preview row (lives inside the message container; RebuildMessages keeps it)
        _previewGO = new GameObject("Preview", typeof(RectTransform));
        _previewGO.transform.SetParent(_messageContainer, false);
        _previewText = _previewGO.AddComponent<TextMeshProUGUI>();
        if (_font != null) _previewText.font = _font;
        _previewText.fontSize = 14;
        _previewText.fontStyle = FontStyles.Italic;
        _previewText.raycastTarget = false;
        _previewText.enableWordWrapping = true;
        _previewText.overflowMode = TextOverflowModes.Overflow;
        _previewLayout = _previewGO.AddComponent<LayoutElement>();
        _previewLayout.preferredHeight = 0;
        _previewGO.SetActive(false);

        // Input area (bottom of panel)
        var inputGO = new GameObject("ChatInput", typeof(RectTransform), typeof(CanvasGroup));
        inputGO.transform.SetParent(_chatPanel.transform, false);
        _inputGroup = inputGO.GetComponent<CanvasGroup>();
        _inputGroup.alpha = 0f;
        _inputGroup.blocksRaycasts = false;

        var inputRT = inputGO.GetComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0, 0);
        inputRT.anchorMax = new Vector2(1, 0);
        inputRT.pivot = new Vector2(0.5f, 0);
        inputRT.anchoredPosition = new Vector2(0, 4);
        inputRT.sizeDelta = new Vector2(-8, 28);

        BuildInputField(inputGO.transform);

        _inputField.gameObject.SetActive(false);
    }

    void BuildInputField(Transform parent)
    {
        // Background
        var bgGO = new GameObject("InputBG", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(parent, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(0.13f, 0.10f, 0.07f, 0.85f); // walnut inset
        bgGO.GetComponent<Image>().raycastTarget = false; // never block clicks behind the input bar

        // TMP_InputField
        var inputGO = new GameObject("TMP_InputField", typeof(RectTransform), typeof(TMP_InputField));
        inputGO.transform.SetParent(parent, false);
        _inputField = inputGO.GetComponent<TMP_InputField>();
        _inputField.lineType = TMP_InputField.LineType.SingleLine;

        var inputRT = inputGO.GetComponent<RectTransform>();
        inputRT.anchorMin = Vector2.zero;
        inputRT.anchorMax = Vector2.one;
        inputRT.offsetMin = new Vector2(6, 0);
        inputRT.offsetMax = new Vector2(-6, 0);

        // Text Area (required by TMP_InputField)
        var textAreaGO = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textAreaGO.transform.SetParent(inputGO.transform, false);
        var textAreaRT = textAreaGO.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = Vector2.zero;
        textAreaRT.offsetMax = Vector2.zero;

        // Placeholder
        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(textAreaGO.transform, false);
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;
        var phTmp = phGO.GetComponent<TextMeshProUGUI>();
        if (_font != null) phTmp.font = _font;
        phTmp.text = "按 Enter 发送, Esc 取消...";
        phTmp.fontSize = 14;
        phTmp.color = new Color(0.62f, 0.53f, 0.40f, 0.5f); // sepia
        phTmp.raycastTarget = false;
        phTmp.fontStyle = FontStyles.Italic;

        // Text
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(textAreaGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var textTmp = textGO.GetComponent<TextMeshProUGUI>();
        if (_font != null) textTmp.font = _font;
        textTmp.text = "";
        textTmp.fontSize = 14;
        textTmp.color = new Color(0.93f, 0.86f, 0.72f); // parchment
        textTmp.raycastTarget = false;

        // Wire up TMP_InputField
        _inputField.textViewport = textAreaRT;
        _inputField.textComponent = textTmp;
        _inputField.placeholder = phTmp;
        _inputField.characterLimit = MaxMessageLength;
        _inputField.onEndEdit.AddListener(OnInputEndEdit);
        _inputField.onValueChanged.AddListener(UpdatePreview);
    }

    #endregion
}
