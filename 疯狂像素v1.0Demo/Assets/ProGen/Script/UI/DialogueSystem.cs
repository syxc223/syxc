using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 对话系统
/// 在游戏开始时显示对话序列
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject dialoguePanel; // 对话面板
    [SerializeField] private Text dialogueText; // 对话文本
    [SerializeField] private Text speakerNameText; // 说话者名称文本（可选）
    [SerializeField] private Image speakerPortrait; // 说话者头像（可选）
    [SerializeField] private Button continueButton; // 继续按钮（可选）
    [SerializeField] private Text continueButtonText; // 继续按钮文本
    
    [Header("对话设置")]
    [SerializeField] private DialogueData[] dialogues; // 对话数据数组
    [SerializeField] private float typingSpeed = 0.05f; // 打字机效果速度（每个字符的延迟，秒）
    [SerializeField] private bool useTypingEffect = true; // 是否使用打字机效果
    [SerializeField] private bool autoShowNext = false; // 是否自动显示下一句（不使用按钮）
    [SerializeField] private float autoShowDelay = 2f; // 自动显示延迟（秒）
    
    [Header("输入设置")]
    [SerializeField] private KeyCode continueKey = KeyCode.Space; // 继续对话的按键
    [SerializeField] private bool useMouseClick = true; // 是否使用鼠标点击
    
    [Header("完成回调")]
    [SerializeField] private bool pauseGameOnDialogue = true; // 对话时是否暂停游戏
    
    private int currentDialogueIndex = 0; // 当前对话索引
    private bool isTyping = false; // 是否正在打字
    private bool dialogueActive = false; // 对话是否激活
    private Coroutine typingCoroutine; // 打字协程引用
    
    private static bool hasPlayedDialogue = false; // 静态标志：对话是否已经播放过
    private static bool shouldPlayOnLastScene = false; // 静态标志：是否应该在最后一个场景播放对话
    
    public System.Action OnDialogueComplete; // 对话完成事件

    private void Start()
    {
        InitializeDialogue();
    }

    private void Update()
    {
        if (!dialogueActive) return;

        // 检查输入
        bool inputDetected = false;
        
        // 检查键盘输入
        if (Keyboard.current != null)
        {
            if (continueKey == KeyCode.Space && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                inputDetected = true;
            }
            else if (Keyboard.current[GetKeyCodeFromKeyCode(continueKey)].wasPressedThisFrame)
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
                // 如果正在打字，立即完成当前文本
                SkipTyping();
            }
            else
            {
                // 显示下一句对话
                ShowNextDialogue();
            }
        }
    }

    /// <summary>
    /// 初始化对话系统
    /// </summary>
    private void InitializeDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClick);
            continueButton.gameObject.SetActive(!autoShowNext);
        }

        if (continueButtonText != null && continueButtonText.text == "")
        {
            continueButtonText.text = "继续";
        }
    }

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue()
    {
        // 如果对话已经播放过且不是最后一个场景，直接返回
        if (hasPlayedDialogue && !shouldPlayOnLastScene)
        {
            Debug.Log("DialogueSystem: 对话已经播放过，跳过本次对话");
            return;
        }
        
        // 如果是最后一个场景的对话，重置标志
        if (shouldPlayOnLastScene)
        {
            shouldPlayOnLastScene = false;
            hasPlayedDialogue = false; // 允许播放最后一个场景的对话
            Debug.Log("DialogueSystem: 检测到最后一个场景，准备播放对话");
        }

        if (dialogues == null || dialogues.Length == 0)
        {
            Debug.LogWarning("DialogueSystem: 没有设置对话数据！");
            return;
        }

        currentDialogueIndex = 0;
        dialogueActive = true;
        hasPlayedDialogue = true; // 标记对话已播放

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (pauseGameOnDialogue)
        {
            Time.timeScale = 0f;
        }

        ShowDialogue(0);
    }

    /// <summary>
    /// 显示指定索引的对话
    /// </summary>
    private void ShowDialogue(int index)
    {
        if (index < 0 || index >= dialogues.Length)
        {
            EndDialogue();
            return;
        }

        DialogueData dialogue = dialogues[index];
        
        // 设置说话者名称
        if (speakerNameText != null)
        {
            if (!string.IsNullOrEmpty(dialogue.speakerName))
            {
                speakerNameText.text = dialogue.speakerName;
                speakerNameText.gameObject.SetActive(true);
            }
            else
            {
                speakerNameText.gameObject.SetActive(false);
            }
        }

        // 设置说话者头像
        if (speakerPortrait != null)
        {
            if (dialogue.speakerPortrait != null)
            {
                speakerPortrait.sprite = dialogue.speakerPortrait;
                speakerPortrait.gameObject.SetActive(true);
            }
            else
            {
                speakerPortrait.gameObject.SetActive(false);
            }
        }

        // 显示对话文本
        if (dialogueText != null)
        {
            if (useTypingEffect)
            {
                StartTyping(dialogue.dialogueText);
            }
            else
            {
                dialogueText.text = dialogue.dialogueText;
            }
        }

        // 如果是最后一句，更新按钮文本
        if (continueButtonText != null)
        {
            if (index == dialogues.Length - 1)
            {
                continueButtonText.text = "结束";
            }
            else
            {
                continueButtonText.text = "继续";
            }
        }

        // 自动显示下一句（如果启用）
        if (autoShowNext && index < dialogues.Length - 1)
        {
            StartCoroutine(AutoShowNextDialogue());
        }
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    private void StartTyping(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        isTyping = true;
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    /// <summary>
    /// 打字协程
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = "";
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed); // 使用Realtime不受Time.timeScale影响
        }

        isTyping = false;
    }

    /// <summary>
    /// 跳过打字效果
    /// </summary>
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        
        if (dialogueText != null && currentDialogueIndex < dialogues.Length)
        {
            dialogueText.text = dialogues[currentDialogueIndex].dialogueText;
        }
    }

    /// <summary>
    /// 显示下一句对话
    /// </summary>
    private void ShowNextDialogue()
    {
        currentDialogueIndex++;
        ShowDialogue(currentDialogueIndex);
    }

    /// <summary>
    /// 自动显示下一句对话
    /// </summary>
    private IEnumerator AutoShowNextDialogue()
    {
        yield return new WaitForSecondsRealtime(autoShowDelay);
        
        if (dialogueActive && !isTyping)
        {
            ShowNextDialogue();
        }
    }

    /// <summary>
    /// 继续按钮点击事件
    /// </summary>
    private void OnContinueButtonClick()
    {
        if (isTyping)
        {
            SkipTyping();
        }
        else
        {
            ShowNextDialogue();
        }
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    private void EndDialogue()
    {
        dialogueActive = false;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (pauseGameOnDialogue)
        {
            Time.timeScale = 1f;
        }

        OnDialogueComplete?.Invoke();
        Debug.Log("DialogueSystem: 对话结束");
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

    /// <summary>
    /// 重置对话播放标志（用于新游戏开始时）
    /// </summary>
    public static void ResetDialogueFlag()
    {
        hasPlayedDialogue = false;
        shouldPlayOnLastScene = false;
        Debug.Log("DialogueSystem: 对话播放标志已重置");
    }

    /// <summary>
    /// 设置最后一个场景对话标志（在进入最后一个场景时调用）
    /// </summary>
    public static void SetLastSceneDialogueFlag()
    {
        shouldPlayOnLastScene = true;
        Debug.Log("DialogueSystem: 已设置最后一个场景对话标志");
    }
}

/// <summary>
/// 对话数据（可在Inspector中配置）
/// </summary>
[System.Serializable]
public class DialogueData
{
    [Header("对话内容")]
    [TextArea(3, 5)]
    public string dialogueText; // 对话文本
    
    [Header("说话者信息（可选）")]
    public string speakerName = ""; // 说话者名称
    public Sprite speakerPortrait; // 说话者头像
}

