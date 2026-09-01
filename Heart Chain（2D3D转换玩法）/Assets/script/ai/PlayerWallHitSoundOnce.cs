using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 撞墙音效（每面墙仅触发一次）。
/// 用法：挂在玩家身上，设置 wallHitClip，并设置 wallMask（墙所在的层）。
/// 当玩家第一次与某个墙 Collider 发生碰撞时播放一次音效；同一面墙后续再撞不会重复播放。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerWallHitSoundOnce : MonoBehaviour
{
    [Header("音效")]
    public AudioClip wallHitClip;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("墙层设置")]
    public LayerMask wallMask; // 只勾选墙所在 Layer（例如 Wall）

    [Header("过滤")]
    public float minImpactSpeed = 1.0f; // 低于这个碰撞速度不触发（避免轻微贴墙也响）

    private AudioSource source;
    private readonly HashSet<int> playedWallColliderIds = new HashSet<int>();

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (wallHitClip == null) return;

        int otherLayerMask = 1 << collision.collider.gameObject.layer;
        bool isWall = (wallMask.value & otherLayerMask) != 0;
        if (!isWall) return;

        // 用相对速度判断“撞”的程度
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;

        int wallId = collision.collider.GetInstanceID();
        if (!playedWallColliderIds.Add(wallId))
        {
            // 这面墙已经触发过
            return;
        }

        source.PlayOneShot(wallHitClip, volume);
    }
}

