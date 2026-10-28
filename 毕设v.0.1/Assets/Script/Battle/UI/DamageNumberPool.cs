using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

/// <summary>
/// 伤害飘字对象池（教程 011/073/074）：复用 Text，避免每次命中 new/Destroy。
/// </summary>
public class DamageNumberPool : MonoBehaviour
{
    private Transform _layer;
    private Font _font;
    private ObjectPool<GameObject> _pool;

    public void Init(Transform layer, Font font)
    {
        _layer = layer;
        _font = font;
        _pool = new ObjectPool<GameObject>(
            CreateItem, OnGet, OnRelease, OnDestroyItem, true, 12, 48);
    }

    private GameObject CreateItem()
    {
        var go = new GameObject("DamageNumber", typeof(RectTransform), typeof(Text));
        var text = go.GetComponent<Text>();
        text.font = _font;
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_layer, false);
        rt.sizeDelta = new Vector2(160, 48);
        go.SetActive(false);
        return go;
    }

    private void OnGet(GameObject go) => go.SetActive(true);
    private void OnRelease(GameObject go) => go.SetActive(false);
    private void OnDestroyItem(GameObject go) => Destroy(go);

    /// <summary>在指定锚点位置弹出一个上浮淡出的数字。</summary>
    public void Spawn(Vector2 anchoredPosition, string content, Color color)
    {
        if (_pool == null)
            return;
        GameObject go = _pool.Get();
        var text = go.GetComponent<Text>();
        text.text = content;
        text.color = color;
        go.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
        StartCoroutine(FloatAndRelease(go));
    }

    private IEnumerator FloatAndRelease(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        var text = go.GetComponent<Text>();
        Vector2 start = rt.anchoredPosition;
        float duration = 0.8f;
        float t = 0f;
        while (t < duration && go != null)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = start + new Vector2(0f, 60f * (t / duration));
            var c = text.color;
            c.a = 1f - t / duration;
            text.color = c;
            yield return null;
        }
        if (go != null)
        {
            var c = text.color;
            c.a = 1f;
            text.color = c;
            _pool.Release(go);
        }
    }
}
