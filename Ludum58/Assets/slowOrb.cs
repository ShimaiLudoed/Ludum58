using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zenject;

public class SlowOrb : MonoBehaviour
{
    [Header("Speed")]
    [Range(0.05f, 1f)] public float slowFactor = 0.5f;
    public float transitionSpeed = 2f;
    public float slowDuration = 3f;

    [Header("Post-process (URP)")]
    public Volume globalVolume;
    public Color slowTint = new Color(0.75f, 0.9f, 1f);
    public float colorBlendSpeed = 2f;
    public float saturationChange = -25f;

    [Header("Visual / Pickup")]
    public float rotationSpeed = 50f;
    public Collider pickupCollider;
    public Renderer[] visuals;

    // --- Audio ---
    [Header("Audio Slowdown")]
    [Tooltip("Опционально: конкретный источник музыки. Можно оставить пустым.")]
    public AudioSource musicSource;
    [Tooltip("Управлять ли питчем музыки во время слоу-мо")]
    public bool controlMusicPitch = true;
    [Tooltip("Применять ко ВСЕМ AudioSource в сцене")]
    public bool affectAllAudioInScene = true;
    [Tooltip("Если true — целевой pitch равен slowFactor, иначе берётся ниже")]
    public bool useSlowFactorForPitch = true;
    [Range(0.05f, 1f)] public float musicPitchAtSlow = 0.5f;

    // runtime
    SingleTunnelSpawner _spawner;
    ColorAdjustments _colorAdj;
    float _origMul = 1f;
    Color _origColor = Color.white;
    float _origSat = 0f;
    bool _effectApplied;

    // audio runtime
    float _origPitch = 1f;                         // если задан одиночный musicSource
    List<AudioSource> _audioTargets;               // все источники
    List<float> _audioOrigPitches;                 // их исходные pitch

    [Header("Safety")]
    public bool persistUntilReverted = true;
    [Min(0)] public int extraRevertFrames = 1;
    public bool logDebug = false;
    bool _revertedOnce;

    [Header("Manager")]
    public bool delegateToManager = true;

    [Inject] public void Construct(SingleTunnelSpawner spawner) { _spawner = spawner; }

    void Start()
    {
        if (!pickupCollider) pickupCollider = GetComponent<Collider>();
        if (pickupCollider && !pickupCollider.isTrigger) pickupCollider.isTrigger = true;

        if (visuals == null || visuals.Length == 0) visuals = GetComponentsInChildren<Renderer>(true);

        if (!globalVolume) globalVolume = FindObjectOfType<Volume>();
        if (globalVolume && globalVolume.profile && globalVolume.profile.TryGet(out ColorAdjustments ca))
        {
            _colorAdj = ca;
            _origColor = _colorAdj.colorFilter.value;
            _origSat = _colorAdj.saturation.value;
        }
        else if (logDebug) Debug.LogWarning("[SlowOrb] ColorAdjustments not found; PP off");

        if (_spawner) _origMul = Mathf.Max(_spawner.GameSpeedMultiplier, 0.0001f);
        else { _origMul = 1f; if (logDebug) Debug.LogWarning("[SlowOrb] Spawner not injected; time slow off"); }

        // AUDIO: собрать цели
        _audioTargets = new List<AudioSource>(32);
        _audioOrigPitches = new List<float>(32);

        if (affectAllAudioInScene)
        {
            var all = FindObjectsOfType<AudioSource>(true);
            foreach (var a in all) { if (a) { _audioTargets.Add(a); _audioOrigPitches.Add(a.pitch); } }
        }
        else
        {
            if (!musicSource) musicSource = FindObjectOfType<AudioSource>();
            if (musicSource)
            {
                _origPitch = musicSource.pitch;
                _audioTargets.Add(musicSource);
                _audioOrigPitches.Add(musicSource.pitch);
            }
        }
    }

