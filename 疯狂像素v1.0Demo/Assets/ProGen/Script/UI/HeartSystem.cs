using UnityEngine;
using UnityEngine.UI;
public class HeartSystem : MonoBehaviour
{
    private const int HEART_ROW_SIZE = 8;//每行心形图标数量
    private const int HEART_OFFSET = 32;//心形图标间距
    [SerializeField] private Sprite[] heartquadSprites;//心形图标精灵数组
    [SerializeField] private GameObject heartPrefab;//心形图标预制体
    [SerializeField] private RectTransform heartContainer;//心形图标容器
    private float lastHealth = -1f;//上一次的生命值
    private float lastMaxHealth = -1f;//上一次的最大生命值

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeHearts();
    }

    // Update is called once per frame
    void Update()
    {
        // 获取当前最大生命值
        float currentMaxHealth = PlayerStats.MaxHealth > 0 ? PlayerStats.MaxHealth : 5.0f;
        
        // 如果最大生命值发生变化，重新初始化心形图标
        if (currentMaxHealth != lastMaxHealth)
        {
            Debug.Log($"HeartSystem: 最大生命值变化 {lastMaxHealth} -> {currentMaxHealth}，重新初始化心形图标");
            InitializeHearts();
            lastMaxHealth = currentMaxHealth;
        }
        
        //仅在生命值发生变化时更新
        if(PlayerHealth.Health != lastHealth)
        {
            UpdateHearts();
            lastHealth = PlayerHealth.Health;
        }
    }
    void InitializeHearts()
    {
        foreach(Transform child in heartContainer)
        {
            Destroy(child.gameObject);//销毁现有的心形图标
        }

        // 获取当前最大生命值（使用PlayerStats.MaxHealth）
        float currentMaxHealth = PlayerStats.MaxHealth > 0 ? PlayerStats.MaxHealth : 5.0f;
        lastMaxHealth = currentMaxHealth;

        //需要计算所需数量的心形图标（最多 12 颗心）
        int maxHearts = Mathf.Min(Mathf.CeilToInt(currentMaxHealth), 12);
        
        Debug.Log($"HeartSystem: 初始化心形图标，最大生命值: {currentMaxHealth}, 心形数量: {maxHearts}");

        //创建心形图标
        for(int i = 0; i < maxHearts; i++)
        {
            GameObject newHeart = Instantiate(heartPrefab, heartContainer);//实例化心形图标预制体
            RectTransform heartRect = newHeart.GetComponent<RectTransform>();//获取心形图标的RectTransform组件

            //计算心形图标的位置
            float x = (i % HEART_ROW_SIZE) * HEART_OFFSET;//计算x坐标
            float y = -(i / HEART_ROW_SIZE) * HEART_OFFSET;//计算y坐标
            heartRect.anchoredPosition = new Vector2(x, y);//设置心形图
        }
        UpdateHearts();//更新心形图标显示
    }
    void UpdateHearts()
    {
        // 检查数组是否有效
        if (heartquadSprites == null || heartquadSprites.Length < 5)
        {
            Debug.LogWarning("HeartSystem: heartquadSprites 数组未正确初始化或长度不足（需要至少5个元素）");
            return;
        }

        int index = 0;
        float currentHealth = PlayerHealth.Health;//获取当前生命值
        int fullHeartCount = Mathf.FloorToInt(currentHealth);//完整心形图标数量
        float fractionalPart = currentHealth - fullHeartCount;//当前生命值的小数部分

        foreach (Transform heart in heartContainer)
        {
            Image heartImage = heart.GetComponent<Image>();//获取心形图标的Image组件
            if (heartImage == null) continue;

            if (index < fullHeartCount)
            {
                // 完整的心
                heartImage.sprite = heartquadSprites[0];//设置为满心形图标
            }
            else if (index == fullHeartCount && fractionalPart > 0f)
            {
                // 部分填充的心（根据小数部分计算帧数，范围0-3）
                int frame = Mathf.Clamp(Mathf.FloorToInt(fractionalPart * 4), 0, 3);
                heartImage.sprite = heartquadSprites[frame];//设置为部分心形图标
            }
            else
            {
                // 空的心
                heartImage.sprite = heartquadSprites[4];//设置为空心形图标
            }
            index++;
        }
    }

    /// <summary>
    /// 强制刷新心形图标（供外部调用）
    /// </summary>
    public void RefreshHearts()
    {
        InitializeHearts();
    }
}
