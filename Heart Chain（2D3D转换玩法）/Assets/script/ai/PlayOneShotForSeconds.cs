using System.Collections;
using UnityEngine;

/// <summary>
/// 场景开始时依次播放两个音效：
/// 先播放 firstClip 持续 firstSeconds 秒，再播放 secondClip 持续 secondSeconds 秒。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayOneShotForSeconds : MonoBehaviour
{
    [Header("第一段")]
    public AudioClip firstClip;
    public float firstSeconds = 4f;

    [Header("第二段")]
    public AudioClip secondClip;
    public float secondSeconds = 2f;
    [Range(0f, 1f)]
    public float volume = 1f;
    public bool playOnStart = true;

    [Header("播放完自动退出")]
    public bool quitAfterPlay = false;
    public float quitDelaySeconds = 0f;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
    }

    void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        StopAllCoroutines();
        source.Stop();
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        if (firstClip == null)
        {
            Debug.LogWarning("PlayOneShotForSeconds: First Clip 为空，将跳过第一段。", this);
        }
        if (secondClip == null)
        {
            Debug.LogWarning("PlayOneShotForSeconds: Second Clip 为空，将跳过第二段。", this);
        }

        if (firstClip != null && firstSeconds > 0f)
        {
            source.clip = firstClip;
            source.volume = volume;
            source.loop = true;
            source.Play();
            yield return new WaitForSeconds(firstSeconds);
        }

        source.Stop();

        if (secondClip != null && secondSeconds > 0f)
        {
            source.clip = secondClip;
            source.volume = volume;
            source.loop = true;
            source.Play();
            yield return new WaitForSeconds(secondSeconds);
        }

        source.Stop();

        if (quitAfterPlay)
        {
            if (quitDelaySeconds > 0f)
                yield return new WaitForSeconds(quitDelaySeconds);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

