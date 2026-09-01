using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardKeyUIFlasher : MonoBehaviour
{
    public Image keyW;
    public Image keyA;
    public Image keyS;
    public Image keyD;
    public Image keySpace;
    public Image keyMouseLeft;

    public Color flashColor = Color.blue;
    public float flashDuration = 0.2f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && keyW != null)
            StartCoroutine(FlashImage(keyW));
        if (Input.GetKeyDown(KeyCode.A) && keyA != null)
            StartCoroutine(FlashImage(keyA));
        if (Input.GetKeyDown(KeyCode.S) && keyS != null)
            StartCoroutine(FlashImage(keyS));
        if (Input.GetKeyDown(KeyCode.D) && keyD != null)
            StartCoroutine(FlashImage(keyD));
        if (Input.GetKeyDown(KeyCode.Space) && keySpace != null)
            StartCoroutine(FlashImage(keySpace));

        // 鼠标左键
        if (Input.GetMouseButtonDown(0) && keyMouseLeft != null)
            StartCoroutine(FlashImage(keyMouseLeft));
    }

    IEnumerator FlashImage(Image img)
    {
        Color original = img.color;
        float t = 0f;

        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.PingPong(t * 4f, 1f);
            img.color = Color.Lerp(original, flashColor, k);
            yield return null;
        }

        img.color = original;
    }
}