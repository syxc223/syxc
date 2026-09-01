using System.Collections;
using UnityEngine;

/// <summary>
/// 当玩家按下 WASD 时，让对应的方块进行蓝色闪烁。
/// 将此脚本挂在任意物体上，在 Inspector 里把四个 Cube 拖到对应槽位即可。
/// </summary>
public class KeyboardCubeFlasher : MonoBehaviour
{
    [Header("对应按键的方块")]
    public Renderer cubeW;
    public Renderer cubeA;
    public Renderer cubeS;
    public Renderer cubeD;

    [Header("闪烁设置")]
    public Color flashColor = Color.blue;
    public float flashDuration = 0.2f;   // 一次闪烁时长

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && cubeW != null)
            StartCoroutine(FlashRenderer(cubeW));

        if (Input.GetKeyDown(KeyCode.A) && cubeA != null)
            StartCoroutine(FlashRenderer(cubeA));

        if (Input.GetKeyDown(KeyCode.S) && cubeS != null)
            StartCoroutine(FlashRenderer(cubeS));

        if (Input.GetKeyDown(KeyCode.D) && cubeD != null)
            StartCoroutine(FlashRenderer(cubeD));
    }

    IEnumerator FlashRenderer(Renderer rend)
    {
        if (rend == null) yield break;

        // 使用实例材质，避免影响到共享材质
        Material mat = rend.material;
        Color original = mat.color;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.PingPong(t * 4f, 1f); // 稍微有点明暗变化
            mat.color = Color.Lerp(original, flashColor, k);
            yield return null;
        }

        mat.color = original;
    }
}

