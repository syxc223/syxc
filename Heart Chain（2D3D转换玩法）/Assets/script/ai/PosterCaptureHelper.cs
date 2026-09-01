using System.IO;
using UnityEngine;

/// <summary>
/// 3:4 竖版海报截图工具。
/// 挂在 Main Camera 上，运行场景后按 F12 保存一张 PNG 海报。
/// </summary>
[RequireComponent(typeof(Camera))]
public class PosterCaptureHelper : MonoBehaviour
{
    [Header("海报尺寸（宽:高 = 3:4）")]
    public int width = 1080;
    public int height = 1440;

    [Header("保存文件名（不含扩展名）")]
    public string fileName = "HeartChain_Poster_3x4";

    [Tooltip("运行时按此键截图")]
    public KeyCode captureKey = KeyCode.F12;

    Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void Update()
    {
        if (Input.GetKeyDown(captureKey))
            CapturePoster();
    }

    /// <summary> 也可在 Inspector 右键组件 → Capture Poster Now </summary>
    [ContextMenu("Capture Poster Now")]
    public void CapturePoster()
    {
        if (_camera == null) _camera = GetComponent<Camera>();

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var prevTarget = _camera.targetTexture;
        var prevActive = RenderTexture.active;

        _camera.targetTexture = rt;
        _camera.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        _camera.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        rt.Release();
        Destroy(rt);

        byte[] png = tex.EncodeToPNG();
        Destroy(tex);

        string folder = Path.Combine(Application.dataPath, "../Posters");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, fileName + ".png");
        File.WriteAllBytes(path, png);

        Debug.Log($"海报已保存：{path}  ({width}×{height}, 3:4)");
    }
}