    void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.unscaledDeltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_effectApplied) return;

        bool isPlayer =
            other.CompareTag("Player") ||
            other.GetComponent<CharacterController>() ||
            other.GetComponentInParent<CharacterController>() ||
            other.GetComponentInParent<PlayerStats>();

        if (!isPlayer)
        {
            if (logDebug) Debug.Log($"[SlowOrb] Trigger by {other.name}, ignored");
            return;
        }

        if (pickupCollider) pickupCollider.enabled = false;
        if (visuals != null) foreach (var r in visuals) if (r) r.enabled = false;

        if (delegateToManager)
        {
            SlowMoManager.Run(
                spawner: _spawner,
                volume: globalVolume,
                colorAdj: _colorAdj,
                slowFactor: slowFactor,
                slowDuration: slowDuration,
                transSpeed: transitionSpeed,
                tint: slowTint,
                colorBlendSpeed: colorBlendSpeed,
                saturationDelta: saturationChange,
                log: logDebug,
                // audio
                audioTargets: _audioTargets,
                useSlowFactorForPitch: useSlowFactorForPitch,
                musicPitchAtSlow: musicPitchAtSlow,
                controlMusic: controlMusicPitch
            );
            Destroy(gameObject);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(EffectRoutine());
    }

    void OnDisable()
    {
        if (_effectApplied) ForceRevertNow();
    }

    void OnDestroy()
    {
        if (_effectApplied) ForceRevertNow();
    }

    IEnumerator EffectRoutine()
    {
        _effectApplied = true;

        if (persistUntilReverted)
        {
            if (logDebug) Debug.Log("[SlowOrb] Persist until reverted");
            DontDestroyOnLoad(gameObject);
        }

        try
        {
            if (_spawner) yield return SmoothMul(_spawner.GameSpeedMultiplier, Mathf.Clamp(slowFactor, 0.05f, 1f));
            if (_colorAdj) yield return TintTo(true);

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, slowDuration));

            if (_spawner)
            {
                float backTarget = (_origMul > 0f) ? _origMul : 1f;
                yield return SmoothMul(_spawner.GameSpeedMultiplier, backTarget);
            }
            if (_colorAdj) yield return TintTo(false);
        }
        finally
        {
            ForceRevertNow();
        }

        for (int i = 0; i < extraRevertFrames; i++) yield return null;
        yield return null;
        Destroy(gameObject);
    }

    void ForceRevertNow()
    {
        if (_revertedOnce) return;
        _revertedOnce = true;

        if (_spawner) _spawner.GameSpeedMultiplier = (_origMul > 0f ? _origMul : 1f);
        if (_colorAdj) { _colorAdj.colorFilter.value = _origColor; _colorAdj.saturation.value = _origSat; }

        if (controlMusicPitch && _audioTargets != null)
        {
            for (int i = 0; i < _audioTargets.Count; i++)
            {
                var a = _audioTargets[i];
                if (!a) continue;
                float p = (_audioOrigPitches != null && i < _audioOrigPitches.Count) ? _audioOrigPitches[i] : 1f;
                a.pitch = p;
            }
        }

        _effectApplied = false;
    }

    IEnumerator SmoothMul(float from, float to)
    {
        if (!_spawner) yield break;

        from = Mathf.Max(0.0001f, from);
        to   = Mathf.Max(0.0001f, to);

        // audio start/end per target
        List<float> pFrom = null, pTo = null;
        if (controlMusicPitch && _audioTargets != null && _audioTargets.Count > 0)
        {
            pFrom = new List<float>(_audioTargets.Count);
            pTo   = new List<float>(_audioTargets.Count);
            for (int i = 0; i < _audioTargets.Count; i++)
            {
                var a = _audioTargets[i];
                float cur = a ? a.pitch : 1f;
                float basePitch = (_audioOrigPitches != null && i < _audioOrigPitches.Count) ? _audioOrigPitches[i] : 1f;
                float targetPitch;
                if (to < from) targetPitch = useSlowFactorForPitch ? to : musicPitchAtSlow;   // вход
                else           targetPitch = basePitch;                                       // выход
                pFrom.Add(cur);
                pTo.Add(targetPitch);
            }
        }

        float t = 0f;
        while (t < 1f)
        {
            t += (transitionSpeed > 0f ? (Time.unscaledDeltaTime * transitionSpeed) : 1f);
            float k = Mathf.Clamp01(t);

            _spawner.GameSpeedMultiplier = Mathf.Lerp(from, to, k);

            if (pFrom != null)
            {
                for (int i = 0; i < _audioTargets.Count; i++)
                {
                    var a = _audioTargets[i];
                    if (!a) continue;
                    a.pitch = Mathf.Lerp(pFrom[i], pTo[i], k);
                }
            }

            yield return null;
        }

        _spawner.GameSpeedMultiplier = to;
        if (pTo != null)
        {
            for (int i = 0; i < _audioTargets.Count; i++)
            {
                var a = _audioTargets[i];
                if (!a) continue;
                a.pitch = pTo[i];
            }
        }
    }

    IEnumerator TintTo(bool slow)
    {
        if (_colorAdj == null) yield break;

        Color startC = _colorAdj.colorFilter.value;
        float startS = _colorAdj.saturation.value;

        Color endC = slow ? slowTint : _origColor;
        float endS = slow ? (_origSat + saturationChange) : _origSat;

        float t = 0f;
        while (t < 1f)
        {
            t += (colorBlendSpeed > 0f ? (Time.unscaledDeltaTime * colorBlendSpeed) : 1f);
            float k = Mathf.Clamp01(t);
            _colorAdj.colorFilter.value = Color.Lerp(startC, endC, k);
            _colorAdj.saturation.value  = Mathf.Lerp(startS, endS, k);
            yield return null;
        }

        _colorAdj.colorFilter.value = endC;
        _colorAdj.saturation.value  = endS;
    }

    // Zenject factory
    public class SlowOrbFactory : Zenject.PlaceholderFactory<SlowOrb> { }
}

