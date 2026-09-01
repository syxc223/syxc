using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 漫画面板控制器
/// 实现点击屏幕逐张显示图片的效果
/// </summary>
public class ComicPanelController : MonoBehaviour
{
    [Header("图片设置")]
    [SerializeField] private Image[] comicPanels; // 漫画图片数组（按顺序）
    [SerializeField] private float fadeInDuration = 0.5f; // 淡入动画时长（秒）
    [SerializeField] private float panelDisplayTime = 2f; // 每张图片显示时间（秒，可选）
    
    [Header("输入设置")]
    [SerializeField] private KeyCode nextPanelKey = KeyCode.Space; // 切换到下一张的按键
    [SerializeField] private bool useMouseClick = true; // 是否使用鼠标点击
    
    [Header("自动播放（可选）")]
    [SerializeField] private bool autoPlay = false; // 是否自动播放
    [SerializeField] private float autoPlayDelay = 3f; // 自动播放延迟（秒）
    
    [Header("开始效果设置")]
    [SerializeField] private float startBlackScreenFadeOutDuration = 1f; // 开始时黑屏淡出时长（秒）
    
    [Header("结束效果设置")]
    [SerializeField] private Image blackScreen; // 黑屏遮罩
    [SerializeField] private Text storyText; // 故事文本
    [SerializeField] private string storyMessage = "你的头脑愈发昏沉，然后眼前一黑，昏了过去"; // 故事文本内容
    [SerializeField] private Button continueButton; // 继续按钮
    [SerializeField] private float blackScreenFadeDuration = 2f; // 黑屏淡入时长（秒）
    [SerializeField] private float textFadeInDuration = 1f; // 文字淡入时长（秒）
    [SerializeField] private float textDelay = 1f; // 文字显示延迟（秒）
    [SerializeField] private float buttonDelay = 2f; // 按钮显示延迟（秒）
    
    [Header("场景切换设置")]
    [SerializeField] private string nextSceneName = ""; // 下一个场景名称（如果为空则加载下一个场景索引）
    [SerializeField] private int nextSceneIndex = -1; // 下一个场景索引（如果为-1则使用场景名称）
    
    private int currentPanelIndex = 0; // 当前显示的图片索引
    private bool isTransitioning = false; // 是否正在过渡中
    private bool allPanelsShown = false; // 所有图片是否已显示

    private void Start()
    {
        InitializePanels();
        InitializeEndEffects();
        StartCoroutine(PlayStartSequence());
    }

    /// <summary>
    /// 播放开始序列（黑屏淡出 -> 显示第一张图片）
    /// </summary>
    private IEnumerator PlayStartSequence()
    {
        // 开始时黑屏完全显示
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            Color color = blackScreen.color;
            color.a = 1f; // 完全不透明
            blackScreen.color = color;
        }

        // 隐藏所有漫画图片
        if (comicPanels != null)
        {
            foreach (var panel in comicPanels)
            {
                if (panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }
        }

        // 等待一帧确保UI更新
        yield return null;

        // 淡出黑屏
        if (blackScreen != null)
        {
            yield return StartCoroutine(FadeOutImage(blackScreen, startBlackScreenFadeOutDuration));
        }

        // 黑屏淡出后，显示第一张图片
        if (comicPanels != null && comicPanels.Length > 0 && comicPanels[0] != null)
        {
            comicPanels[0].gameObject.SetActive(true);
            comicPanels[0].color = new Color(1f, 1f, 1f, 1f);
            Debug.Log("ComicPanelController: 黑屏淡出完成，显示第一张图片");
        }
    }

    /// <summary>
    /// 初始化结束效果（黑屏、文字、按钮）
    /// </summary>
    private void InitializeEndEffects()
    {
        // 初始化黑屏（在PlayStartSequence中会设置为完全不透明，这里先设置为透明）
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            Color color = blackScreen.color;
            color.a = 0f; // 初始透明，PlayStartSequence会设置为1
            blackScreen.color = color;
        }

        // 初始化文字（隐藏）
        if (storyText != null)
        {
            storyText.gameObject.SetActive(false);
            Color color = storyText.color;
            color.a = 0f;
            storyText.color = color;
        }

