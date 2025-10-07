using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Если пусто — возьмётся RenderSettings.skybox")]
    [SerializeField] private Material targetSkybox;

    [Header("Rotation")]
    [SerializeField] private float speedDegPerSec = 12f;
    [SerializeField] private bool startOnPlay = false;
    [SerializeField] private bool useUnscaledTime = true;

    float angle;
    bool rotating;
    Material runtimeMat;

    void Awake()
    {
        // Материал в RenderSettings уникализируем, чтобы править его в рантайме.
        runtimeMat = targetSkybox != null ? new Material(targetSkybox)
                                          : new Material(RenderSettings.skybox);
        RenderSettings.skybox = runtimeMat;

        angle = runtimeMat.HasProperty("_Rotation")
              ? runtimeMat.GetFloat("_Rotation")
              : 0f;

        rotating = startOnPlay;
    }

    void Update()
    {
        if (!rotating) return;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        angle = (angle + speedDegPerSec * dt) % 360f;
        if (runtimeMat.HasProperty("_Rotation"))
            runtimeMat.SetFloat("_Rotation", angle);
    }

    // КНОПКИ UI → OnClick: вызови один из методов ниже.
    public void StartRotation()  { if (!startOnPlay) rotating = true; }
    public void StopRotation()   { if (!startOnPlay) rotating = false; }
    public void ToggleRotation() { if (!startOnPlay) rotating = !rotating; }

    // Опционально: изменить скорость из UI Slider.
    public void SetSpeed(float newSpeed) { speedDegPerSec = newSpeed; }
}
