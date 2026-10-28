using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized audio manager. Generates procedural SFX and BGM.
/// Handles 3D spatial audio, footstep scheduling, and BGM crossfade.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Master")]
    [SerializeField] float masterVolume = 1f;
    [SerializeField] float sfxVolume = 0.8f;
    [SerializeField] float bgmVolume = 0.5f;

    [Header("Audio Source Pool")]
    [SerializeField] int poolSize = 16;

    readonly List<AudioSource> _sfxPool = new();
    AudioSource _bgmSource;
    AudioSource _bgmSource2;
    int _poolIndex;

    readonly Dictionary<string, AudioClip> _clips = new();
    readonly Dictionary<string, float> _clipVolumes = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitAudioSources();
        GenerateAllClips();
    }

    void InitAudioSources()
    {
        // BGM sources
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.volume = 0f;
        _bgmSource2 = gameObject.AddComponent<AudioSource>();
        _bgmSource2.loop = true;
        _bgmSource2.volume = 0f;

        // SFX pool
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f; // 3D by default
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.maxDistance = 30f;
            src.minDistance = 1f;
            _sfxPool.Add(src);
        }
    }

    #region Public API

    public void PlaySfx(string name)
    {
        if (_clips.TryGetValue(name, out var clip))
            PlayOnPool(clip, Vector3.zero, false, _clipVolumes.GetValueOrDefault(name, 1f) * sfxVolume * masterVolume);
    }

    public void PlaySfxAt(Vector3 pos, string name)
    {
        if (_clips.TryGetValue(name, out var clip))
            PlayOnPool(clip, pos, true, _clipVolumes.GetValueOrDefault(name, 1f) * sfxVolume * masterVolume);
    }

    public void PlaySfxAt(Vector3 pos, string name, float pitchVariation)
    {
        if (_clips.TryGetValue(name, out var clip))
        {
            var src = GetNextSource();
            src.transform.position = pos;
            src.spatialBlend = 1f;
            src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            src.PlayOneShot(clip, _clipVolumes.GetValueOrDefault(name, 1f) * sfxVolume * masterVolume);
        }
    }

    public void PlayBgm(string name)
    {
        if (!_clips.TryGetValue(name, out var clip)) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        CrossfadeBgm(clip);
    }

    public void StopBgm()
    {
        _bgmSource.Stop();
        _bgmSource2.Stop();
    }

    public void SetMasterVolume(float vol) => masterVolume = vol;
    public void SetSfxVolume(float vol) => sfxVolume = vol;
    public void SetBgmVolume(float vol) => bgmVolume = vol;

    public float MasterVolume => masterVolume;
    public float SfxVolume => sfxVolume;
    public float BgmVolume => bgmVolume;

    #endregion

    #region Internal

    AudioSource GetNextSource()
    {
        var src = _sfxPool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _sfxPool.Count;
        src.pitch = 1f;
        return src;
    }

    void PlayOnPool(AudioClip clip, Vector3 pos, bool spatial3D, float volume)
    {
        var src = GetNextSource();
        src.transform.position = pos;
        src.spatialBlend = spatial3D ? 1f : 0f;
        src.PlayOneShot(clip, volume);
    }

    void CrossfadeBgm(AudioClip newClip)
    {
        // Simple swap: fade in new, fade out old
        var oldSrc = _bgmSource;
        var newSrc = _bgmSource2;
        if (_bgmSource.isPlaying)
        {
            oldSrc = _bgmSource;
            newSrc = _bgmSource2;
        }
        else
        {
            oldSrc = _bgmSource2;
            newSrc = _bgmSource;
        }

        newSrc.clip = newClip;
        newSrc.volume = 0f;
        newSrc.Play();

        StartCoroutine(CrossfadeRoutine(oldSrc, newSrc));
    }

    System.Collections.IEnumerator CrossfadeRoutine(AudioSource fadeOut, AudioSource fadeIn)
    {
        float t = 0f;
        float duration = 1.5f;
        float startVol = fadeOut.volume;
        while (t < duration)
        {
            t += Time.deltaTime;
            float ratio = t / duration;
            fadeOut.volume = Mathf.Lerp(startVol, 0f, ratio);
            fadeIn.volume = Mathf.Lerp(0f, bgmVolume * masterVolume, ratio);
            yield return null;
        }
        fadeOut.Stop();
    }

    #endregion

    #region Procedural Audio Generation

    void GenerateAllClips()
    {
        // SFX
        AddClip("PortalShoot", Sfx.PortalShoot(), 0.6f);
        AddClip("PortalEnter", Sfx.PortalEnter(), 0.7f);
        AddClip("PlatePress", Sfx.PlatePress(), 0.5f);
        AddClip("PlateRelease", Sfx.PlateRelease(), 0.4f);
        AddClip("DoorOpen", Sfx.DoorOpen(), 0.5f);
        AddClip("DoorClose", Sfx.DoorClose(), 0.5f);
        AddClip("CubePickup", Sfx.CubePickup(), 0.5f);
        AddClip("CubeDrop", Sfx.CubeDrop(), 0.4f);
        AddClip("LaserActivate", Sfx.LaserActivate(), 0.4f);
        AddClip("LaserDeactivate", Sfx.LaserDeactivate(), 0.3f);
        AddClip("ZoneEnter", Sfx.ZoneEnter(), 0.4f);
        AddClip("PuzzleComplete", Sfx.PuzzleComplete(), 0.8f);
        AddClip("Footstep", Sfx.Footstep(), 0.2f);
        AddClip("Footstep2", Sfx.Footstep(), 0.2f);
        AddClip("UIClick", Sfx.UIClick(), 0.5f);
        AddClip("Connect", Sfx.Connect(), 0.6f);

        // BGM
        AddClip("ChamberBGM", Bgm.GenerateChamberBgm(), 1f);
        AddClip("MenuBGM", Bgm.GenerateMenuBgm(), 1f);
    }

    void AddClip(string name, AudioClip clip, float vol)
    {
        _clips[name] = clip;
        _clipVolumes[name] = vol;
    }

    #endregion

    #region Procedural SFX Generators

    static class Sfx
    {
        const int SR = 44100;

        public static AudioClip PortalShoot()
        {
            float dur = 0.3f;
            var clip = AudioClip.Create("PortalShoot", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 8f);
                float freq = Mathf.Lerp(1200, 400, t);
                data[i] = (Mathf.Sin(2 * Mathf.PI * freq * t) * 0.5f + GenNoise() * 0.3f) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip PortalEnter()
        {
            float dur = 0.5f;
            var clip = AudioClip.Create("PortalEnter", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 4f);
                float sweep = Mathf.Lerp(100, 2000, t);
                data[i] = (Mathf.Sin(2 * Mathf.PI * sweep * t) * 0.3f
                          + GenNoise() * 0.5f * Mathf.Exp(-t * 6f)) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip PlatePress()
        {
            float dur = 0.15f;
            var clip = AudioClip.Create("PlatePress", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 25f);
                data[i] = (Mathf.Sin(2 * Mathf.PI * 150 * t) * 0.6f + GenNoise() * 0.4f) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip PlateRelease()
        {
            float dur = 0.12f;
            var clip = AudioClip.Create("PlateRelease", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 30f);
                data[i] = (Mathf.Sin(2 * Mathf.PI * 300 * t) * 0.5f + GenNoise() * 0.3f) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip DoorOpen()
        {
            float dur = 0.8f;
            var clip = AudioClip.Create("DoorOpen", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.SmoothStep(0, 1, t) * (1 - Mathf.Exp(-t * 2f));
                float motor = Mathf.Sin(2 * Mathf.PI * 80 * t) * 0.4f;
                float grind = GenNoise() * 0.3f * Mathf.Lerp(1, 0.3f, t);
                data[i] = (motor + grind) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip DoorClose()
        {
            float dur = 0.5f;
            var clip = AudioClip.Create("DoorClose", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 3f);
                float motor = Mathf.Sin(2 * Mathf.PI * 60 * t) * 0.5f;
                data[i] = (motor + GenNoise() * 0.3f) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CubePickup()
        {
            float dur = 0.2f;
            var clip = AudioClip.Create("CubePickup", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 15f);
                data[i] = (Mathf.Sin(2 * Mathf.PI * 500 * t) * 0.3f + GenNoise() * 0.4f) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CubeDrop()
        {
            float dur = 0.3f;
            var clip = AudioClip.Create("CubeDrop", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 10f);
                data[i] = (Mathf.Sin(2 * Mathf.PI * 120 * t) * 0.5f + GenNoise() * 0.5f) * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip LaserActivate()
        {
            float dur = 0.4f;
            var clip = AudioClip.Create("LaserActivate", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 5f);
                float freq = Mathf.Lerp(200, 800, t);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.4f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip LaserDeactivate()
        {
            float dur = 0.3f;
            var clip = AudioClip.Create("LaserDeactivate", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 8f);
                float freq = Mathf.Lerp(800, 200, t);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.3f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip ZoneEnter()
        {
            float dur = 0.5f;
            var clip = AudioClip.Create("ZoneEnter", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 3f);
                float freq = 440 * Mathf.Pow(2, t * 3);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.3f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip PuzzleComplete()
        {
            float dur = 2f;
            var clip = AudioClip.Create("PuzzleComplete", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            // Success melody: C-E-G-C
            float[] notes = { 523, 659, 784, 1047 };
            float noteDur = dur / notes.Length;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                int noteIdx = Mathf.FloorToInt(t / noteDur);
                noteIdx = Mathf.Clamp(noteIdx, 0, notes.Length - 1);
                float noteT = t - noteIdx * noteDur;
                float env = Mathf.Exp(-noteT * 3f) * (1 - Mathf.Exp(-noteT * 50f));
                data[i] = Mathf.Sin(2 * Mathf.PI * notes[noteIdx] * t) * env * 0.4f
                        + Mathf.Sin(2 * Mathf.PI * notes[noteIdx] * 2 * t) * env * 0.15f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip Footstep()
        {
            float dur = 0.1f;
            var clip = AudioClip.Create("Footstep", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 40f);
                data[i] = GenNoise() * env * 0.4f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip UIClick()
        {
            float dur = 0.08f;
            var clip = AudioClip.Create("UIClick", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 30f);
                data[i] = Mathf.Sin(2 * Mathf.PI * 800 * t) * env * 0.3f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip Connect()
        {
            float dur = 0.5f;
            var clip = AudioClip.Create("Connect", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 4f);
                float freq = Mathf.Lerp(300, 900, t);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.4f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        static float GenNoise() => Random.Range(-1f, 1f);
    }

    #endregion

    #region Procedural BGM Generators

    static class Bgm
    {
        const int SR = 44100;

        public static AudioClip GenerateChamberBgm()
        {
            float dur = 16f;
            var clip = AudioClip.Create("ChamberBGM", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];

            // Ambient pad with arpeggio in A minor
            float[] scale = { 220, 262, 294, 330, 392, 440, 523, 587 };
            float[] bassNotes = { 110, 110, 87, 98 }; // A, A, F, G
            float beatDur = 0.5f;
            int samplesPerBeat = (int)(SR * beatDur);

            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                int beatIdx = (int)(t / beatDur) % 32;

                // Bass line
                int bassIdx = (beatIdx / 8) % bassNotes.Length;
                float bassNote = bassNotes[bassIdx];
                float bassEnv = Mathf.Exp(-(t % beatDur) * 1.5f);
                float bass = Mathf.Sin(2 * Mathf.PI * bassNote * t) * 0.2f * bassEnv;

                // Arpeggio
                int arpIdx = beatIdx % scale.Length;
                float arpNote = scale[arpIdx];
                float arpEnv = Mathf.Exp(-(t % beatDur) * 3f);
                float arp = Mathf.Sin(2 * Mathf.PI * arpNote * t) * 0.08f * arpEnv;

                // Pad (sustained chord)
                float pad = (Mathf.Sin(2 * Mathf.PI * 110 * t) + Mathf.Sin(2 * Mathf.PI * 165 * t)
                            + Mathf.Sin(2 * Mathf.PI * 220 * t)) * 0.02f;

                // Subtle noise for atmosphere
                float noise = Mathf.Sin(2 * Mathf.PI * 0.5f * t) * 0.01f;

                data[i] = bass + arp + pad + noise;
            }

            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip GenerateMenuBgm()
        {
            float dur = 12f;
            var clip = AudioClip.Create("MenuBGM", (int)(SR * dur), 1, SR, false);
            var data = new float[(int)(SR * dur)];

            // Gentle melody in C major
            float[] melody = { 523, 659, 784, 659, 523, 587, 659, 523 };
            float noteDur = 1.5f;

            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SR;
                int noteIdx = (int)(t / noteDur) % melody.Length;
                float noteT = t - noteIdx * noteDur;
                float env = Mathf.Exp(-noteT * 1f) * (1 - Mathf.Exp(-noteT * 10f));
                float note = melody[noteIdx];

                float melodySound = Mathf.Sin(2 * Mathf.PI * note * t) * 0.15f * env;
                float harmony = Mathf.Sin(2 * Mathf.PI * note * 1.5f * t) * 0.08f * env;
                float bass = Mathf.Sin(2 * Mathf.PI * note * 0.5f * t) * 0.1f;

                data[i] = melodySound + harmony + bass;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }

    #endregion
}
