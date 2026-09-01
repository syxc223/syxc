using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 文字序列控制器
/// 实现屏幕变灰、逐行显示文字（带打字音效）、最后显示按钮并返回主菜单
/// </summary>
public class TextSequenceController : MonoBehaviour
{
    [Header("遮罩设置")]
    [SerializeField] private Image grayOverlay; // 灰色遮罩（覆盖整个屏幕）
    [SerializeField] private float grayFadeInDuration = 2f; // 灰色遮罩淡入时长（秒）
    
    [Header("文字设置")]
    [SerializeField] private Text[] textLines; // 文字行数组（按顺序显示）
    [SerializeField] private float typingSpeed = 0.05f; // 打字速度（每个字符的延迟，秒）
    [SerializeField] private float lineDelay = 1f; // 每行文字之间的延迟（秒）
    [SerializeField] private float lineFadeInDuration = 0.5f; // 每行文字淡入时长（秒）
    
    [Header("音效设置")]
    [SerializeField] private AudioClip typingSound; // 打字音效
    [SerializeField] private float typingSoundInterval = 0.1f; // 打字音效播放间隔（秒）
    [SerializeField] private bool useTypingSound = true; // 是否使用打字音效
    
    [Header("按钮设置")]
    [SerializeField] private Button continueButton; // 继续按钮（最后一行文字显示后出现）
    [SerializeField] private Text continueButtonText; // 按钮文本
    [SerializeField] private float buttonFadeInDuration = 0.5f; // 按钮淡入时长（秒）
    
    [Header("场景切换设置")]
    [SerializeField] private string mainMenuSceneName = ""; // 主菜单场景名称（如果为空则使用场景索引0）
    [SerializeField] private int mainMenuSceneIndex = 0; // 主菜单场景索引（默认0）
    
    [Header("输入设置")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space; // 跳过当前文字/显示下一行的按键
    [SerializeField] private bool useMouseClick = true; // 是否使用鼠标点击
    
    private int currentLineIndex = 0; // 当前文字行索引
    private bool isTyping = false; // 是否正在打字
    private bool sequenceActive = false; // 序列是否激活
    private Coroutine typingCoroutine; // 打字协程引用
    private AudioSource audioSource; // 音效音频源（如果没有AudioManager则使用本地音频源）
    
    private void Start()
    {
        InitializeSequence();
        StartCoroutine(PlaySequence());
    }
    
    private void Update()
    {
        if (!sequenceActive) return;
        
        // 检查输入（用于跳过打字或显示下一行）
        bool inputDetected = false;
        
        // 检查键盘输入
        if (Keyboard.current != null)
        {
            if (skipKey == KeyCode.Space && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                inputDetected = true;
            }
            else if (Keyboard.current[GetKeyCodeFromKeyCode(skipKey)].wasPressedThisFrame)
            {
                inputDetected = true;
            }
        }
        
        // 检查鼠标点击
        if (useMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
        }
        
        if (inputDetected)
        {
            if (isTyping)
            {
                // 如果正在打字，立即完成当前文字
                SkipTyping();
            }
            else if (currentLineIndex < textLines.Length - 1)
            {
                // 如果还有下一行，显示下一行
                ShowNextLine();
            }
        }
    }
    
    /// <summary>
    /// 初始化序列
    /// </summary>
    private void InitializeSequence()
    {
        // 初始化灰色遮罩（透明）
        if (grayOverlay != null)
        {
            grayOverlay.gameObject.SetActive(true);
            Color color = grayOverlay.color;
            color.a = 0f; // 初始透明
            grayOverlay.color = color;
            
            // 关闭Raycast Target，防止遮挡文字和按钮的交互
            grayOverlay.raycastTarget = false;
            
            // 确保遮罩在Hierarchy中位于最前面（索引0），这样它会先渲染
            // Unity UI的渲染顺序：索引小的先渲染，索引大的后渲染（后渲染的在上层）
            // 所以遮罩应该在最前面（先渲染，作为背景），文字和按钮在后面（后渲染，显示在遮罩上方）
            grayOverlay.transform.SetAsFirstSibling();
            Debug.Log($"[TextSequenceController] 灰色遮罩初始化完成，层级索引: {grayOverlay.transform.GetSiblingIndex()}");
        }
        else
        {
            Debug.LogError("[TextSequenceController] 灰色遮罩未设置！");
        }
        
        // 隐藏所有文字行，并确保它们在遮罩后面（这样会渲染在遮罩上方）
        if (textLines != null)
        {
            foreach (var textLine in textLines)
            {
                if (textLine != null)
                {
                    textLine.gameObject.SetActive(false);
                    Color color = textLine.color;
                    color.a = 0f;
                    textLine.color = color;
                    
                    // 确保文字在遮罩后面（索引更大），这样文字会渲染在遮罩上方
                    if (grayOverlay != null)
                    {
                        int grayOverlayIndex = grayOverlay.transform.GetSiblingIndex();
                        int currentTextIndex = textLine.transform.GetSiblingIndex();
                        // 如果文字的索引小于等于遮罩的索引，将文字移到遮罩后面
                        if (currentTextIndex <= grayOverlayIndex)
                        {
                            textLine.transform.SetSiblingIndex(grayOverlayIndex + 1);
                        }
                    }
                }
            }
        }
        
        // 隐藏按钮，并确保它在遮罩后面（这样会渲染在遮罩上方）
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinueButtonClick);
            
            // 确保按钮在遮罩后面（索引更大），这样按钮会渲染在遮罩上方
            if (grayOverlay != null)
            {
                int grayOverlayIndex = grayOverlay.transform.GetSiblingIndex();
                int currentButtonIndex = continueButton.transform.GetSiblingIndex();
                // 如果按钮的索引小于等于遮罩的索引，将按钮移到遮罩后面
                if (currentButtonIndex <= grayOverlayIndex)
                {
                    continueButton.transform.SetSiblingIndex(grayOverlayIndex + 1);
                }
            }
        }
        
