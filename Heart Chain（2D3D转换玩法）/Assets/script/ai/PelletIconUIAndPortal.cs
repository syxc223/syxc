using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 使用图像显示吃豆进度，并在吃够指定数量后生成传送门。
/// 用法：
/// 1. 在场景中新建一个空物体（例如 UIManager），挂上本脚本。
/// 2. 在 Canvas 里准备若干个 Image（代表豆子格子），拖到 beanSlots 数组中。
/// 3. 为空格子 / 已吃到的格子准备两张 Sprite，分别赋给 emptySprite / filledSprite。
/// 4. 指定 portalPrefab（带触发器的传送门预制体）和 portalSpawnPoint（生成位置）。
/// 5. 在传送门预制体上挂 PortalSceneLoader，并设置目标场景名。
/// </summary>
public class PelletIconUIAndPortal : MonoBehaviour
{
    [Header("豆子进度 UI")]
    public Image[] beanSlots;          // 图标格子，从左到右排好
    public Sprite emptySprite;         // 未获得时的图标
    public Sprite filledSprite;        // 已获得时的图标

    [Header("传送门条件")]
    public int beansNeededForPortal = 5;   // 吃到多少豆子后出现传送门

    [Header("传送门设置")]
    public GameObject portalPrefab;
    public Transform portalSpawnPoint;

    [Header("刷新间隔（秒）")]
    public float refreshInterval = 0.2f;

    private int totalPellets;
    private bool portalSpawned;
    private float timer;

    void Start()
    {
        // 记录初始豆子总数
        Pellet[] pellets = FindObjectsOfType<Pellet>();
        totalPellets = pellets.Length;

        UpdateUIAndPortal();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            UpdateUIAndPortal();
        }
    }

    void UpdateUIAndPortal()
    {
        int remaining = FindObjectsOfType<Pellet>().Length;
        int collected = Mathf.Clamp(totalPellets - remaining, 0, totalPellets);

        UpdateIcons(collected);
        TrySpawnPortal(collected);
    }

    void UpdateIcons(int collected)
    {
        if (beanSlots == null || beanSlots.Length == 0) return;

        for (int i = 0; i < beanSlots.Length; i++)
        {
            if (beanSlots[i] == null) continue;

            bool filled = i < collected;
            Sprite targetSprite = filled ? filledSprite : emptySprite;

            if (targetSprite != null)
            {
                beanSlots[i].sprite = targetSprite;
            }

            // 也可以通过颜色高亮，防止没有提供不同的贴图
            beanSlots[i].color = filled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
    }

    void TrySpawnPortal(int collected)
    {
        if (portalSpawned) return;

        if (collected >= beansNeededForPortal)
        {
            if (portalPrefab != null && portalSpawnPoint != null)
            {
                GameObject portal = Instantiate(portalPrefab, portalSpawnPoint.position, portalSpawnPoint.rotation);
                portalSpawned = true;

                // 尝试触发相机过场
                PortalRevealCamera revealCam = FindObjectOfType<PortalRevealCamera>();
                if (revealCam != null)
                {
                    revealCam.PlayReveal(portal.transform);
                }
            }
        }
    }
}

