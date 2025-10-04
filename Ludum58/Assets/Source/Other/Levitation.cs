using UnityEngine;

[DisallowMultipleComponent]
public class CosmicLevitate : MonoBehaviour
{
    [Header("General")]
    [Tooltip("UI: RectTransform. 3D: Transform.localPosition")]
    public bool isUI = true;
    public bool useUnscaledTime = true;

    [Header("Position wobble (units or pixels)")]
    public Vector3 posAmplitude = new Vector3(8f, 12f, 0f);
    public Vector2 posSpeedRange = new Vector2(0.08f, 0.18f);

    [Header("Rotation wobble (degrees)")]
    public Vector3 rotAmplitude = new Vector3(0f, 0f, 6f);
    public Vector2 rotSpeedRange = new Vector2(0.06f, 0.12f);

    [Header("Scale pulse (additive, 1 = no change)")]
    public Vector2 scaleAmplitudeXY = new Vector2(0.02f, 0.02f);
    public Vector2 scaleSpeedRange = new Vector2(0.12f, 0.22f);

    [Header("Noise (very subtle)")]
    public float noiseAmount = 0.3f;     // добавка к posAmplitude в %
    public float noiseSpeed = 0.25f;

    [Header("Random seed (0 = auto)")]
    public int seed = 0;

    // cached
    RectTransform rt;
    Vector3 basePos, baseEuler, baseScale;
    Vector3 posFreq, rotFreq, scaleFreq;
    Vector3 posPhase, rotPhase, scalePhase;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (isUI && rt) { basePos = rt.anchoredPosition3D; }
        else            { basePos = transform.localPosition; }

        baseEuler = transform.localEulerAngles;
        baseScale = transform.localScale;

        int s = seed != 0 ? seed : (GetInstanceID() ^ System.Environment.TickCount);
        var rand = new System.Random(s);

        // helper
        float R(float a, float b) => Mathf.Lerp(a, b, (float)rand.NextDouble());
        Vector3 RV3(float a, float b) =>
            new Vector3(R(a,b), R(a,b), R(a,b));

        posFreq   = RV3(posSpeedRange.x,   posSpeedRange.y);
        rotFreq   = RV3(rotSpeedRange.x,   rotSpeedRange.y);
        scaleFreq = RV3(scaleSpeedRange.x, scaleSpeedRange.y);

        posPhase   = RV3(0f, Mathf.PI * 2f);
        rotPhase   = RV3(0f, Mathf.PI * 2f);
        scalePhase = RV3(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        // sinus offsets
        Vector3 pOff = new Vector3(
            Mathf.Sin((t * posFreq.x)   + posPhase.x) * posAmplitude.x,
            Mathf.Sin((t * posFreq.y)   + posPhase.y) * posAmplitude.y,
            Mathf.Sin((t * posFreq.z)   + posPhase.z) * posAmplitude.z
        );

        Vector3 rOff = new Vector3(
            Mathf.Sin((t * rotFreq.x)   + rotPhase.x) * rotAmplitude.x,
            Mathf.Sin((t * rotFreq.y)   + rotPhase.y) * rotAmplitude.y,
            Mathf.Sin((t * rotFreq.z)   + rotPhase.z) * rotAmplitude.z
        );

        float sx = 1f + Mathf.Sin((t * scaleFreq.x) + scalePhase.x) * scaleAmplitudeXY.x;
        float sy = 1f + Mathf.Sin((t * scaleFreq.y) + scalePhase.y) * scaleAmplitudeXY.y;

        // subtle noise on position amplitude
        float n = noiseAmount * 0.01f;
        if (n > 0f)
        {
            float nx = (Mathf.PerlinNoise(t * noiseSpeed,  13.37f) - 0.5f) * 2f * posAmplitude.x * n;
            float ny = (Mathf.PerlinNoise(t * noiseSpeed,  42.42f) - 0.5f) * 2f * posAmplitude.y * n;
            pOff.x += nx; pOff.y += ny;
        }

        // apply
        if (isUI && rt) rt.anchoredPosition3D = basePos + pOff;
        else            transform.localPosition = basePos + pOff;

        transform.localEulerAngles = baseEuler + rOff;
        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        posSpeedRange.x   = Mathf.Max(0.001f, Mathf.Min(posSpeedRange.x,   posSpeedRange.y));
        rotSpeedRange.x   = Mathf.Max(0.001f, Mathf.Min(rotSpeedRange.x,   rotSpeedRange.y));
        scaleSpeedRange.x = Mathf.Max(0.001f, Mathf.Min(scaleSpeedRange.x, scaleSpeedRange.y));
    }
#endif
}
