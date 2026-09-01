using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 付费传送门UI
/// 显示支付确认面板
/// </summary>
public class PaidExitUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject paymentPanel; // 支付面板（父对象）
    [SerializeField] private Text messageText; // 提示文本
    [SerializeField] private Text coinAmountText; // 金币数量文本
    [SerializeField] private Button confirmButton; // 确认支付按钮
    [SerializeField] private Button cancelButton; // 取消按钮

    [Header("消息设置")]
    [SerializeField] private string paymentMessage = "是否支付 {0} 金币进入下一个场景？";
    [SerializeField] private string insufficientCoinsMessage = "金币不足！需要 {0} 金币，当前只有 {1} 金币。";

    private System.Action onPaymentComplete; // 支付完成回调
    private System.Action onPaymentCancel; // 支付取消回调
    private int requiredCoins; // 需要的金币数量

    private static PaidExitUI instance; // 单例实例
    public static PaidExitUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PaidExitUI>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("PaidExitUI");
                    instance = obj.AddComponent<PaidExitUI>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // 确保在场景切换时不被销毁
        
        // 监听场景加载事件，确保UI引用在新场景中正确
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        if (paymentPanel != null)
        {
            paymentPanel.SetActive(false); // 初始隐藏
        }

        // 绑定按钮事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmPayment);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelPayment);
        }
    }
    
    private void OnDestroy()
    {
        // 取消监听场景加载事件
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// 场景加载完成时的回调
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 确保游戏时间恢复正常
        Time.timeScale = 1f;
        
        // 重新查找UI元素（因为场景切换时旧场景的UI被销毁了）
        ReinitializeUIReferences();
        
        // 隐藏支付面板（如果还在显示）
        if (paymentPanel != null)
        {
            paymentPanel.SetActive(false);
        }
        
        // 重置回调（防止旧场景的回调被调用）
        onPaymentComplete = null;
        onPaymentCancel = null;
        
        Debug.Log("[PaidExitUI] 场景加载完成，已重置UI状态并重新查找UI引用");
    }
    
    /// <summary>
    /// 重新初始化UI引用（场景切换时调用）
    /// </summary>
    private void ReinitializeUIReferences()
    {
        // 如果UI引用已失效（为null），尝试重新查找
        if (paymentPanel == null)
        {
            Debug.LogWarning("[PaidExitUI] paymentPanel 引用已失效，尝试重新查找...");
            
            // 尝试通过名称查找支付面板
            GameObject foundPanel = GameObject.Find("PaymentPanel");
            if (foundPanel == null)
            {
                // 尝试查找包含"Payment"或"Paid"或"支付"的GameObject（递归查找所有子对象）
                GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (GameObject obj in allObjects)
                {
                    // 递归查找所有子对象
                    foundPanel = FindChildByName(obj.transform, "PaymentPanel");
                    if (foundPanel == null)
                    {
                        foundPanel = FindChildByName(obj.transform, "PaidExitPanel");
                    }
                    if (foundPanel == null && (obj.name.Contains("Payment") || obj.name.Contains("Paid") || obj.name.Contains("支付")))
                    {
                        foundPanel = obj;
                        break;
                    }
                    if (foundPanel != null) break;
                }
            }
            
            if (foundPanel != null)
            {
                paymentPanel = foundPanel;
                Debug.Log($"[PaidExitUI] 重新找到支付面板: {foundPanel.name}");
                
                // 重新查找子组件
                Text[] texts = foundPanel.GetComponentsInChildren<Text>(true); // 包括未激活的对象
                if (texts.Length >= 1)
                {
                    messageText = texts[0];
                }
                if (texts.Length >= 2)
                {
                    coinAmountText = texts[1];
                }
                
                // 重新查找按钮
                Button[] buttons = foundPanel.GetComponentsInChildren<Button>(true); // 包括未激活的对象
                if (buttons.Length >= 2)
                {
                    confirmButton = buttons[0];
                    cancelButton = buttons[1];
                }
                else if (buttons.Length == 1)
                {
                    confirmButton = buttons[0];
                }
                
                // 重新绑定按钮事件
                if (confirmButton != null)
                {
                    confirmButton.onClick.RemoveAllListeners();
                    confirmButton.onClick.AddListener(OnConfirmPayment);
                }
                if (cancelButton != null)
                {
                    cancelButton.onClick.RemoveAllListeners();
                    cancelButton.onClick.AddListener(OnCancelPayment);
                }
            }
            else
            {
                Debug.LogWarning("[PaidExitUI] 场景切换后未找到支付面板！请确保新场景中有名为 'PaymentPanel' 或包含 'Payment'/'Paid'/'支付' 的GameObject。");
            }
        }
        else
        {
            // UI引用仍然有效，但需要重新绑定按钮事件（防止重复绑定）
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmPayment);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnCancelPayment);
            }
        }
    }
    
    /// <summary>
    /// 递归查找子对象（按名称）
    /// </summary>
    private GameObject FindChildByName(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent.gameObject;
        }
        
        foreach (Transform child in parent)
        {
            GameObject found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 显示支付确认面板
    /// </summary>
    /// <param name="coins">需要的金币数量</param>
    /// <param name="onComplete">支付完成回调</param>
    /// <param name="onCancel">支付取消回调</param>
    public void ShowPaymentPanel(int coins, System.Action onComplete, System.Action onCancel)
    {
        requiredCoins = coins;
        onPaymentComplete = onComplete;
        onPaymentCancel = onCancel;

        // 如果 paymentPanel 为 null，尝试重新查找
        if (paymentPanel == null)
        {
            Debug.LogWarning("[PaidExitUI] ShowPaymentPanel: paymentPanel 为 null，尝试重新查找...");
            ReinitializeUIReferences();
        }

        if (paymentPanel != null)
        {
            paymentPanel.SetActive(true);
            Time.timeScale = 0f; // 暂停游戏
            UpdateUI();
        }
        else
        {
            Debug.LogError("[PaidExitUI] paymentPanel 未设置且无法自动查找！请确保场景中有支付面板UI。");
            onCancel?.Invoke(); // 如果面板未设置，调用取消回调
        }
    }

    /// <summary>
    /// 隐藏支付确认面板
    /// </summary>
    public void HidePaymentPanel()
    {
        if (paymentPanel != null)
        {
            paymentPanel.SetActive(false);
            Time.timeScale = 1f; // 恢复游戏
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        int currentCoins = CoinSystem.CoinCount;
        bool canAfford = currentCoins >= requiredCoins;

        // 更新提示文本
        if (messageText != null)
        {
            if (canAfford)
            {
                messageText.text = string.Format(paymentMessage, requiredCoins);
            }
            else
            {
                messageText.text = string.Format(insufficientCoinsMessage, requiredCoins, currentCoins);
            }
        }

        // 更新金币数量文本
        if (coinAmountText != null)
        {
            coinAmountText.text = $"当前金币: {currentCoins} / 需要: {requiredCoins}";
        }

        // 更新确认按钮状态
        if (confirmButton != null)
        {
            confirmButton.interactable = canAfford;
            // 可以在这里改变按钮颜色或文本
            Text buttonText = confirmButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = canAfford ? $"支付 {requiredCoins} 金币" : "金币不足";
            }
        }
    }

    /// <summary>
    /// 确认支付按钮点击事件
    /// </summary>
    private void OnConfirmPayment()
    {
        int currentCoins = CoinSystem.CoinCount;
        
        if (currentCoins >= requiredCoins)
        {
            // 扣除金币
            bool success = CoinSystem.SpendCoins(requiredCoins);
            if (success)
            {
                Debug.Log($"PaidExitUI: 成功支付 {requiredCoins} 金币");
                HidePaymentPanel();
                onPaymentComplete?.Invoke();
                onPaymentComplete = null;
                onPaymentCancel = null;
            }
            else
            {
                Debug.LogError("PaidExitUI: 支付失败！");
            }
        }
        else
        {
            Debug.LogWarning($"PaidExitUI: 金币不足！需要 {requiredCoins}，当前只有 {currentCoins}");
            UpdateUI(); // 更新UI显示
        }
    }

    /// <summary>
    /// 取消支付按钮点击事件
    /// </summary>
    private void OnCancelPayment()
    {
        Debug.Log("PaidExitUI: 取消支付");
        HidePaymentPanel();
        onPaymentCancel?.Invoke();
        onPaymentComplete = null;
        onPaymentCancel = null;
    }
}

