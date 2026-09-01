using UnityEngine;
using TMPro;

/// <summary>
/// 在 UI 上显示还剩多少豆（不使用阿拉伯数字）。
/// 把这个脚本挂到场景中的某个物体上，并在 Inspector 里把 Text 引用拖进来即可。
/// </summary>
public class PelletRemainingUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI remainingText;

    [Header("刷新间隔（秒）")]
    public float refreshInterval = 0.3f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            UpdateRemainingText();
        }
    }

    void UpdateRemainingText()
    {
        if (remainingText == null) return;

        // 直接统计场景中还存在多少 Pellet
        int remaining = FindObjectsOfType<Pellet>().Length;

        string numberText = ToChineseNumber(remaining);
        remainingText.text = "还剩" + numberText + "颗豆";
    }

    /// <summary>
    /// 将整数转换为中文数字表示（零到九十九）。
    /// 例如：0→零，5→五，10→十，12→十二，20→二十，34→三十四。
    /// </summary>
    string ToChineseNumber(int value)
    {
        if (value <= 0)
        {
            return "零";
        }

        if (value > 99)
        {
            value = 99;
        }

        string[] digits =
        {
            "零","一","二","三","四","五","六","七","八","九"
        };

        if (value < 10)
        {
            return digits[value];
        }

        int tens = value / 10;
        int ones = value % 10;

        if (value == 10)
        {
            return "十";
        }

        if (tens == 1)
        {
            // 十一、十二……
            return ones == 0 ? "十" : "十" + digits[ones];
        }

        // 二十、三十……
        string result = digits[tens] + "十";
        if (ones != 0)
        {
            result += digits[ones];
        }

        return result;
    }
}