/* ===================== PERSISTENT MANAGER ===================== */
public sealed class SlowMoManager : MonoBehaviour
{
    static SlowMoManager _inst;
    SingleTunnelSpawner _spawner;
    Volume _volume;
    ColorAdjustments _colorAdj;
    float _origMul = 1f;
    Color _origColor = Color.white;
    float _origSat = 0f;
    bool _active;
    bool _log;

    // audio
    List<AudioSource> _audios;
    List<float> _origPitches;
    bool _controlMusic;
    bool _useSlowFactorForPitch;
    float _musicPitchAtSlow = 0.5f;

    public static void Run(
        SingleTunnelSpawner spawner,
        Volume volume,
        ColorAdjustments colorAdj,
        float slowFactor,
        float slowDuration,
        float transSpeed,
        Color tint,
        float colorBlendSpeed,
        float saturationDelta,
        bool log,
        // audio
        List<AudioSource> audioTargets,
        bool useSlowFactorForPitch,
        float musicPitchAtSlow,
        bool controlMusic)
    {
        if (_inst == null)
        {
            var go = new GameObject("[SlowMoManager]");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<SlowMoManager>();
        }
        _inst._log = log;
        _inst.StartCoroutine(_inst.RunRoutine(
            spawner, volume, colorAdj,
            slowFactor, slowDuration, transSpeed, tint, colorBlendSpeed, saturationDelta,
            audioTargets, useSlowFactorForPitch, musicPitchAtSlow, controlMusic));
    }

