using UnityEngine;

/// <summary>
/// 给主角跳跃添加音效。
/// 用法：挂在玩家身上，设置 jumpClip；玩家在地面上按下 Jump（默认空格）时播放一次音效。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerJumpSound : MonoBehaviour
{
    [Header("音效")]
    public AudioClip jumpClip;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("落地检测")]
    public LayerMask groundMask = ~0;     // 默认检测所有层；建议在 Inspector 里只勾地面/平台层
    public float groundCheckDistance = 1.1f;
    public float groundCheckUpOffset = 0.1f;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    void Update()
    {
        if (jumpClip == null) return;

        // 与 DimensionPlayerController 的跳跃按键一致：Input.GetButtonDown("Jump")
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            source.PlayOneShot(jumpClip, volume);
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * groundCheckUpOffset;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }
}

