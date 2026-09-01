using UnityEngine;

/// <summary>
/// 金币被玩家拾取时播放音效。
/// 用法：
/// 1) 挂在金币物体上（金币 Collider 勾 IsTrigger）。
/// 2) 设置 collectClip。
/// 3) 玩家需要有 Collider + Rigidbody（任一方有 Rigidbody 即可触发 Trigger）。
/// </summary>
public class CoinCollectSound : MonoBehaviour
{
    [Header("音效")]
    public AudioClip collectClip;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("拾取判定")]
    [Tooltip("只要碰到带这些组件的物体就算玩家；留空则不检查组件。")]
    public bool requirePlayerComponent = true;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (requirePlayerComponent)
        {
            // 兼容两套玩家：2D3D解谜的 DimensionPlayerController / 吃豆人 PacmanPlayerController
            bool isPlayer =
                other.GetComponentInParent<DimensionPlayerController>() != null ||
                other.GetComponentInParent<PacmanPlayerController>() != null;

            if (!isPlayer) return;
        }

        collected = true;

        if (collectClip != null)
        {
            // 在世界坐标播放一次音效，不依赖金币自身的 AudioSource
            AudioSource.PlayClipAtPoint(collectClip, transform.position, volume);
        }

        Destroy(gameObject);
    }
}