    IEnumerator RunRoutine(
        SingleTunnelSpawner spawner,
        Volume volume,
        ColorAdjustments colorAdj,
        float slowFactor,
        float slowDuration,
        float transSpeed,
        Color tint,
        float colorBlendSpeed,
        float saturationDelta,
        // audio
        List<AudioSource> audioTargets,
        bool useSlowFactorForPitch,
        float musicPitchAtSlow,
        bool controlMusic)
    {
        if (_active)
        {
            if (_log) Debug.Log("[SlowMoManager] Busy, queuing next");
            while (_active) yield return null;
        }

        _active = true;

        _spawner = spawner;
        _volume  = volume;

        _colorAdj = colorAdj;
        if (_volume && _colorAdj == null && _volume.profile) _volume.profile.TryGet(out _colorAdj);

        _origMul = (_spawner ? Mathf.Max(_spawner.GameSpeedMultiplier, 0.0001f) : 1f);
        if (_colorAdj != null) { _origColor = _colorAdj.colorFilter.value; _origSat = _colorAdj.saturation.value; }

        // audio
        _controlMusic = controlMusic;
        _useSlowFactorForPitch = useSlowFactorForPitch;
        _musicPitchAtSlow = Mathf.Clamp(musicPitchAtSlow, 0.05f, 1f);

        _audios = new List<AudioSource>();
        _origPitches = new List<float>();
        if (_controlMusic)
        {
            if (audioTargets != null && audioTargets.Count > 0)
            {
                foreach (var a in audioTargets) { if (a) { _audios.Add(a); _origPitches.Add(a.pitch); } }
            }
            else
            {
                var all = FindObjectsOfType<AudioSource>(true);
                foreach (var a in all) { if (a) { _audios.Add(a); _origPitches.Add(a.pitch); } }
            }
        }

        // вход
        if (_spawner) yield return LerpMul(_origMul, Mathf.Clamp(slowFactor, 0.05f, 1f), transSpeed);
        if (_colorAdj) yield return LerpTint(_colorAdj.colorFilter.value, tint, _colorAdj.saturation.value, _origSat + saturationDelta, colorBlendSpeed);

        // держим
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, slowDuration));

        // выход
        if (_spawner) yield return LerpMul(_spawner.GameSpeedMultiplier, _origMul, transSpeed);
        if (_colorAdj) yield return LerpTint(_colorAdj.colorFilter.value, _origColor, _colorAdj.saturation.value, _origSat, colorBlendSpeed);

        // финал
        if (_spawner) _spawner.GameSpeedMultiplier = _origMul;
        if (_colorAdj) { _colorAdj.colorFilter.value = _origColor; _colorAdj.saturation.value = _origSat; }
        if (_controlMusic && _audios != null)
        {
            for (int i = 0; i < _audios.Count; i++)
            {
                var a = _audios[i];
                if (!a) continue;
                float p = (i < _origPitches.Count) ? _origPitches[i] : 1f;
                a.pitch = p;
            }
        }

        _active = false;
    }

    IEnumerator LerpMul(float from, float to, float speed)
    {
        from = Mathf.Max(0.0001f, from);
        to   = Mathf.Max(0.0001f, to);

        // аудио цели
        List<float> pFrom = null, pTo = null;
        if (_controlMusic && _audios != null && _audios.Count > 0)
        {
            pFrom = new List<float>(_audios.Count);
            pTo   = new List<float>(_audios.Count);
            for (int i = 0; i < _audios.Count; i++)
            {
                float cur = _audios[i] ? _audios[i].pitch : 1f;
                float basePitch = (i < _origPitches.Count) ? _origPitches[i] : 1f;
                float target = (to < from) ? (_useSlowFactorForPitch ? to : _musicPitchAtSlow) : basePitch;
                pFrom.Add(cur);
                pTo.Add(target);
            }
        }

        float t = 0f;
        while (t < 1f)
        {
            t += (speed > 0f ? Time.unscaledDeltaTime * speed : 1f);
            float k = Mathf.Clamp01(t);
            _spawner.GameSpeedMultiplier = Mathf.Lerp(from, to, k);

            if (pFrom != null)
            {
                for (int i = 0; i < _audios.Count; i++)
                {
                    var a = _audios[i];
                    if (!a) continue;
                    a.pitch = Mathf.Lerp(pFrom[i], pTo[i], k);
                }
            }

            yield return null;
        }

        _spawner.GameSpeedMultiplier = to;

        if (pTo != null)
        {
            for (int i = 0; i < _audios.Count; i++)
            {
                var a = _audios[i];
                if (!a) continue;
                a.pitch = pTo[i];
            }
        }
    }

    IEnumerator LerpTint(Color c0, Color c1, float s0, float s1, float speed)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += (speed > 0f ? Time.unscaledDeltaTime * speed : 1f);
            float k = Mathf.Clamp01(t);
            _colorAdj.colorFilter.value = Color.Lerp(c0, c1, k);
            _colorAdj.saturation.value  = Mathf.Lerp(s0, s1, k);
            yield return null;
        }
        _colorAdj.colorFilter.value = c1;
        _colorAdj.saturation.value  = s1;
    }
}
