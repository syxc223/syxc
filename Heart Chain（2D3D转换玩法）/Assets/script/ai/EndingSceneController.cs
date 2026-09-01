using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 最后一个场景的结尾控制：
/// 1. 开场全黑；
/// 2. 缓缓亮起并显示一段文字；
/// 3. 停留一段时间后自动退出游戏（编辑器中则停止播放）。
/// </summary>
public class EndingSceneController : MonoBehaviour
{
    Animation anim;

    void Start()
    {
        anim = GetComponent<Animation>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.Play();
        }
    }
}

