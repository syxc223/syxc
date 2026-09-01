using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 金币UI显示
/// 监听CoinSystem的金币变化并更新UI文本
/// </summary>
public class CoinUI : MonoBehaviour
{
    [SerializeField] private Text coinText; // 显示金币的Text组件
    [SerializeField] private string displayFormat = "金币: {0}"; // 显示格式

    private void OnEnable()
    {
        // 订阅金币变化事件
        CoinSystem.OnCoinsChanged += UpdateCoinDisplay;
        
        // 初始化显示
        UpdateCoinDisplay(CoinSystem.CoinCount);
    }

    private void OnDisable()
    {
        // 取消订阅事件
        CoinSystem.OnCoinsChanged -= UpdateCoinDisplay;
    }

    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateCoinDisplay(int coinCount)
    {
        if (coinText != null)
        {
            coinText.text = string.Format(displayFormat, coinCount);
        }
    }
}

