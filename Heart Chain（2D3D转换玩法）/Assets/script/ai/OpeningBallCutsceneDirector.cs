using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 开场动画叙事：
/// 1. 小白球 —— 孩子无忧无虑的蹦跳；
/// 2. 红球登场 —— 母亲约束出现，唠叨声起；
/// 3. 持续压迫 —— 红球不断逼近，挤压活动空间，小白球活力渐失；
/// 4. 第一视角仰视 —— 红球变得高大，孩子被迫提前面对"现实"；
/// 5. 黑屏 → 凄凉音乐 → 小白球缓慢、沉重地前行。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class OpeningBallCutsceneDirector : MonoBehaviour
{
    [Header("角色与场景")]
    public Transform smallBall;
    public Transform redBall;
    public Transform groundPlane;
    public Camera mainCamera;

    [Header("阶段1：无忧无虑")]
    public float bounceHeight = 0.8f;
    public float bounceHorizontalRange = 2.5f;
    public float bounceSpeed = 3.5f;
    public float bouncePhaseDuration = 4f;
    [Tooltip("蹦跳时小白球 Y 轴自转速度（度/秒），表现孩子活泼")]
    public float carefreeSpinSpeed = 90f;
    [Tooltip("落地挤压 / 起跳拉伸幅度")]
    public float squashStretchAmount = 0.15f;

    [Header("阶段2：红球登场")]
    public float redBallEnterDuration = 2f;
    public Vector3 redBallStartOffset = new Vector3(-6f, 0f, 0f);
    public float redBallScale = 1.5f;

    [Header("阶段3：持续压迫")]
    public float oppressionDuration = 3.5f;
    [Tooltip("压迫结束时红球距小白球的距离（越小越窒息）")]
    public float redBallOppressDistance = 1.2f;
    [Tooltip("压迫结束时小白球活动范围缩至原来的比例")]
    public float minPlayRangeRatio = 0.15f;
    [Tooltip("压迫结束时蹦跳高度缩至原来的比例")]
    public float minBounceHeightRatio = 0.1f;
    [Tooltip("红球每次「唠叨」脉冲的缩放幅度")]
    public float redPulseScale = 0.08f;

    [Header("阶段4：第一视角仰视")]
    public float lookUpDuration = 2f;
    public float redBallGrowDuration = 2.5f;
    public float redBallToweringScale = 4f;
    [Tooltip("仰视时红球微微前倾，像俯身压迫")]
    public float redLeanAngle = 12f;

    [Header("黑屏过场")]
    public CanvasGroup fadeCanvas;
    public float blackScreenDuration = 1.5f;

    [Header("天空盒切换")]
    public Material skyboxBefore;
    public Material skyboxAfter;
    public float skyboxFadeDuration = 1f;

    [Header("音效与音乐")]
    public AudioClip happyMusic;
    public AudioClip chatteringSfx;
    public AudioClip sadMusic;
    public float happyMusicFadeIn = 0.8f;
    public float sadMusicFadeIn = 1.2f;

    [Header("阶段5：失去活力后缓慢行走")]
    public float slowWalkDuration = 5f;
    public float slowWalkSpeed = 0.35f;
    public Vector3 slowWalkDirection = Vector3.forward;
    [Tooltip("行走结束后小白球相对初始大小的比例")]
    public float finalBallScaleRatio = 0.75f;

    [Header("结束后切换场景")]
    public string nextSceneName;

    AudioSource _audioSource;
    Vector3 _smallBallStartPos;
    Vector3 _smallBallStartScale;
    Vector3 _redBallFinalPos;
    Color _ballColorStart;
    Color _ballColorEnd = new Color(0.72f, 0.72f, 0.75f);
    Renderer _smallBallRenderer;
    Material _smallBallMaterial;
    float _bouncePhase;
    float _vitality = 1f;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        if (smallBall != null)
        {
            _smallBallStartPos = smallBall.position;
            _smallBallStartScale = smallBall.localScale;
            _smallBallRenderer = smallBall.GetComponent<Renderer>();
            if (_smallBallRenderer != null)
            {
                _smallBallMaterial = _smallBallRenderer.material;
                _ballColorStart = _smallBallMaterial.color;
            }
        }

        if (redBall != null)
        {
            redBall.gameObject.SetActive(false);
            _redBallFinalPos = _smallBallStartPos + Vector3.forward * 2f;
        }

        if (fadeCanvas != null) fadeCanvas.alpha = 0f;

        if (skyboxBefore != null)
        {
            RenderSettings.skybox = skyboxBefore;
            DynamicGI.UpdateEnvironment();
        }
    }

    void Start()
    {
        StartCoroutine(PlayFullOpening());
    }

    IEnumerator PlayFullOpening()
    {
        yield return StartCoroutine(PhaseCarefreeBouncing());
        yield return StartCoroutine(PhaseRedBallEnter());
        yield return StartCoroutine(PhaseOppression());

        if (skyboxAfter != null)
            yield return StartCoroutine(SwitchSkyboxWithFade());

        yield return StartCoroutine(PhaseFirstPersonTowering());
        yield return StartCoroutine(PhaseBlackoutAndSadWalk());
    }

    // ── 阶段1：孩子无忧无虑 ──────────────────────────────────────
    IEnumerator PhaseCarefreeBouncing()
    {
        if (smallBall == null || mainCamera == null) yield break;

        if (happyMusic != null)
            yield return StartCoroutine(FadeInMusic(happyMusic, happyMusicFadeIn, loop: true));

        float elapsed = 0f;
        _bouncePhase = 0f;
        _vitality = 1f;

        while (elapsed < bouncePhaseDuration)
        {
            elapsed += Time.deltaTime;
            _bouncePhase += Time.deltaTime * bounceSpeed;

            // 活动范围随时间略增，表现"越来越欢"
            float rangeBoost = 1f + 0.2f * Mathf.Sin(elapsed * 0.8f);
            float x = Mathf.Sin(_bouncePhase) * bounceHorizontalRange * rangeBoost;
            float y = BounceCurve(_bouncePhase) * bounceHeight * rangeBoost;

            ApplyBallMotion(_smallBallStartPos, x, y, _vitality);
            ApplyCarefreeSquashStretch(y / (bounceHeight * rangeBoost));
            smallBall.Rotate(Vector3.up, carefreeSpinSpeed * Time.deltaTime, Space.World);

            UpdateFollowCamera(0.35f);
            yield return null;
        }

        ResetBallScale();
        smallBall.position = _smallBallStartPos;
    }

    // ── 阶段2：红球登场，唠叨声起 ────────────────────────────────
    IEnumerator PhaseRedBallEnter()
    {
        if (redBall == null) yield break;

        // 欢快音乐渐弱
        if (_audioSource != null && _audioSource.isPlaying)
            yield return StartCoroutine(FadeOutAudio(0.6f));

        redBall.gameObject.SetActive(true);
        redBall.localScale = Vector3.one * redBallScale;
        Vector3 startPos = _redBallFinalPos + redBallStartOffset;
        redBall.position = startPos;

        if (chatteringSfx != null)
        {
            _audioSource.clip = chatteringSfx;
            _audioSource.loop = true;
            _audioSource.volume = 0f;
            _audioSource.Play();
            yield return StartCoroutine(FadeInVolume(1f, 0.4f));
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(redBallEnterDuration, 0.01f);
            float eased = EaseInOutCubic(t);
            redBall.position = Vector3.Lerp(startPos, _redBallFinalPos, eased);

            // 红球出现时，孩子还想跳，但已经有些犹豫
            float hesitation = 1f - eased * 0.4f;
            float x = Mathf.Sin(_bouncePhase) * bounceHorizontalRange * hesitation;
            _bouncePhase += Time.deltaTime * bounceSpeed * hesitation;
            float y = BounceCurve(_bouncePhase) * bounceHeight * hesitation;
            ApplyBallMotion(_smallBallStartPos, x, y, _vitality * hesitation);
            ApplyCarefreeSquashStretch(y / bounceHeight);

            UpdateFollowCamera(0.5f, lookAtRed: true, redLookWeight: eased * 0.25f);
            yield return null;
        }

        redBall.position = _redBallFinalPos;
        ResetBallScale();
    }

    // ── 阶段3：红球不断压迫，活力流失 ────────────────────────────
    IEnumerator PhaseOppression()
    {
        if (redBall == null || smallBall == null) yield break;

        float elapsed = 0f;
        Vector3 redStart = _redBallFinalPos;
        Vector3 redEnd = _smallBallStartPos + Vector3.forward * redBallOppressDistance;
        Vector3 redScaleBase = redBall.localScale;
        float pulseTimer = 0f;

        while (elapsed < oppressionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / oppressionDuration;
            float eased = EaseInOutCubic(t);

            // 红球步步紧逼
            redBall.position = Vector3.Lerp(redStart, redEnd, eased);

            // 唠叨脉冲：红球周期性胀大，像每句"叮嘱"
            pulseTimer += Time.deltaTime;
            float pulse = 1f + redPulseScale * Mathf.Sin(pulseTimer * 8f) * (1f - t * 0.5f);
            redBall.localScale = redScaleBase * pulse;

            // 活动空间与蹦跳高度被不断压缩
            float rangeFactor = Mathf.Lerp(1f, minPlayRangeRatio, eased);
            float heightFactor = Mathf.Lerp(1f, minBounceHeightRatio, eased);
            float speedFactor = Mathf.Lerp(1f, 0.3f, eased);

            _bouncePhase += Time.deltaTime * bounceSpeed * speedFactor;
            float x = Mathf.Sin(_bouncePhase) * bounceHorizontalRange * rangeFactor;
            float y = BounceCurve(_bouncePhase) * bounceHeight * heightFactor;

            // 被红球"推"得后退
            float pushBack = eased * 0.6f;
            Vector3 pushedCenter = _smallBallStartPos - Vector3.forward * pushBack;
            ApplyBallMotion(pushedCenter, x, y, _vitality * (1f - eased));

            // 活力流失：颜色变灰、体积缩小
            _vitality = 1f - eased;
            ApplyVitalityVisuals(_vitality);

            // 自转越来越慢，直到几乎停转
            float spin = carefreeSpinSpeed * (1f - eased);
            if (spin > 5f)
                smallBall.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);

            UpdateFollowCamera(0.55f, lookAtRed: true, redLookWeight: 0.3f + eased * 0.4f);
            yield return null;
        }

        // 最后几次微弱挣扎，然后彻底停下
        for (int i = 0; i < 3; i++)
        {
            float struggle = 1f - i / 3f;
            _bouncePhase += Time.deltaTime * bounceSpeed * 0.2f * struggle;
            float x = Mathf.Sin(_bouncePhase) * bounceHorizontalRange * minPlayRangeRatio * struggle;
            float y = BounceCurve(_bouncePhase) * bounceHeight * minBounceHeightRatio * struggle;
            ApplyBallMotion(_smallBallStartPos - Vector3.forward * 0.6f, x, y, struggle * 0.2f);
            ApplyVitalityVisuals(struggle * 0.2f);
            yield return new WaitForSeconds(0.25f);
        }

        smallBall.position = _smallBallStartPos - Vector3.forward * 0.6f;
        ResetBallScale();
        _vitality = 0f;
        ApplyVitalityVisuals(0f);
    }

    // ── 阶段4：第一视角，红球变得高大 ────────────────────────────
    IEnumerator PhaseFirstPersonTowering()
    {
        if (mainCamera == null || smallBall == null || redBall == null) yield break;

        mainCamera.transform.SetParent(smallBall);
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;

        Quaternion lookStraight = mainCamera.transform.rotation;
        Quaternion lookUp = Quaternion.Euler(-30f, 0f, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(lookUpDuration, 0.01f);
            float eased = EaseInOutCubic(t);
            mainCamera.transform.localRotation = Quaternion.Slerp(lookStraight, lookUp, eased);
            yield return null;
        }

        Vector3 redScaleStart = redBall.localScale;
        Vector3 redScaleEnd = Vector3.one * redBallToweringScale;
        Quaternion redRotStart = redBall.rotation;
        Quaternion redRotEnd = redRotStart * Quaternion.Euler(redLeanAngle, 0f, 0f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(redBallGrowDuration, 0.01f);
            float eased = EaseInOutCubic(t);
            redBall.localScale = Vector3.Lerp(redScaleStart, redScaleEnd, eased);
            redBall.rotation = Quaternion.Slerp(redRotStart, redRotEnd, eased);

            // 红球胀大时，孩子被挤得更小
            float childScale = Mathf.Lerp(1f, finalBallScaleRatio, eased);
            smallBall.localScale = _smallBallStartScale * childScale;

            yield return null;
        }

        mainCamera.transform.SetParent(null);
    }

    // ── 阶段5：黑屏 → 凄凉音乐 → 缓慢行走 ─────────────────────
    IEnumerator PhaseBlackoutAndSadWalk()
    {
        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                fadeCanvas.alpha = Mathf.Clamp01(t);
                yield return null;
            }
            fadeCanvas.alpha = 1f;
        }

        yield return new WaitForSeconds(blackScreenDuration);

        if (_audioSource != null) _audioSource.Stop();

        if (sadMusic != null)
            yield return StartCoroutine(FadeInMusic(sadMusic, sadMusicFadeIn, loop: true));

        if (redBall != null) redBall.gameObject.SetActive(false);

        // 恢复行走大小，但保持"失去活力"的灰色调
        if (smallBall != null)
            smallBall.localScale = _smallBallStartScale * finalBallScaleRatio;
        ApplyVitalityVisuals(0f);

        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.2f;
                fadeCanvas.alpha = 1f - Mathf.Clamp01(t);
                yield return null;
            }
            fadeCanvas.alpha = 0f;
        }

        // 镜头缓慢移到陪走机位
        Vector3 camStartPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        Vector3 camTargetOffset = new Vector3(0f, 1.8f, -3.5f);
        Vector3 camEndPos = (smallBall != null ? smallBall.position : Vector3.zero) + camTargetOffset;

        float camBlendT = 0f;
        while (camBlendT < 1f)
        {
            camBlendT += Time.deltaTime / 1.5f;
            float eased = EaseInOutCubic(camBlendT);
            if (mainCamera != null && smallBall != null)
            {
                mainCamera.transform.position = Vector3.Lerp(camStartPos, camEndPos, eased);
                mainCamera.transform.rotation = Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    Quaternion.LookRotation((smallBall.position + slowWalkDirection * 2f) - mainCamera.transform.position),
                    eased);
            }
            yield return null;
        }

        // 缓慢、沉重地前行 —— 不再蹦跳，只是滚动
        float walkElapsed = 0f;
        Vector3 walkPos = smallBall != null ? smallBall.position : Vector3.zero;
        float radius = smallBall != null ? smallBall.localScale.x * 0.5f : 0.5f;
        Vector3 moveDir = slowWalkDirection.normalized;
        Vector3 rollAxis = Vector3.Cross(Vector3.up, moveDir);
        if (rollAxis == Vector3.zero) rollAxis = Vector3.right;

        while (walkElapsed < slowWalkDuration)
        {
            float dt = Time.deltaTime;
            walkElapsed += dt;

            if (smallBall != null)
            {
                float distance = slowWalkSpeed * dt;
                walkPos += moveDir * distance;
                smallBall.position = walkPos;

                float angleDeg = (distance / radius) * Mathf.Rad2Deg;
                smallBall.Rotate(rollAxis, angleDeg, Space.World);

                // 轻微下压，像被生活压弯
                float sag = 0.03f * Mathf.Sin(walkElapsed * 1.5f);
                smallBall.localScale = new Vector3(
                    _smallBallStartScale.x * finalBallScaleRatio * (1f + sag),
                    _smallBallStartScale.y * finalBallScaleRatio * (1f - sag * 2f),
                    _smallBallStartScale.z * finalBallScaleRatio * (1f + sag));
            }

            if (mainCamera != null && smallBall != null)
            {
                Vector3 desiredCamPos = smallBall.position + camTargetOffset;
                mainCamera.transform.position = Vector3.Lerp(
                    mainCamera.transform.position, desiredCamPos, 1f - Mathf.Exp(-3f * dt));
                mainCamera.transform.rotation = Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    Quaternion.LookRotation((smallBall.position + slowWalkDirection * 2f) - mainCamera.transform.position),
                    1f - Mathf.Exp(-3f * dt));
            }

            yield return null;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // ── 辅助方法 ─────────────────────────────────────────────────

    /// <summary> 更自然的蹦跳曲线：起跳快、顶点慢、落地快 </summary>
    float BounceCurve(float phase)
    {
        return Mathf.Pow(Mathf.Abs(Mathf.Sin(phase * 2f)), 0.65f);
    }

    void ApplyBallMotion(Vector3 center, float x, float y, float vitalityFactor)
    {
        if (smallBall == null) return;
        smallBall.position = center + new Vector3(x, y, 0f);
    }

    void ApplyCarefreeSquashStretch(float normalizedHeight)
    {
        if (smallBall == null) return;
        float squash = 1f - squashStretchAmount * (1f - normalizedHeight);
        float stretch = 1f + squashStretchAmount * normalizedHeight * 0.5f;
        smallBall.localScale = new Vector3(
            _smallBallStartScale.x * stretch,
            _smallBallStartScale.y * squash,
            _smallBallStartScale.z * stretch);
    }

    void ResetBallScale()
    {
        if (smallBall != null)
            smallBall.localScale = _smallBallStartScale * Mathf.Lerp(finalBallScaleRatio, 1f, _vitality);
    }

    void ApplyVitalityVisuals(float vitality)
    {
        if (_smallBallMaterial == null) return;
        _smallBallMaterial.color = Color.Lerp(_ballColorEnd, _ballColorStart, vitality);

        if (smallBall != null)
        {
            float scale = Mathf.Lerp(finalBallScaleRatio, 1f, vitality);
            smallBall.localScale = _smallBallStartScale * scale;
        }
    }

    void UpdateFollowCamera(float height, bool lookAtRed = false, float redLookWeight = 0f)
    {
        if (mainCamera == null || smallBall == null) return;

        Vector3 desiredCamPos = smallBall.position + new Vector3(0f, height + 2.2f, -5f);
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position, desiredCamPos, 1f - Mathf.Exp(-6f * Time.deltaTime));

        Vector3 lookTarget = smallBall.position + Vector3.up * 0.5f;
        if (lookAtRed && redBall != null && redLookWeight > 0f)
            lookTarget = Vector3.Lerp(lookTarget, redBall.position, redLookWeight);

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            Quaternion.LookRotation(lookTarget - mainCamera.transform.position),
            1f - Mathf.Exp(-6f * Time.deltaTime));
    }

    float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    IEnumerator FadeInMusic(AudioClip clip, float duration, bool loop)
    {
        if (_audioSource == null || clip == null) yield break;
        _audioSource.clip = clip;
        _audioSource.loop = loop;
        _audioSource.volume = 0f;
        _audioSource.Play();
        yield return StartCoroutine(FadeInVolume(1f, duration));
    }

    IEnumerator FadeInVolume(float target, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(0f, target, t / duration);
            yield return null;
        }
        _audioSource.volume = target;
    }

    IEnumerator FadeOutAudio(float duration)
    {
        if (_audioSource == null || !_audioSource.isPlaying) yield break;
        float startVol = _audioSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        _audioSource.Stop();
        _audioSource.volume = 1f;
    }

    IEnumerator SwitchSkyboxWithFade()
    {
        if (fadeCanvas == null)
        {
            RenderSettings.skybox = skyboxAfter;
            DynamicGI.UpdateEnvironment();
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(skyboxFadeDuration, 0.01f);
            fadeCanvas.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        fadeCanvas.alpha = 1f;

        RenderSettings.skybox = skyboxAfter;
        DynamicGI.UpdateEnvironment();

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(skyboxFadeDuration, 0.01f);
            fadeCanvas.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }
}
