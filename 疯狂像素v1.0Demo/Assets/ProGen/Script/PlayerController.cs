using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    //组件引用
    private Rigidbody2D player;
    private SpriteRenderer sprite;
    private Animator animator;
    private PlayerInput playerInput;
    private Vector2 moveInput;//来自输入系统的当前移动输入
    private InputAction moveAction;//引用移动输入动作
    [SerializeField] private bool IsDead = false;//玩家是否死亡
    private bool hasShownGameOverUI = false; // 是否已显示游戏结束UI

    private void Awake()
    {
        //获取组件引用
        player = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();
            animator = GetComponent<Animator>();

        player.gravityScale = 0f;//禁用重力
        player.constraints = RigidbodyConstraints2D.FreezeRotation;//冻结旋转

        moveAction= playerInput.actions["Move"];//获取移动输入动作
        


    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 检查是否是最后一个场景
        CheckIfLastScene();
        
        DialogueSystem dialogueSystem = FindFirstObjectByType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue();
        }
    }

    /// <summary>
    /// 检查是否是最后一个场景，如果是则设置对话标志
    /// </summary>
    private void CheckIfLastScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        
        // 如果当前场景是最后一个场景（索引从0开始，所以最后一个场景的索引是 sceneCount - 1）
        if (currentSceneIndex == sceneCount - 1)
        {
            Debug.Log($"PlayerController: 检测到最后一个场景（索引: {currentSceneIndex}），设置对话标志");
            DialogueSystem.SetLastSceneDialogueFlag();
        }
    }

    // Update is called once per frame
    void Update()
    {
        SetAnimationParameters();
        if(PlayerHealth.Health <=0)
        {
            IsDead = true;
            // 显示游戏结束UI（只显示一次）
            if (!hasShownGameOverUI)
            {
                // 播放玩家死亡音效
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPlayerDeathSound();
                }
                ShowGameOverUI();
                hasShownGameOverUI = true;
            }
        }
        else
        {
            // 生命值恢复时重置标记
            if (hasShownGameOverUI && PlayerHealth.Health > 0)
            {
                hasShownGameOverUI = false;
            }
        }
    }

    /// <summary>
    /// 显示游戏结束界面
    /// </summary>
    private void ShowGameOverUI()
    {
        GameOverUI gameOverUI = GameOverUI.Instance;
        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOverPanel();
        }
        else
        {
            Debug.LogWarning("PlayerController: 未找到 GameOverUI 实例！请确保场景中存在 GameOverUI 组件。");
        }
    }
    void FixedUpdate()
    {
        if (PlayerHealth.Health > 0)
        {
            Move();
        }
        else
        {
            player.linearVelocity = new Vector2(0, 0);
        }
        
    }

    void Move()
    {
        moveInput= moveAction.ReadValue<Vector2>();//读取当前移动输入值

        //根据输入和移动速度计算速度
        Vector2 velocity = moveInput.normalized * moveSpeed;
        player.linearVelocity = velocity;

    }
    private void SetAnimationParameters()
    {
        if (!IsDead)
        {
            
        
        if(player.linearVelocity != Vector2.zero)
        {
            animator.SetBool("IsMoving", true);
        }
        if(player.linearVelocity == Vector2.zero)
        {
            animator.SetBool("IsMoving", false);
        }
   
        }
        if(IsDead)
        {
            animator.SetBool("IsDead", true);
        }
    }
    private void ResetHealth()
    {
        PlayerHealth.Health = 5.0f;
        IsDead = false;
        RestartLevel();
    }
    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