        // 初始化按钮（隐藏）
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinueButtonClick);
        }
    }

    private void Update()
    {
        // 检查输入
        if (!isTransitioning && !allPanelsShown)
        {
            bool inputDetected = false;
            
            // 检查键盘输入（使用新的 Input System）
            if (Keyboard.current != null)
            {
                // 检查空格键（默认按键）
                if (nextPanelKey == KeyCode.Space && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    inputDetected = true;
                }
                // 检查其他按键
                else if (Keyboard.current[GetKeyCodeFromKeyCode(nextPanelKey)].wasPressedThisFrame)
                {
                    inputDetected = true;
                }
            }
            
            // 检查鼠标点击（使用新的 Input System）
            if (useMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                inputDetected = true;
            }
            
            if (inputDetected)
            {
                ShowNextPanel();
            }
        }
        
        // 自动播放
        if (autoPlay && !isTransitioning && !allPanelsShown)
        {
            StartCoroutine(AutoPlayNextPanel());
        }
    }

    /// <summary>
    /// 将旧的 KeyCode 转换为新的 Key 枚举（简化版本，只处理常用按键）
    /// </summary>
    private Key GetKeyCodeFromKeyCode(KeyCode keyCode)
    {
        // 常用按键映射
        switch (keyCode)
        {
            case KeyCode.Space: return Key.Space;
            case KeyCode.Return: return Key.Enter;
            case KeyCode.Escape: return Key.Escape;
            case KeyCode.LeftArrow: return Key.LeftArrow;
            case KeyCode.RightArrow: return Key.RightArrow;
            case KeyCode.UpArrow: return Key.UpArrow;
            case KeyCode.DownArrow: return Key.DownArrow;
            default: return Key.Space; // 默认返回空格键
        }
    }

    /// <summary>
    /// 初始化所有面板
    /// </summary>
    private void InitializePanels()
    {
        if (comicPanels == null || comicPanels.Length == 0)
        {
            Debug.LogWarning("ComicPanelController: 未设置漫画图片数组！");
            return;
        }

        // 隐藏所有图片（第一张会在PlayStartSequence中显示）
        for (int i = 0; i < comicPanels.Length; i++)
        {
            if (comicPanels[i] != null)
            {
                comicPanels[i].gameObject.SetActive(false);
            }
        }

        currentPanelIndex = 0;
        Debug.Log($"ComicPanelController: 初始化完成，共 {comicPanels.Length} 张图片");
    }

    /// <summary>
    /// 显示下一张图片
    /// </summary>
    public void ShowNextPanel()
    {
        if (isTransitioning || allPanelsShown)
        {
            return;
        }

        StartCoroutine(TransitionToNextPanel());
    }

    /// <summary>
    /// 过渡到下一张图片
    /// </summary>
    private IEnumerator TransitionToNextPanel()
    {
        isTransitioning = true;

        // 移动到下一张（不隐藏上一张）
        currentPanelIndex++;

        // 检查是否还有下一张
        if (currentPanelIndex < comicPanels.Length)
        {
            // 显示下一张图片
            if (comicPanels[currentPanelIndex] != null)
            {
                comicPanels[currentPanelIndex].gameObject.SetActive(true);
                yield return StartCoroutine(FadeIn(comicPanels[currentPanelIndex]));
                Debug.Log($"ComicPanelController: 显示第 {currentPanelIndex + 1} 张图片");
                
                // 等待面板显示时间（如果设置了）
                if (panelDisplayTime > 0f)
                {
                    yield return new WaitForSeconds(panelDisplayTime);
                }
            }
        }
        else
        {
            // 所有图片已显示完毕
            allPanelsShown = true;
            OnAllPanelsShown();
        }

        isTransitioning = false;
    }

    /// <summary>
    /// 淡入效果
    /// </summary>
    private IEnumerator FadeIn(Image image)
    {
        if (image == null) yield break;

        Color color = image.color;
        color.a = 0f;
        image.color = color;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            image.color = color;
            yield return null;
        }

        color.a = 1f;
        image.color = color;
    }

    /// <summary>
    /// 淡出效果
    /// </summary>
    private IEnumerator FadeOut(Image image)
    {
        if (image == null) yield break;

        Color color = image.color;
        float startAlpha = color.a;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / fadeInDuration);
            image.color = color;
            yield return null;
        }

        color.a = 0f;
        image.color = color;
    }

    /// <summary>
    /// 自动播放下一张
    /// </summary>
    private IEnumerator AutoPlayNextPanel()
    {
        autoPlay = false; // 防止重复调用
        yield return new WaitForSeconds(autoPlayDelay);
        
        if (!allPanelsShown)
        {
            ShowNextPanel();
        }
        
        autoPlay = true; // 重新启用自动播放
    }

    /// <summary>
    /// 所有图片显示完毕的回调
    /// </summary>
    private void OnAllPanelsShown()
    {
        Debug.Log("ComicPanelController: 所有图片已显示完毕，开始播放结束效果");
        StartCoroutine(PlayEndSequence());
    }

    /// <summary>
    /// 播放结束序列（黑屏 -> 文字 -> 按钮）
    /// </summary>
    private IEnumerator PlayEndSequence()
    {
        // 1. 淡入黑屏
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            yield return StartCoroutine(FadeInImage(blackScreen, blackScreenFadeDuration));
        }

        // 2. 等待一段时间后显示文字
        yield return new WaitForSeconds(textDelay);

        // 3. 显示并淡入文字
        if (storyText != null)
        {
            storyText.text = storyMessage;
            storyText.gameObject.SetActive(true);
            yield return StartCoroutine(FadeInText(storyText, textFadeInDuration));
        }

        // 4. 等待一段时间后显示按钮
        yield return new WaitForSeconds(buttonDelay);

        // 5. 显示继续按钮
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 淡入Image（用于黑屏）
    /// </summary>
    private IEnumerator FadeInImage(Image image, float duration)
    {
        if (image == null) yield break;

        Color color = image.color;
        color.a = 0f;
        image.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            image.color = color;
            yield return null;
        }

        color.a = 1f;
        image.color = color;
    }

    /// <summary>
    /// 淡出Image（用于开始时黑屏淡出）
    /// </summary>
    private IEnumerator FadeOutImage(Image image, float duration)
    {
        if (image == null) yield break;

        Color color = image.color;
        float startAlpha = color.a;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            image.color = color;
            yield return null;
        }

        color.a = 0f;
        image.color = color;
    }

    /// <summary>
    /// 淡入Text（用于故事文字）
    /// </summary>
    private IEnumerator FadeInText(Text text, float duration)
    {
        if (text == null) yield break;

        Color color = text.color;
        color.a = 0f;
        text.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            text.color = color;
            yield return null;
        }

        color.a = 1f;
        text.color = color;
    }

    /// <summary>
    /// 继续按钮点击事件
    /// </summary>
    private void OnContinueButtonClick()
    {
        Debug.Log("ComicPanelController: 点击继续按钮，加载下一个场景");
        LoadNextScene();
    }

    /// <summary>
    /// 加载下一个场景
    /// </summary>
    private void LoadNextScene()
    {
        // 恢复游戏时间（如果之前暂停了）
        Time.timeScale = 1f;

        // 根据设置加载场景
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // 使用场景名称加载
            SceneManager.LoadScene(nextSceneName);
        }
        else if (nextSceneIndex >= 0)
        {
            // 使用场景索引加载
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            // 默认加载下一个场景索引
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentSceneIndex + 1;
            int sceneCount = SceneManager.sceneCountInBuildSettings;

            if (nextIndex < sceneCount)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("ComicPanelController: 没有下一个场景！");
            }
        }
    }

    /// <summary>
    /// 重置漫画展示（重新开始）
    /// </summary>
    public void ResetComic()
    {
        currentPanelIndex = 0;
        allPanelsShown = false;
        isTransitioning = false;
        InitializePanels();
    }

    /// <summary>
    /// 跳过所有图片，直接显示最后一张
    /// </summary>
    public void SkipToEnd()
    {
        if (comicPanels == null || comicPanels.Length == 0) return;

        // 隐藏所有图片
        foreach (var panel in comicPanels)
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }

        // 显示最后一张
        if (comicPanels[comicPanels.Length - 1] != null)
        {
            comicPanels[comicPanels.Length - 1].gameObject.SetActive(true);
            comicPanels[comicPanels.Length - 1].color = new Color(1f, 1f, 1f, 1f);
        }

        currentPanelIndex = comicPanels.Length - 1;
        allPanelsShown = true;
        Debug.Log("ComicPanelController: 已跳过到最后一张图片");
    }
}

