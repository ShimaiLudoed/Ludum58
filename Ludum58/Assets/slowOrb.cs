using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zenject;

public class SlowOrb : MonoBehaviour
{
    [Header("Параметры замедления")]
    [Tooltip("Во сколько раз уменьшать скорость (1 = без изменений)")]
    [Range(0.05f, 2f)] public float slowFactor = 0.5f;
    [Tooltip("Скорость плавного перехода (чем больше — тем быстрее смена)")]
    public float transitionSpeed = 1f;
    [Tooltip("Длительность эффекта (сек)")]
    public float slowDuration = 5f;

    [Header("Постобработка")]
    public Volume globalVolume;
    public Color slowTint = new Color(0.3f, 0.6f, 1f, 1f);
    public float colorBlendSpeed = 2f;
    [Range(-100f, 100f)] public float saturationChange = -30f;

    [Header("Визуал")]
    public float rotationSpeed = 60f;
    [SerializeField] Collider pickupCollider;
    [SerializeField] Renderer[] visuals;

    private SingleTunnelSpawner _spawner;
    private float _origMul = 1f;

    private ColorAdjustments _colorAdj;
    private Color _origColor;
    private float _origSat;

    [Inject] public void Construct(SingleTunnelSpawner spawner) { _spawner = spawner; }

    void Start()
    {
        if (!pickupCollider) pickupCollider = GetComponent<Collider>();
        if (visuals == null || visuals.Length == 0) visuals = GetComponentsInChildren<Renderer>(true);

        if (!globalVolume) globalVolume = FindObjectOfType<Volume>(true);
        if (globalVolume && globalVolume.profile && globalVolume.profile.TryGet(out _colorAdj))
        {
            _colorAdj.colorFilter.overrideState = true;
            _colorAdj.saturation.overrideState = true;
            _origColor = _colorAdj.colorFilter.value;
            _origSat   = _colorAdj.saturation.value;
        }

        if (_spawner != null) _origMul = _spawner.GameSpeedMultiplier;
    }

    void Update() => transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

    void OnTriggerEnter(Collider other)
    {
        if (!_spawner) return;
        if (!other.GetComponentInParent<PlayerStats>()) return;

        if (pickupCollider) pickupCollider.enabled = false;
        SetVisuals(false);

        StopAllCoroutines();
        StartCoroutine(EffectRoutine());
    }

    System.Collections.IEnumerator EffectRoutine()
    {
        // 1) Замедляем
        yield return SmoothMul(_spawner.GameSpeedMultiplier, Mathf.Clamp(slowFactor, 0.01f, 10f));

        // 2) Тонируем
        if (_colorAdj) yield return TintTo(slow: true);

        // 3) Ждём длительность эффекта
        float t = 0f;
        while (t < slowDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 4) Возвращаем скорость
        yield return SmoothMul(_spawner.GameSpeedMultiplier, _origMul <= 0f ? 1f : _origMul);

        // 5) Возвращаем цвет
        if (_colorAdj) yield return TintTo(slow: false);

        // 6) Теперь гарантированно уничтожаем
        Destroy(gameObject);
    }

    System.Collections.IEnumerator SmoothMul(float from, float to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * Mathf.Max(transitionSpeed, 0.0001f);
            _spawner.GameSpeedMultiplier = Mathf.Lerp(from, to, t);
            yield return null;
        }
        _spawner.GameSpeedMultiplier = to;
    }

    System.Collections.IEnumerator TintTo(bool slow)
    {
        float t = 0f;
        Color cFrom = slow ? _origColor : slowTint;
        Color cTo   = slow ? slowTint   : _origColor;
        float sFrom = slow ? _origSat : (_origSat + saturationChange);
        float sTo   = slow ? (_origSat + saturationChange) : _origSat;

        while (t < 1f)
        {
            t += Time.deltaTime * Mathf.Max(colorBlendSpeed, 0.0001f);
            _colorAdj.colorFilter.value = Color.Lerp(cFrom, cTo, t);
            _colorAdj.saturation.value  = Mathf.Lerp(sFrom, sTo, t);
            yield return null;
        }
        _colorAdj.colorFilter.value = cTo;
        _colorAdj.saturation.value  = sTo;
    }

    void SetVisuals(bool on)
    {
        if (visuals == null) return;
        foreach (var r in visuals)
            if (r) r.enabled = on;
    }

    public class SlowOrbFactory : PlaceholderFactory<SlowOrb> { }
}
