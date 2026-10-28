using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 运行时创建调查 UI（调查面板、证物出示、证物手册）。
/// </summary>
public class InvestigationUISetup : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectOfType<ExaminePanelController>() != null
            && FindObjectOfType<EvidencePresentPanelController>() != null)
            return;

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("InvestigationCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        CreateExaminePanel(canvas.transform);
        CreateEvidencePresentPanel(canvas.transform);
        CreateEvidenceJournal(canvas.transform);
    }

    private static void CreateExaminePanel(Transform parent)
    {
        if (FindObjectOfType<ExaminePanelController>() != null)
            return;

        var panel = CreatePanelRoot(parent, "ExaminePanel", new Vector2(520, 320));
        var title = CreateText(panel.transform, "Title", new Vector2(0, 100), 24);
        var body = CreateText(panel.transform, "Body", new Vector2(0, 20), 18);
        body.rectTransform.sizeDelta = new Vector2(460, 160);
        var close = CreateButton(panel.transform, "Close", new Vector2(0, -120), "关闭");

        var controller = panel.AddComponent<ExaminePanelController>();
        SetField(controller, "titleText", title);
        SetField(controller, "contentText", body);
        SetField(controller, "closeButton", close);
    }

    private static void CreateEvidencePresentPanel(Transform parent)
    {
        if (FindObjectOfType<EvidencePresentPanelController>() != null)
            return;

        var panel = CreatePanelRoot(parent, "EvidencePresentPanel", new Vector2(560, 420));
        var prompt = CreateText(panel.transform, "Prompt", new Vector2(0, 160), 20);
        var feedback = CreateText(panel.transform, "Feedback", new Vector2(0, 120), 16);

        var listGo = new GameObject("EvidenceList");
        listGo.transform.SetParent(panel.transform, false);
        var listRect = listGo.AddComponent<RectTransform>();
        listRect.sizeDelta = new Vector2(480, 180);
        listRect.anchoredPosition = new Vector2(0, 10);
        var layout = listGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;

        var buttonPrefab = CreateButton(listGo.transform, "EvidenceButtonPrefab", Vector2.zero, "证物");
        buttonPrefab.gameObject.SetActive(false);

        var cancel = CreateButton(panel.transform, "Cancel", new Vector2(0, -170), "取消");

        var controller = panel.AddComponent<EvidencePresentPanelController>();
        SetField(controller, "promptText", prompt);
        SetField(controller, "feedbackText", feedback);
        SetField(controller, "evidenceListContainer", listGo.transform);
        SetField(controller, "evidenceButtonPrefab", buttonPrefab);
        SetField(controller, "cancelButton", cancel);
    }

    private static void CreateEvidenceJournal(Transform parent)
    {
        if (FindObjectOfType<EvidenceJournalController>() != null)
            return;

        var panel = CreatePanelRoot(parent, "EvidenceJournal", new Vector2(480, 360));
        var body = CreateText(panel.transform, "JournalBody", Vector2.zero, 16);
        body.rectTransform.sizeDelta = new Vector2(420, 280);
        body.alignment = TextAlignmentOptions.TopLeft;

        var controller = panel.AddComponent<EvidenceJournalController>();
        SetField(controller, "journalRoot", panel);
        SetField(controller, "journalContentText", body);

        panel.SetActive(false);

        var input = panel.AddComponent<EvidenceJournalInput>();
        SetField(input, "journal", controller);
    }

    private static GameObject CreatePanelRoot(Transform parent, string name, Vector2 size)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        panel.SetActive(false);
        return panel;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchoredPos, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(440, 40);
        rect.anchoredPosition = anchoredPos;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = string.Empty;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchoredPos, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420, 36);
        rect.anchoredPosition = anchoredPos;
        var image = go.AddComponent<Image>();
        image.color = new Color(0.25f, 0.35f, 0.55f, 1f);
        var button = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return button;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}

public class EvidenceJournalInput : MonoBehaviour
{
    [SerializeField] private EvidenceJournalController journal;

    private void Update()
    {
        if (GameModeManager.Instance == null)
            return;

        var mode = GameModeManager.Instance.CurrentGameMode;
        if (mode != GameMode.Explore && mode != GameMode.Pause)
            return;

        if (Input.GetKeyDown(KeyCode.J))
            journal?.ToggleJournal();
    }
}
