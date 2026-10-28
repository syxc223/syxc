using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Pool;
using MFramework.Event;

public class InteractionUIController : MonoBehaviour,
IEventReceiver<InteractionChangedEvent>,
IEventReceiver<InteractionMenuRequestEvent>,
IEventReceiver<GameModeChangedEvent>
{
    [Header("Head Icon")]
    [SerializeField] private RectTransform actionIconHolder;
    [SerializeField] private GameObject actionIconPrefab;

    [Header("Menu Button")]
    [SerializeField] private RectTransform actionMenuHolder;
    [SerializeField] private GameObject actionMenuButtonPrefab;

    private ObjectPool<GameObject> _iconPool;
    private ObjectPool<GameObject> _menuButtonPool;
    private readonly List<GameObject> _activeIcons = new(8);
    private readonly List<GameObject> _activeButtons = new(8);

    private IReadOnlyList<ActionCommandInfo> _currentCommandList;

    private Transform _headAnchor;//交互对象头顶的挂点

    private InteractionBase _target;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
        InitPool();
        actionIconHolder.gameObject.SetActive(false);
        actionMenuHolder.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InteractionChangedEvent>(this);
        EventBus.Subscribe<InteractionMenuRequestEvent>(this);
        EventBus.Subscribe<GameModeChangedEvent>(this);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<InteractionChangedEvent>(this);
        EventBus.Unsubscribe<InteractionMenuRequestEvent>(this);
        EventBus.Unsubscribe<GameModeChangedEvent>(this);
    }



    private void LateUpdate()
    {
        if(_headAnchor == null)
            return;

        bool iconsActive = actionIconHolder.gameObject.activeSelf;
        bool menuActive = actionMenuHolder.gameObject.activeSelf;
        if(!iconsActive && !menuActive)
            return;

        UpdateOverlayPositions();
    }



    private void InitPool()
    {
        _iconPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(actionIconPrefab, actionIconHolder),
            actionOnGet: (obj) =>
            {
                obj.SetActive(true);
                obj.transform.SetAsLastSibling();
            },
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 8,
            maxSize: 20);

        _menuButtonPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(actionMenuButtonPrefab, actionMenuHolder),
            actionOnGet: (obj) =>
    {
        obj.SetActive(true);
        obj.transform.SetAsLastSibling();
    },
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 8,
            maxSize: 20);
    }

    private bool ValidateReferences()
    {
        bool ok = true;

        if (actionIconHolder == null) { Debug.LogError($"{nameof(InteractionUIController)} missing reference: {nameof(actionIconHolder)}", this); ok = false; }
        if (actionMenuHolder == null) { Debug.LogError($"{nameof(InteractionUIController)} missing reference: {nameof(actionMenuHolder)}", this); ok = false; }

        if (actionIconPrefab == null) { Debug.LogError($"{nameof(InteractionUIController)} missing reference: {nameof(actionIconPrefab)}", this); ok = false; }
        if (actionMenuButtonPrefab == null) { Debug.LogError($"{nameof(InteractionUIController)} missing reference: {nameof(actionMenuButtonPrefab)}", this); ok = false; }

        return ok;
    }

    private void UpdateOverlayPositions()
    {
        if(_target is null || !_target.isActiveAndEnabled)
        {
            HideHeadIcons();
            HideActionMenu();
            return;
        }

        var cam = Camera.main;
        if(cam == null)
            return;

        var screenPos = cam.WorldToScreenPoint(_headAnchor.position);

        // 锚点在相机背后时，投影坐标会镜像到屏幕另一侧，直接隐藏
        if(screenPos.z <= 0f)
        {
            HideHeadIcons();
            return;
        }

        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screenPos, null, out var localPoint))
            return;

        if(actionIconHolder.gameObject.activeSelf)
            actionIconHolder.localPosition = localPoint;

        if(actionMenuHolder.gameObject.activeSelf)
        {
            ClampToCanvas(actionMenuHolder, ref localPoint);
            actionMenuHolder.localPosition = localPoint;
        }
    }

    /// <summary>把 UI 元素位置钳制在画布范围内，防止 NPC 靠近屏幕边缘时菜单出屏</summary>
    private void ClampToCanvas(RectTransform holder, ref Vector2 localPoint)
    {
        var canvasRect = transform as RectTransform;
        Rect canvas = canvasRect.rect;

        float halfWidth = holder.rect.width * 0.5f;
        float halfHeight = holder.rect.height * 0.5f;
        const float margin = 10f;

        float minX = canvas.x + margin + halfWidth;
        float maxX = canvas.xMax - margin - halfWidth;
        float minY = canvas.y + margin + halfHeight;
        float maxY = canvas.yMax - margin - halfHeight;

        localPoint.x = minX > maxX ? canvas.center.x : Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = minY > maxY ? canvas.center.y : Mathf.Clamp(localPoint.y, minY, maxY);
    }


    /// <summary>打开菜单当帧立即定位到 NPC 头顶（强制重建布局以获得菜单实际尺寸）</summary>
    private void PositionMenu()
    {
        if(_headAnchor == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(actionMenuHolder);
        UpdateOverlayPositions();
    }

    #region 对象池相关方法
    private void SyncPool(List<GameObject> activeList, ObjectPool<GameObject> pool, int targetCount)
    {
        while (activeList.Count > targetCount)
        {
            int lastIndex = activeList.Count - 1;
            GameObject item = activeList[lastIndex];
            pool.Release(item);
            activeList.RemoveAt(lastIndex);
        }

        while (activeList.Count < targetCount)
        {
            GameObject item = pool.Get();
            activeList.Add(item);
        }
    }
    private void ReleaseAll(List<GameObject> activeList, ObjectPool<GameObject> pool)
    {
        for (int i = 0; i < activeList.Count; i++)
        {
            pool.Release(activeList[i]);
        }
        activeList.Clear();
    }

    #endregion


    #region 事件相关方法
    public void OnEvent(InteractionChangedEvent evt)
    {
        _target = evt.target;
        if(!evt.inRange || evt.target is null)
        {
            HideHeadIcons();
            HideActionMenu();
            _currentCommandList = null;
            _target = null;
            return;
        }
        //启动显示头顶icon
        _currentCommandList = evt.target.CachedCommandInfo;
        _headAnchor = evt.target.HeadAnchor;

        ShowHeadIcons();
    }


    #endregion
        public void OnEvent(InteractionMenuRequestEvent evt)
    {
        HideHeadIcons();

        _target = evt.target;
        _headAnchor = evt.target.HeadAnchor;
        actionMenuHolder.gameObject.SetActive(true);

        OpenMenu(evt.target);
        PositionMenu();
    }

        public void OnEvent(GameModeChangedEvent evt)
    {
        if(evt.newMode == global::GameMode.InteractionMenu)
            return;

            if(actionMenuHolder.gameObject.activeSelf)
             HideActionMenu();

            if(evt.newMode == global::GameMode.Explore)
        {
            if(_currentCommandList is not null &&_currentCommandList.Count>0)
             ShowHeadIcons();
             return;
        }

            HideHeadIcons();
    }

    private void ShowHeadIcons()
    {
        if(_currentCommandList.Count == 0)
         return;
        actionIconHolder.gameObject.SetActive(true);

        SyncPool(_activeIcons, _iconPool, _currentCommandList.Count);

        for (int i = 0; i < _activeIcons.Count; i++)
        {
            var obj = _activeIcons[i];
            var cmd = _currentCommandList[i];

            obj.GetComponent<Image>().sprite = cmd.Icon;
        }
    }
    private void OpenMenu(InteractionBase target)
{
    GameModeManager.Instance.RequestChangeMode(global::GameMode.InteractionMenu);
    SyncPool(_activeButtons, _menuButtonPool, _currentCommandList.Count);

    Button firstButton = null;

    for (int i = 0; i < _activeButtons.Count; i++)
    {
        var btn = _activeButtons[i];
        var cmd = _currentCommandList[i];

        int commandIndex = i;//闭包问题，必须引入一个局部变量

        btn.GetComponent<ActionMenuButton>().SetButton(cmd, () => 
        {
            target.ExecuteCommandFromUI(commandIndex);
                CloseMenu(false);
        });

        if(firstButton == null)
         firstButton = btn.GetComponent<Button>();
    }

    if(firstButton != null)
    {
     firstButton.Select();//默认选中第一个按钮
     EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }
}
  private void CloseMenu(bool restoreHeadIcons)
    {

        HideActionMenu();
        if(restoreHeadIcons)
         ShowHeadIcons();
         else
         HideHeadIcons();
}
    private void HideHeadIcons()
{
    actionIconHolder?.gameObject.SetActive(false);
    ReleaseAll(_activeIcons, _iconPool);
}

    private void HideActionMenu()
{
    actionMenuHolder?.gameObject.SetActive(false);
    ReleaseAll(_activeButtons, _menuButtonPool);
}


}