        // 设置按钮文本
        if (continueButtonText != null && continueButtonText.text == "")
        {
            continueButtonText.text = "返回主菜单";
        }
        
        // 尝试获取AudioManager的音效源，如果没有则创建本地音频源
        if (useTypingSound && typingSound != null)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                // 使用AudioManager播放音效（如果AudioManager支持播放自定义音效）
                // 注意：如果AudioManager没有公开播放自定义音效的方法，则创建本地AudioSource
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = 0.5f;
            }
            else
            {
                // 创建本地音频源
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = 0.5f;
            }
        }
    }
    
    /// <summary>
    /// 播放序列（灰色遮罩 -> 逐行显示文字 -> 显示按钮）
    /// </summary>
    private IEnumerator PlaySequence()
    {
        // 1. 淡入灰色遮罩
        if (grayOverlay != null)
        {
            yield return StartCoroutine(FadeInImage(grayOverlay, grayFadeInDuration));
            Debug.Log("TextSequenceController: 灰色遮罩淡入完成");
        }
        
        // 2. 等待一小段时间
        yield return new WaitForSeconds(0.5f);
        
        // 3. 激活序列（允许输入控制）
        sequenceActive = true;
        
        // 4. 逐行显示文字
        Debug.Log($"[TextSequenceController] 开始显示文字，共 {textLines.Length} 行");
        for (int i = 0; i < textLines.Length; i++)
        {
            currentLineIndex = i;
            yield return StartCoroutine(ShowTextLine(i));
            
            // 如果不是最后一行，等待一段时间后继续
            if (i < textLines.Length - 1)
            {
                yield return new WaitForSeconds(lineDelay);
            }
        }
        Debug.Log("[TextSequenceController] 所有文字显示完成");
        
        // 5. 所有文字显示完毕，显示按钮
        yield return new WaitForSeconds(1f);
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            yield return StartCoroutine(FadeInButton(continueButton, buttonFadeInDuration));
            Debug.Log("TextSequenceController: 按钮显示完成");
        }
    }
    
    /// <summary>
    /// 显示指定索引的文字行
    /// </summary>
    private IEnumerator ShowTextLine(int index)
    {
        if (index < 0 || index >= textLines.Length || textLines[index] == null)
        {
            Debug.LogWarning($"[TextSequenceController] ShowTextLine: 无效的索引 {index}");
            yield break;
        }
        
        Text textLine = textLines[index];
        Debug.Log($"[TextSequenceController] 开始显示第 {index + 1} 行文字: {textLine.name}");
        
        // 确保文字在遮罩后面（渲染在上层）
        if (grayOverlay != null)
        {
            int grayOverlayIndex = grayOverlay.transform.GetSiblingIndex();
            int currentTextIndex = textLine.transform.GetSiblingIndex();
            if (currentTextIndex <= grayOverlayIndex)
            {
                textLine.transform.SetSiblingIndex(grayOverlayIndex + 1);
                Debug.Log($"[TextSequenceController] 调整文字层级: {textLine.name} 从索引 {currentTextIndex} 移到 {grayOverlayIndex + 1}");
            }
        }
        
        // 激活文字对象
        textLine.gameObject.SetActive(true);
        Debug.Log($"[TextSequenceController] 激活文字对象: {textLine.name}");
        
        // 获取原始文字内容（在淡入之前保存）
        string fullText = textLine.text;
        
        // 淡入文字
        yield return StartCoroutine(FadeInText(textLine, lineFadeInDuration));
        Debug.Log($"[TextSequenceController] 文字淡入完成: {textLine.name}, Alpha: {textLine.color.a}");
        
        // 开始打字效果
        if (typingSpeed > 0 && !string.IsNullOrEmpty(fullText))
        {
            yield return StartCoroutine(TypeText(textLine, fullText));
        }
        else
        {
            // 如果没有打字效果，直接显示完整文字
            textLine.text = fullText;
        }
        
        Debug.Log($"[TextSequenceController] 第 {index + 1} 行文字显示完成");
    }
    
    /// <summary>
    /// 打字效果（逐字符显示）
    /// </summary>
    private IEnumerator TypeText(Text textComponent, string fullText)
    {
        if (textComponent == null) yield break;
        
        isTyping = true;
        textComponent.text = "";
        
        float lastSoundTime = -typingSoundInterval; // 确保第一声立即播放
        
        foreach (char c in fullText)
        {
            textComponent.text += c;
            
            // 播放打字音效
            if (useTypingSound && typingSound != null && audioSource != null)
            {
                float currentTime = Time.unscaledTime;
                if (currentTime - lastSoundTime >= typingSoundInterval)
                {
                    audioSource.PlayOneShot(typingSound);
                    lastSoundTime = currentTime;
                }
            }
            
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        
        isTyping = false;
    }
    
    /// <summary>
    /// 跳过打字效果（立即显示完整文字）
    /// </summary>
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (currentLineIndex >= 0 && currentLineIndex < textLines.Length && textLines[currentLineIndex] != null)
        {
            // 文字已经在ShowTextLine中设置了，这里不需要重新设置
            isTyping = false;
        }
    }
    
    /// <summary>
    /// 显示下一行文字
    /// </summary>
    private void ShowNextLine()
    {
        if (currentLineIndex < textLines.Length - 1)
        {
            StartCoroutine(ShowTextLine(currentLineIndex + 1));
        }
    }
    
    /// <summary>
    /// 淡入Image（用于灰色遮罩）
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
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            image.color = color;
            yield return null;
        }
        
        color.a = 1f;
        image.color = color;
    }
    
    /// <summary>
    /// 淡入Text（用于文字行）
    /// </summary>
    private IEnumerator FadeInText(Text text, float duration)
    {
        if (text == null) yield break;
        
        Color color = text.color;
        float targetAlpha = color.a; // 保存目标透明度
        
        // 如果目标透明度为0，设置为1（确保文字可见）
        if (targetAlpha <= 0f)
        {
            targetAlpha = 1f;
            Debug.Log($"[TextSequenceController] 文字 {text.name} 的原始Alpha为0，设置为1以确保可见");
        }
        
        color.a = 0f;
        text.color = color;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(0f, targetAlpha, elapsed / duration);
            text.color = color;
            yield return null;
        }
        
        color.a = targetAlpha;
        text.color = color;
        
        Debug.Log($"[TextSequenceController] 文字 {text.name} 淡入完成，最终Alpha: {color.a}");
    }
    
    /// <summary>
    /// 淡入Button（用于继续按钮）
    /// </summary>
    private IEnumerator FadeInButton(Button button, float duration)
    {
        if (button == null) yield break;
        
        // 获取按钮的Image组件（用于淡入效果）
        Image buttonImage = button.GetComponent<Image>();
        Text buttonText = button.GetComponentInChildren<Text>();
        
        // 淡入按钮背景
        if (buttonImage != null)
        {
            Color color = buttonImage.color;
            float targetAlpha = color.a;
            color.a = 0f;
            buttonImage.color = color;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(0f, targetAlpha, elapsed / duration);
                buttonImage.color = color;
                yield return null;
            }
            
            color.a = targetAlpha;
            buttonImage.color = color;
        }
        
        // 淡入按钮文字
        if (buttonText != null)
        {
            Color color = buttonText.color;
            float targetAlpha = color.a;
            color.a = 0f;
            buttonText.color = color;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(0f, targetAlpha, elapsed / duration);
                buttonText.color = color;
                yield return null;
            }
            
            color.a = targetAlpha;
            buttonText.color = color;
        }
    }
    
    /// <summary>
    /// 继续按钮点击事件
    /// </summary>
    private void OnContinueButtonClick()
    {
        Debug.Log("TextSequenceController: 点击返回主菜单按钮");
        LoadMainMenu();
    }
    
    /// <summary>
    /// 加载主菜单场景
    /// </summary>
    private void LoadMainMenu()
    {
        // 恢复游戏时间（如果之前暂停了）
        Time.timeScale = 1f;
        
        // 根据设置加载场景
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            // 使用场景名称加载
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // 使用场景索引加载（默认0）
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }
    
    /// <summary>
    /// 将旧的 KeyCode 转换为新的 Key 枚举
    /// </summary>
    private Key GetKeyCodeFromKeyCode(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Space: return Key.Space;
            case KeyCode.Return: return Key.Enter;
            case KeyCode.Escape: return Key.Escape;
            case KeyCode.LeftArrow: return Key.LeftArrow;
            case KeyCode.RightArrow: return Key.RightArrow;
            case KeyCode.UpArrow: return Key.UpArrow;
            case KeyCode.DownArrow: return Key.DownArrow;
            default: return Key.Space;
        }
    }
}