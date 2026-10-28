using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class SetupMenuUI
{
    static TMP_FontAsset _font;

    public static void Run()
    {
        _font = TMP_Settings.defaultFontAsset;
        if (_font == null)
        {
            Debug.LogError("TMP default font not found! Ensure TMP Essential Resources are imported.");
            return;
        }

        // Search including inactive objects
        GameObject canvas = null;
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            if (root.name == "MainMenuCanvas") { canvas = root; break; }
        }
        if (canvas == null)
        {
            Debug.LogError("MainMenuCanvas not found!");
            return;
        }

        canvas.SetActive(true);

        var cg = canvas.GetComponent<CanvasGroup>();
        if (cg == null) cg = canvas.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // Remove old UI elements
        string[] toRemove = { "MenuPanel", "HostButton", "IPInput", "JoinButton", "QuitButton", "IPDisplay" };
        foreach (var name in toRemove)
        {
            var child = canvas.transform.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        var loginPanel = CreateLoginPanel(canvas.transform);
        var roomPanel = CreateRoomPanel(canvas.transform);
        var prefab = CreateRoomItemPrefab();

        WireLoginPanel(loginPanel);
        WireRoomPanel(roomPanel, prefab);
        WireUIManager(canvas, loginPanel, roomPanel);

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("Menu UI setup complete!");
    }

    static TMP_Text CreateText(Transform parent, string name, string content, int fontSize, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = content;
        t.font = _font;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        return t;
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static Button CreateButton(Transform parent, string name, string label, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.6f, 1f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        colors.pressedColor = new Color(0.15f, 0.25f, 0.45f, 1f);
        btn.colors = colors;

        var text = CreateText(go.transform, "Label", label, fontSize);
        var rt = text.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return btn;
    }

    static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, int fontSize, bool password = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        var taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 6);
        taRT.offsetMax = new Vector2(-10, -6);

        var ph = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        ph.transform.SetParent(textArea.transform, false);
        var phT = ph.GetComponent<TextMeshProUGUI>();
        phT.text = placeholder;
        phT.font = _font;
        phT.fontSize = fontSize;
        phT.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        phT.alignment = TextAlignmentOptions.Left;
        var phRT = ph.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;

        var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txt.transform.SetParent(textArea.transform, false);
        var txtT = txt.GetComponent<TextMeshProUGUI>();
        txtT.text = "";
        txtT.font = _font;
        txtT.fontSize = fontSize;
        txtT.color = Color.white;
        txtT.alignment = TextAlignmentOptions.Left;
        var txtRT = txt.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = textArea.GetComponent<RectTransform>();
        input.textComponent = txtT;
        input.placeholder = phT;
        input.text = "";
        input.characterLimit = 0;
        input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;

        return input;
    }

    static void SetRT(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static void SetRTCentered(RectTransform rt, float width, float height, float yPos)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0, yPos);
    }

    static GameObject CreateLoginPanel(Transform parent)
    {
        var panel = new GameObject("LoginPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        var panelRT = panel.GetComponent<RectTransform>();
        SetRT(panelRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var title = CreateText(panel.transform, "LoginTitle", "登录", 36);
        SetRTCentered(title.GetComponent<RectTransform>(), 200, 50, 180);

        var userIn = CreateInputField(panel.transform, "UsernameInput", "用户名 (3-20字符)", 20);
        SetRTCentered(userIn.GetComponent<RectTransform>(), 300, 40, 100);

        var passIn = CreateInputField(panel.transform, "PasswordInput", "密码 (6-32字符)", 20, true);
        SetRTCentered(passIn.GetComponent<RectTransform>(), 300, 40, 40);

        var msg = CreateText(panel.transform, "MessageText", "", 16);
        msg.color = new Color(1f, 0.4f, 0.4f, 1f);
        SetRTCentered(msg.GetComponent<RectTransform>(), 350, 25, 0);

        var btnRow = new GameObject("ButtonRow", typeof(RectTransform));
        btnRow.transform.SetParent(panel.transform, false);
        SetRTCentered(btnRow.GetComponent<RectTransform>(), 320, 45, -60);

        var loginBtn = CreateButton(btnRow.transform, "LoginButton", "登录", 20);
        SetRTCentered(loginBtn.GetComponent<RectTransform>(), 140, 40, -30);

        var regBtn = CreateButton(btnRow.transform, "RegisterButton", "注册", 20);
        SetRTCentered(regBtn.GetComponent<RectTransform>(), 140, 40, 30);
        regBtn.GetComponent<Image>().color = new Color(0.3f, 0.45f, 0.3f, 1f);

        var quitBtn = CreateButton(panel.transform, "QuitButton", "退出游戏", 18);
        SetRTCentered(quitBtn.GetComponent<RectTransform>(), 140, 35, -120);
        quitBtn.GetComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);

        var ip = CreateText(panel.transform, "IPDisplay", "", 14, TextAlignmentOptions.Center);
        ip.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        var ipRT = ip.GetComponent<RectTransform>();
        SetRT(ipRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 5), new Vector2(0, 25));

        CreateRegisterPanel(panel.transform);
        panel.AddComponent<LoginPanel>();
        return panel;
    }

    static GameObject CreateRegisterPanel(Transform parent)
    {
        var panel = new GameObject("RegisterPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        var panelRT = panel.GetComponent<RectTransform>();
        SetRT(panelRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel.SetActive(false);

        var title = CreateText(panel.transform, "RegTitle", "注册新账号", 32);
        SetRTCentered(title.GetComponent<RectTransform>(), 250, 45, 200);

        var userIn = CreateInputField(panel.transform, "RegUsernameInput", "用户名 (3-20字符)", 20);
        SetRTCentered(userIn.GetComponent<RectTransform>(), 300, 40, 130);

        var passIn = CreateInputField(panel.transform, "RegPasswordInput", "密码 (6-32字符)", 20, true);
        SetRTCentered(passIn.GetComponent<RectTransform>(), 300, 40, 70);

        var confIn = CreateInputField(panel.transform, "RegConfirmInput", "确认密码", 20, true);
        SetRTCentered(confIn.GetComponent<RectTransform>(), 300, 40, 10);

        var msg = CreateText(panel.transform, "RegMessageText", "", 16);
        msg.color = new Color(1f, 0.4f, 0.4f, 1f);
        SetRTCentered(msg.GetComponent<RectTransform>(), 350, 25, -30);

        var confirmBtn = CreateButton(panel.transform, "ConfirmRegisterButton", "确认注册", 20);
        SetRTCentered(confirmBtn.GetComponent<RectTransform>(), 140, 40, -80);

        var backBtn = CreateButton(panel.transform, "BackButton", "返回", 20);
        SetRTCentered(backBtn.GetComponent<RectTransform>(), 140, 40, -130);
        backBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);

        return panel;
    }

    static GameObject CreateRoomPanel(Transform parent)
    {
        var panel = new GameObject("RoomPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        var panelRT = panel.GetComponent<RectTransform>();
        SetRT(panelRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel.SetActive(false);

        var welcome = CreateText(panel.transform, "WelcomeText", "欢迎", 24, TextAlignmentOptions.Left);
        var wRT = welcome.GetComponent<RectTransform>();
        SetRT(wRT, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(20, -50), new Vector2(0, -10));

        var logoutBtn = CreateButton(panel.transform, "LogoutButton", "注销", 18);
        SetRT(logoutBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -50), new Vector2(-20, -15));
        logoutBtn.GetComponent<Image>().color = new Color(0.4f, 0.3f, 0.2f, 1f);

        var quitBtn = CreateButton(panel.transform, "QuitButton", "退出", 18);
        SetRT(quitBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -90), new Vector2(-20, -55));
        quitBtn.GetComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);

        // Room list scroll view
        var scrollGO = new GameObject("RoomScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(panel.transform, false);
        scrollGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        SetRT(scrollRT, new Vector2(0, 0.15f), new Vector2(1, 0.85f), new Vector2(20, 20), new Vector2(-20, -20));

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        SetRT(contentRT, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;

        var btnRow = new GameObject("BottomButtons", typeof(RectTransform));
        btnRow.transform.SetParent(panel.transform, false);
        SetRT(btnRow.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 15), new Vector2(-20, 55));

        var createBtn = CreateButton(btnRow.transform, "CreateRoomButton", "创建房间", 20);
        SetRTCentered(createBtn.GetComponent<RectTransform>(), 160, 40, -40);
        createBtn.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f, 1f);

        var refreshBtn = CreateButton(btnRow.transform, "RefreshButton", "刷新列表", 20);
        SetRTCentered(refreshBtn.GetComponent<RectTransform>(), 160, 40, 40);
        refreshBtn.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.5f, 1f);

        CreateCreateRoomDialog(panel.transform);
        panel.AddComponent<RoomPanel>();
        return panel;
    }

    static GameObject CreateCreateRoomDialog(Transform parent)
    {
        var dialog = new GameObject("CreateRoomDialog", typeof(RectTransform), typeof(Image));
        dialog.transform.SetParent(parent, false);
        dialog.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        SetRT(dialog.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dialog.SetActive(false);

        var inner = CreateImage(dialog.transform, "DialogPanel", new Color(0.15f, 0.15f, 0.2f, 1f));
        SetRTCentered(inner.GetComponent<RectTransform>(), 360, 220, 0);

        var title = CreateText(inner.transform, "DialogTitle", "创建房间", 26);
        SetRTCentered(title.GetComponent<RectTransform>(), 200, 40, 70);

        var nameIn = CreateInputField(inner.transform, "RoomNameInput", "输入房间名称", 20);
        SetRTCentered(nameIn.GetComponent<RectTransform>(), 300, 40, 10);

        var confirmBtn = CreateButton(inner.transform, "ConfirmCreateButton", "创建", 20);
        SetRTCentered(confirmBtn.GetComponent<RectTransform>(), 130, 40, -50);
        confirmBtn.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f, 1f);

        var cancelBtn = CreateButton(inner.transform, "CancelCreateButton", "取消", 20);
        var cancelRT = cancelBtn.GetComponent<RectTransform>();
        SetRTCentered(cancelRT, 130, 40, -50);
        cancelRT.anchoredPosition = new Vector2(75, -50);
        cancelBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);

        return dialog;
    }

    static GameObject CreateRoomItemPrefab()
    {
        var go = new GameObject("RoomItemPrefab", typeof(RectTransform), typeof(Image));
        go.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 0.9f);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        var nameT = CreateText(go.transform, "RoomNameText", "Room Name", 18, TextAlignmentOptions.Left);
        nameT.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 0);

        var hostT = CreateText(go.transform, "HostNameText", "Host: ---", 16, TextAlignmentOptions.Left);
        hostT.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        hostT.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 0);

        var countT = CreateText(go.transform, "PlayerCountText", "1/2", 18, TextAlignmentOptions.Center);
        countT.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 0);

        var joinBtn = CreateButton(go.transform, "JoinButton", "加入", 18);
        joinBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 35);
        joinBtn.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f, 1f);

        string prefabPath = "Assets/Prefabs/RoomItemPrefab.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    static void WireLoginPanel(GameObject loginPanel)
    {
        var lp = loginPanel.GetComponent<LoginPanel>();
        var so = new SerializedObject(lp);

        so.FindProperty("_usernameInput").objectReferenceValue = loginPanel.transform.Find("UsernameInput")?.GetComponent<TMP_InputField>();
        so.FindProperty("_passwordInput").objectReferenceValue = loginPanel.transform.Find("PasswordInput")?.GetComponent<TMP_InputField>();
        so.FindProperty("_loginButton").objectReferenceValue = loginPanel.transform.Find("ButtonRow/LoginButton")?.GetComponent<Button>();
        so.FindProperty("_registerButton").objectReferenceValue = loginPanel.transform.Find("ButtonRow/RegisterButton")?.GetComponent<Button>();
        so.FindProperty("_quitButton").objectReferenceValue = loginPanel.transform.Find("QuitButton")?.GetComponent<Button>();
        so.FindProperty("_messageText").objectReferenceValue = loginPanel.transform.Find("MessageText")?.GetComponent<TMP_Text>();
        so.FindProperty("_ipText").objectReferenceValue = loginPanel.transform.Find("IPDisplay")?.GetComponent<TMP_Text>();
        so.FindProperty("_registerPanel").objectReferenceValue = loginPanel.transform.Find("RegisterPanel")?.gameObject;

        var regPanel = loginPanel.transform.Find("RegisterPanel");
        if (regPanel != null)
        {
            so.FindProperty("_regUsernameInput").objectReferenceValue = regPanel.Find("RegUsernameInput")?.GetComponent<TMP_InputField>();
            so.FindProperty("_regPasswordInput").objectReferenceValue = regPanel.Find("RegPasswordInput")?.GetComponent<TMP_InputField>();
            so.FindProperty("_regConfirmInput").objectReferenceValue = regPanel.Find("RegConfirmInput")?.GetComponent<TMP_InputField>();
            so.FindProperty("_confirmRegisterButton").objectReferenceValue = regPanel.Find("ConfirmRegisterButton")?.GetComponent<Button>();
            so.FindProperty("_backButton").objectReferenceValue = regPanel.Find("BackButton")?.GetComponent<Button>();
            so.FindProperty("_regMessageText").objectReferenceValue = regPanel.Find("RegMessageText")?.GetComponent<TMP_Text>();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("LoginPanel references wired.");
    }

    static void WireRoomPanel(GameObject roomPanel, GameObject roomItemPrefab)
    {
        var rp = roomPanel.GetComponent<RoomPanel>();
        var so = new SerializedObject(rp);

        so.FindProperty("_welcomeText").objectReferenceValue = roomPanel.transform.Find("WelcomeText")?.GetComponent<TMP_Text>();
        so.FindProperty("_logoutButton").objectReferenceValue = roomPanel.transform.Find("LogoutButton")?.GetComponent<Button>();
        so.FindProperty("_quitButton").objectReferenceValue = roomPanel.transform.Find("QuitButton")?.GetComponent<Button>();
        so.FindProperty("_roomListContent").objectReferenceValue = roomPanel.transform.Find("RoomScrollView/Content")?.GetComponent<RectTransform>();
        so.FindProperty("_roomItemPrefab").objectReferenceValue = roomItemPrefab;
        so.FindProperty("_refreshButton").objectReferenceValue = roomPanel.transform.Find("BottomButtons/RefreshButton")?.GetComponent<Button>();
        so.FindProperty("_createRoomButton").objectReferenceValue = roomPanel.transform.Find("BottomButtons/CreateRoomButton")?.GetComponent<Button>();
        so.FindProperty("_createRoomDialog").objectReferenceValue = roomPanel.transform.Find("CreateRoomDialog")?.gameObject;

        var dialog = roomPanel.transform.Find("CreateRoomDialog");
        if (dialog != null)
        {
            so.FindProperty("_roomNameInput").objectReferenceValue = dialog.Find("DialogPanel/RoomNameInput")?.GetComponent<TMP_InputField>();
            so.FindProperty("_confirmCreateButton").objectReferenceValue = dialog.Find("DialogPanel/ConfirmCreateButton")?.GetComponent<Button>();
            so.FindProperty("_cancelCreateButton").objectReferenceValue = dialog.Find("DialogPanel/CancelCreateButton")?.GetComponent<Button>();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("RoomPanel references wired.");
    }

    static void WireUIManager(GameObject canvas, GameObject loginPanel, GameObject roomPanel)
    {
        var ui = canvas.GetComponent<UIManager>();
        if (ui == null) ui = canvas.AddComponent<UIManager>();

        var so = new SerializedObject(ui);
        so.FindProperty("_loginPanel").objectReferenceValue = loginPanel;
        so.FindProperty("_roomPanel").objectReferenceValue = roomPanel;
        so.FindProperty("_menuGroup").objectReferenceValue = canvas.GetComponent<CanvasGroup>();
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("UIManager references wired.");
    }

    [MenuItem("Tools/Setup Menu UI")]
    public static void MenuRun()
    {
        Run();
    }
}
