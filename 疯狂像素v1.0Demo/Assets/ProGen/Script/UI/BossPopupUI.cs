using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Boss弹窗UI组件（简化版）
/// 当Boss第一次检测到玩家时显示弹窗，1.5秒后自动关闭
/// </summary>
public class BossPopupUI : MonoBehaviour
{
    private static BossPopupUI instance;
    
    [Header("弹窗UI元素")]
    [SerializeField] private GameObject popupPanel; // 弹窗面板
    [SerializeField] private Text bossNameText; // Boss名称文本（Legacy Text）
    
    [Header("弹窗设置")]
    [SerializeField] private string defaultBossName = "变种幽浮"; // 默认Boss名称
    [SerializeField] private float displayDuration = 1.5f; // 弹窗显示时长（秒）
    
    private CanvasGroup canvasGroup; // 用于淡入淡出效果
    private bool isShowing = false; // 是否正在显示
    private Coroutine currentCoroutine; // 当前运行的协程
    
    public static BossPopupUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BossPopupUI>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // 确保只有一个实例
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 保持弹窗在场景切换时不被销毁
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePopup();
    }
    
    private void Start()
    {
        // 初始状态隐藏弹窗
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 初始化弹窗
    /// </summary>
    private void InitializePopup()
    {
        // 如果没有手动设置popupPanel，使用当前GameObject
        if (popupPanel == null)
        {
            popupPanel = gameObject;
        }
        
        // 添加或获取CanvasGroup用于淡入淡出
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 自动查找Text组件
        if (bossNameText == null)
        {
            bossNameText = GetComponentInChildren<Text>();
        }
        
        // 设置默认文本
        if (bossNameText != null)
        {
            bossNameText.text = defaultBossName;
            bossNameText.alignment = TextAnchor.MiddleCenter;
        }
        
        Debug.Log($"[BossPopupUI] 弹窗初始化完成");
    }
    
    /// <summary>
    /// 显示Boss弹窗
    /// </summary>
    /// <param name="bossName">Boss名称（可选，如果为空则使用默认名称）</param>
    public void ShowBossPopup(string bossName = "")
    {
        // 如果正在显示，忽略新的请求
        if (isShowing)
        {
            return;
        }
        
        // 检查关键组件
        if (popupPanel == null)
        {
            Debug.LogError("[BossPopupUI] 错误：popupPanel为null！无法显示弹窗。");
            return;
        }
        
        // 停止之前的协程（如果有）
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        
        // 设置Boss名称
        string displayName = string.IsNullOrEmpty(bossName) ? defaultBossName : bossName;
        if (bossNameText != null)
        {
            bossNameText.text = displayName;
        }
        
        // 启动显示协程
        currentCoroutine = StartCoroutine(ShowPopupCoroutine());
    }
    
    /// <summary>
    /// 显示弹窗的协程
    /// </summary>
    private IEnumerator ShowPopupCoroutine()
    {
        isShowing = true;
        
        // 显示面板
        popupPanel.SetActive(true);
        
        // 确保CanvasGroup存在
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 确保弹窗在最上层
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 100; // 设置较高的排序顺序
        }
        
        // 淡入效果
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        float fadeInDuration = 0.2f; // 淡入时间
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        
        // 等待显示时长
        yield return new WaitForSecondsRealtime(displayDuration);
        
        // 淡出效果
        float fadeOutDuration = 0.2f; // 淡出时间
        elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        
        // 隐藏面板
        popupPanel.SetActive(false);
        
        isShowing = false;
        currentCoroutine = null;
    }
    
    /// <summary>
    /// 强制隐藏弹窗（供外部调用）
    /// </summary>
    public void HidePopup()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        isShowing = false;
    }
}
