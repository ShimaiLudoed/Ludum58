using UnityEngine;

public class DisappearNearCamera : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform player;
    public Camera targetCamera;

    [Header("Порог и тайминги")]
    public float fadeStartDistance = 6f;
    public float fadeDuration = 0.25f;
    public float killDistance = 1.5f;
    public bool disableCollidersOnFade = true;

    private float fadeT = -1f;
    private Vector3 startScale;
    private Collider[] colliders;

    void Awake()
    {
        startScale = transform.localScale;
        colliders = GetComponentsInChildren<Collider>(true);
    }

void Update()
{
    if (!player || !targetCamera) return;

    bool passedPlayer = transform.position.z <= player.position.z;
    float forwardDist = Vector3.Dot(transform.position - targetCamera.transform.position,
                                    targetCamera.transform.forward);

    // Если фейд ещё не начат
    if (fadeT < 0f)
    {
        // Старт фейда
        if (passedPlayer && forwardDist <= fadeStartDistance)
        {
            fadeT = 0f;
            if (disableCollidersOnFade) ToggleColliders(false);
        }

        // Если уже у камеры, вместо Destroy — форсируем мгновенный фейд
        if (passedPlayer && forwardDist <= killDistance && fadeT < 0f)
        {
            fadeT = 0f; // начни фейд
            if (disableCollidersOnFade) ToggleColliders(false);
        }
    }
    else
    {
        // Идёт фейд — НИКАКИХ Destroy по дистанции
        fadeT += Time.deltaTime / Mathf.Max(0.01f, fadeDuration);
        float k = 1f - Mathf.Clamp01(fadeT);
        transform.localScale = startScale * k;

        if (fadeT >= 1f) Destroy(gameObject);
    }
}

    void ToggleColliders(bool state)
    {
        if (colliders == null) return;
        foreach (var c in colliders)
            if (c) c.enabled = state;
    }
}