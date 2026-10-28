using System;
using System.Collections.Generic;
using System.Text;
using Game;
using MFramework.Event;
using UnityEngine;
using UnityEngine.UI;


//后端框架：Node.js + Express
public class BattleUIController : MonoBehaviour,
    IEventReceiver<BattleStartedEvent>,
    IEventReceiver<EntityTurnStartedEvent>,
    IEventReceiver<AwaitPlayerCommandEvent>,
    IEventReceiver<CommandTargetRequestEvent>,
    IEventReceiver<EntityDamagedEvent>,
    IEventReceiver<EntityHealedEvent>,
    IEventReceiver<ShieldBrokenEvent>,
    IEventReceiver<TurnOrderBuiltEvent>,
    IEventReceiver<BattleLogEvent>,
    IEventReceiver<BattleResultReadyEvent>,
    IEventReceiver<BattleEndedEvent>
{
    private class EntityRow
    {
        public BattleEntity Entity;
        public RectTransform Root;
        public Text NameText;
        public RectTransform HpFill;
        public Text HpText;
        public Text ShieldText;
    }

    private BattleController _controller;
    private Font _font;
    private readonly Dictionary<int, EntityRow> _rows = new();

    private RectTransform _enemyPanel;
    private RectTransform _allyPanel;
    private RectTransform _menuPanel;
    private RectTransform _resultPanel;
    private Text _logText;
    private Text _orderText;
    private RectTransform _damageLayer;
    private DamageNumberPool _damagePool;

    private readonly List<string> _logs = new();
    private BattleEntity _currentPlayer;

    public void Bind(BattleController controller)
    {
        _controller = controller;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCanvas();
        SubscribeAll();
    }

    private void OnDestroy() => UnsubscribeAll();

    // ---------- 界面搭建 ----------

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _enemyPanel = MakeVerticalPanel(canvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -20), new Vector2(700, 240));
        _allyPanel = MakeVerticalPanel(canvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -300), new Vector2(700, 220));

        _orderText = MakeText(canvasGo.transform, "", 20, new Color(0.9f, 0.9f, 0.9f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220, 0), new Vector2(360, 500));

        _logText = MakeText(canvasGo.transform, "", 20, new Color(0.85f, 0.85f, 0.85f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30, 220), new Vector2(900, 160));

        _menuPanel = MakeVerticalPanel(canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 30), new Vector2(900, 260));

        // 飘字层
        var dmgGo = new GameObject("DamageLayer", typeof(RectTransform));
        _damageLayer = dmgGo.GetComponent<RectTransform>();
        _damageLayer.SetParent(canvasGo.transform, false);
        Stretch(_damageLayer);
        _damagePool = dmgGo.AddComponent<DamageNumberPool>();
        _damagePool.Init(_damageLayer, _font);

        // 结果面板（默认隐藏）
        BuildResultPanel(canvasGo.transform);
    }

    private void BuildResultPanel(Transform parent)
    {
        var go = new GameObject("ResultPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        _resultPanel = go.GetComponent<RectTransform>();
        _resultPanel.SetParent(parent, false);
        Stretch(_resultPanel);
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        var box = MakeVerticalPanel(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(560, 420));
        box.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.96f);
        MakeText(box, "resultTitle", 30, Color.white, TextAnchor.MiddleCenter, new Vector2(520, 60));
        MakeText(box, "resultBody", 22, new Color(0.9f, 0.9f, 0.9f), TextAnchor.UpperLeft, new Vector2(520, 240)).name = "Body";
        MakeButton(box, "确认", () => _controller.ConfirmResult(), new Vector2(220, 60));
        go.SetActive(false);
    }

    private RectTransform MakeVerticalPanel(Transform parent, Vector2 anchor, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(CanvasGroup), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var vlg = go.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        return rt;
    }

    private Text MakeText(Transform parent, string content, int size, Color color,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var text = go.GetComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.color = color;
        text.text = content;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Text MakeText(Transform parent, string content, int size, Color color, TextAnchor align, Vector2 sizeDelta)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = sizeDelta;
        go.GetComponent<LayoutElement>().preferredHeight = sizeDelta.y;
        var text = go.GetComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.color = color;
        text.text = content;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    private Button MakeButton(Transform parent, string label, Action onClick, Vector2 size)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = size.y; le.minWidth = size.x;
        go.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.32f, 1f);
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.34f, 0.38f, 0.55f, 1f);
        colors.pressedColor = new Color(0.18f, 0.2f, 0.28f, 1f);
        btn.colors = colors;
        var labelText = MakeText(go.transform, label, 22, Color.white, TextAnchor.MiddleCenter, size);
        labelText.raycastTarget = false;
        btn.onClick.AddListener(() => onClick?.Invoke());
        return btn;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // ---------- 实体行 ----------

    private void BuildEntityRows(IReadOnlyList<BattleEntity> entities)
    {
        ClearChildren(_enemyPanel);
        ClearChildren(_allyPanel);
        _rows.Clear();

        foreach (var entity in entities)
        {
            var parent = entity.IsAlly ? _allyPanel : _enemyPanel;
            var row = CreateRow(parent, entity);
            _rows[entity.Id] = row;
        }
        RefreshAllRows();
    }

    private EntityRow CreateRow(Transform parent, BattleEntity entity)
    {
        var rootGo = new GameObject("Row_" + entity.DisplayName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var root = rootGo.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        rootGo.GetComponent<LayoutElement>().preferredHeight = 56;
        var hlg = rootGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childForceExpandWidth = false;

        var name = MakeText(root, entity.DisplayName, 20,
            entity.IsAlly ? new Color(0.7f, 0.85f, 1f) : new Color(1f, 0.7f, 0.7f),
            TextAnchor.MiddleLeft, new Vector2(150, 50));
        name.GetComponent<LayoutElement>().preferredWidth = 150;

        // 血条：底 + 前景（按比例改 anchorMax）
        var barGo = new GameObject("Bar", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        barGo.GetComponent<LayoutElement>().preferredWidth = 260;
        var barRt = barGo.GetComponent<RectTransform>();
        barRt.SetParent(root, false);
        barRt.sizeDelta = new Vector2(260, 24);
        barGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fill = fillGo.GetComponent<RectTransform>();
        fill.SetParent(barRt, false);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fillGo.GetComponent<Image>().color = entity.IsAlly ? new Color(0.3f, 0.75f, 0.35f) : new Color(0.85f, 0.3f, 0.3f);

        var hp = MakeText(root, "", 18, Color.white, TextAnchor.MiddleLeft, new Vector2(120, 40));
        hp.GetComponent<LayoutElement>().preferredWidth = 120;

        var shield = MakeText(root, "", 18, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleLeft, new Vector2(120, 40));
        shield.GetComponent<LayoutElement>().preferredWidth = 120;

        return new EntityRow
        {
            Entity = entity,
            Root = root,
            NameText = name,
            HpFill = fill,
            HpText = hp,
            ShieldText = shield
        };
    }

    private void RefreshAllRows()
    {
        foreach (var row in _rows.Values)
            RefreshRow(row);
    }

    private void RefreshRow(EntityRow row)
    {
        var e = row.Entity;
        int max = Mathf.Max(1, e.Stats.MaxHP);
        float ratio = Mathf.Clamp01((float)e.CurrentHP / max);
        row.HpFill.anchorMax = new Vector2(ratio, 1f);
        row.HpFill.offsetMin = row.HpFill.offsetMax = Vector2.zero;
        row.HpText.text = $"{e.CurrentHP}/{max}";

        if (e.IsAlly)
        {
            int sp = e.Ally.CurrentSP;
            int maxSp = Mathf.Max(1, e.Stats.MaxSP);
            row.ShieldText.text = $"SP {sp}/{maxSp}" + (e.Guarding ? "  [防]" : "");
        }
        else
        {
            row.ShieldText.text = e.IsBroken
                ? "<破防>"
                : (e.Shield > 0 ? new string('◆', e.Shield) : "");
        }

        row.NameText.color = e.IsAlive
            ? (e.IsAlly ? new Color(0.7f, 0.85f, 1f) : new Color(1f, 0.7f, 0.7f))
            : new Color(0.4f, 0.4f, 0.4f);
    }

    // ---------- 菜单 ----------

    private void ShowCommandMenu(BattleEntity actor)
    {
        _currentPlayer = actor;
        _resultPanel.gameObject.SetActive(false);
        ClearChildren(_menuPanel);

        MakeButton(_menuPanel, "攻击", () => _controller.BeginCommand(CommandType.Attack), new Vector2(0, 46));
        MakeButton(_menuPanel, "技能 ▸", ShowSkillMenu, new Vector2(0, 46));
        MakeButton(_menuPanel, "道具 ▸", ShowItemMenu, new Vector2(0, 46));

        var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(_menuPanel, false);
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = true; hlg.childControlWidth = true;
        MakeButton(row.transform, "防御", () => _controller.BeginCommand(CommandType.Guard), new Vector2(0, 46));
        MakeButton(row.transform, "逃跑", () => _controller.BeginCommand(CommandType.Flee), new Vector2(0, 46));
    }

    private void ShowSkillMenu()
    {
        ClearChildren(_menuPanel);
        var allyDef = _currentPlayer.Ally.Definition as AllyDefinitionSO;
        if (allyDef != null && allyDef.Skills != null)
        {
            foreach (var skill in allyDef.Skills)
            {
                var captured = skill;
                var btn = MakeButton(_menuPanel, $"{skill.SkillName}（{skill.SpCost} SP）",
                    () => _controller.BeginCommand(CommandType.Skill, captured), new Vector2(0, 44));
                btn.interactable = _currentPlayer.CurrentSP >= skill.SpCost;
            }
        }
        MakeButton(_menuPanel, "返回", () => ShowCommandMenu(_currentPlayer), new Vector2(0, 44));
    }

    private void ShowItemMenu()
    {
        ClearChildren(_menuPanel);
        if (InventoryManager.Instance != null)
        {
            var items = InventoryManager.Instance.GetAllPartyItems();
            foreach (var inv in items)
            {
                if (inv == null || !inv.IsConsumable || inv.Quantity <= 0)
                    continue;
                var consumable = inv.ItemDefinition as ConsumableItemSO;
                if (consumable == null)
                    continue;
                var captured = consumable;
                MakeButton(_menuPanel, $"{consumable.ItemName} ×{inv.Quantity}",
                    () => _controller.BeginCommand(CommandType.Item, null, captured), new Vector2(0, 44));
            }
        }
        MakeButton(_menuPanel, "返回", () => ShowCommandMenu(_currentPlayer), new Vector2(0, 44));
    }

    private void ShowTargetMenu(CommandTargetRequest request)
    {
        ClearChildren(_menuPanel);
        var title = MakeText(_menuPanel, request.TargetSide == BattleSide.Enemy ? "选择攻击目标" : "选择作用目标",
            20, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 36));
        title.GetComponent<LayoutElement>().preferredWidth = 800;

        foreach (var candidate in request.Candidates)
        {
            var captured = candidate;
            string state = candidate.IsAlive ? $"{candidate.DisplayName}  HP {candidate.CurrentHP}" : $"{candidate.DisplayName}（阵亡）";
            var btn = MakeButton(_menuPanel, state, () =>
                _controller.SubmitTargets(new List<BattleEntity> { captured }), new Vector2(0, 44));
            btn.interactable = candidate.IsAlive || request.Command == CommandType.Item;
        }
        MakeButton(_menuPanel, "取消", () => _controller.CancelTargetSelection(), new Vector2(0, 44));
    }

    // ---------- 事件 ----------

    private void SubscribeAll()
    {
        EventBus.Subscribe(this as IEventReceiver<BattleStartedEvent>);
        EventBus.Subscribe(this as IEventReceiver<EntityTurnStartedEvent>);
        EventBus.Subscribe(this as IEventReceiver<AwaitPlayerCommandEvent>);
        EventBus.Subscribe(this as IEventReceiver<CommandTargetRequestEvent>);
        EventBus.Subscribe(this as IEventReceiver<EntityDamagedEvent>);
        EventBus.Subscribe(this as IEventReceiver<EntityHealedEvent>);
        EventBus.Subscribe(this as IEventReceiver<ShieldBrokenEvent>);
        EventBus.Subscribe(this as IEventReceiver<TurnOrderBuiltEvent>);
        EventBus.Subscribe(this as IEventReceiver<BattleLogEvent>);
        EventBus.Subscribe(this as IEventReceiver<BattleResultReadyEvent>);
        EventBus.Subscribe(this as IEventReceiver<BattleEndedEvent>);
    }

    private void UnsubscribeAll()
    {
        EventBus.Unsubscribe(this as IEventReceiver<BattleStartedEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<EntityTurnStartedEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<AwaitPlayerCommandEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<CommandTargetRequestEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<EntityDamagedEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<EntityHealedEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<ShieldBrokenEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<TurnOrderBuiltEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<BattleLogEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<BattleResultReadyEvent>);
        EventBus.Unsubscribe(this as IEventReceiver<BattleEndedEvent>);
    }

    public void OnEvent(BattleStartedEvent evt) => BuildEntityRows(evt.AllEntities);

    public void OnEvent(EntityTurnStartedEvent evt)
    {
        ClearChildren(_menuPanel);
        RefreshAllRows();
    }

    public void OnEvent(AwaitPlayerCommandEvent evt) => ShowCommandMenu(evt.Actor);

    public void OnEvent(CommandTargetRequestEvent evt) => ShowTargetMenu(evt.Request);

    public void OnEvent(EntityDamagedEvent evt)
    {
        if (_rows.TryGetValue(evt.Target.Id, out var row))
            RefreshRow(row);

        Color color = Color.white;
        string text = evt.Damage.ToString();
        if (evt.BrokeShield) { color = new Color(1f, 0.85f, 0.3f); text = "破防！"; }
        else if (evt.IsCritical) { color = new Color(1f, 0.85f, 0.3f); text = "暴击 " + evt.Damage; }
        else if (evt.IsWeakness) { color = new Color(0.8f, 0.6f, 1f); }
        else { color = new Color(1f, 0.55f, 0.55f); }
        SpawnDamage(text, color);
    }

    public void OnEvent(EntityHealedEvent evt)
    {
        if (_rows.TryGetValue(evt.Target.Id, out var row))
            RefreshRow(row);
        SpawnDamage("+" + evt.Amount, new Color(0.5f, 1f, 0.55f));
    }

    public void OnEvent(ShieldBrokenEvent evt)
    {
        if (_rows.TryGetValue(evt.Enemy.Id, out var row))
            RefreshRow(row);
        SpawnDamage("SHIELD BREAK", new Color(0.7f, 0.85f, 1f));
    }

    public void OnEvent(TurnOrderBuiltEvent evt)
    {
        var sb = new StringBuilder("行动顺序\n");
        int shown = 0;
        foreach (var entity in evt.Order)
        {
            if (shown++ >= 8) break;
            sb.Append("→ ").Append(entity.DisplayName).Append('\n');
        }
        _orderText.text = sb.ToString();
        RefreshAllRows();
    }

    public void OnEvent(BattleLogEvent evt)
    {
        _logs.Add(evt.Message);
        if (_logs.Count > 6)
            _logs.RemoveAt(0);
        _logText.text = string.Join("\n", _logs);
    }

    public void OnEvent(BattleResultReadyEvent evt)
    {
        ClearChildren(_menuPanel);
        _resultPanel.gameObject.SetActive(true);
        var title = _resultPanel.Find("Panel/resultTitle")?.GetComponent<Text>();
        var body = _resultPanel.Find("Panel/Body")?.GetComponent<Text>();
        if (title != null)
        {
            title.text = evt.Result switch
            {
                BattleResult.Victory => "战斗胜利",
                BattleResult.Defeat => "战斗失败",
                BattleResult.Escape => "成功逃跑",
                _ => "战斗结束"
            };
            title.color = evt.Result == BattleResult.Victory ? new Color(0.7f, 1f, 0.7f) : Color.white;
        }
        if (body != null)
        {
            var sb = new StringBuilder();
            if (evt.Result == BattleResult.Victory)
            {
                sb.AppendLine($"获得经验：{evt.ExpGained}");
                sb.AppendLine($"获得金币：{evt.MoneyGained}");
                if (evt.LevelUpMessages != null)
                    foreach (var line in evt.LevelUpMessages)
                        sb.AppendLine(line);
                if (evt.Drops != null && evt.Drops.Count > 0)
                {
                    sb.AppendLine("掉落：");
                    foreach (var drop in evt.Drops)
                        sb.AppendLine($"  {drop.ItemDefinition.ItemName} ×{drop.Quantity}");
                }
            }
            else
            {
                sb.AppendLine("队伍已被转移到安全状态。");
            }
            body.text = sb.ToString();
        }
    }

    public void OnEvent(BattleEndedEvent evt)
    {
        ClearChildren(_menuPanel);
    }

    private void SpawnDamage(string text, Color color)
    {
        var pos = new Vector2(UnityEngine.Random.Range(-260, 260), UnityEngine.Random.Range(60, 220));
        _damagePool.Spawn(pos, text, color);
    }
}
