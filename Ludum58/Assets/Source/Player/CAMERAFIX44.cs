using UnityEngine;

public class ForwardCameraController : MonoBehaviour
{
    [Header("Камера и движение")]
    public float cameraSpeed = 10f;
    public Vector3 cameraDirection = Vector3.forward;

    [Header("Игрок")]
    public Transform player;
    public Rigidbody playerRb;
    public Camera cam;

    [Header("Старые рамки (X/Y, можно выключить)")]
    public bool legacyPlanarBounds = false;
    public float boundaryX = 5f;
    public float boundaryY = 3f;

    [Header("Новые рамки: отдельные стороны (viewport 0..1)")]
    [Tooltip("Отступ слева от 0..1")]
    [Range(0f, 0.49f)] public float leftMargin = 0.08f;
    [Tooltip("Отступ справа от 0..1")]
    [Range(0f, 0.49f)] public float rightMargin = 0.08f;
    [Tooltip("Отступ снизу от 0..1")]
    [Range(0f, 0.49f)] public float bottomMargin = 0.08f;
    [Tooltip("Отступ сверху от 0..1")]
    [Range(0f, 0.49f)] public float topMargin = 0.12f;

    [Header("Возврат в рамки")]
    public float pullStrength = 40f;   // сила возврата
    public float bounceDamping = 6f;   // демпфирование текущей скорости

    [Header("Задержка активации")]
    public float boundaryActivationDelay = 2f;
    float timer; bool active;

    // === ДОБАВЛЕНО: шейк через CinemachineBasicMultiChannelPerlin на VCam ===
    [Header("Шейк камеры (Cinemachine)")]
    public VCamNoisePulse shaker;      // перетащи компонент с VCam
    public float shakeDuration = 0.5f; // длительность включения шума
    bool outL, outR, outB, outT;       // состояние: за границей в прошлый кадр

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody>();

        timer = boundaryActivationDelay;
        active = false;
    }

    void Update()
    {
        transform.position += cameraDirection.normalized * cameraSpeed * Time.deltaTime;

        if (!active)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) active = true;
        }
    }

    void FixedUpdate()
    {
        if (!active || !player || !playerRb || !cam) return;

        // === Новые рамки: viewport ===
        Vector3 vp = cam.WorldToViewportPoint(player.position);
        float minX = leftMargin;
        float maxX = 1f - rightMargin;
        float minY = bottomMargin;
        float maxY = 1f - topMargin;

        float targetX = Mathf.Clamp(vp.x, minX, maxX);
        float targetY = Mathf.Clamp(vp.y, minY, maxY);

        // === ДОБАВЛЕНО: детект входа за каждую грань для шейка ===
        bool nowL = vp.x < minX;
        bool nowR = vp.x > maxX;
        bool nowB = vp.y < minY;
        bool nowT = vp.y > maxY;

        if (shaker)
        {
            if (nowL && !outL) shaker.Pulse(shakeDuration);
            if (nowR && !outR) shaker.Pulse(shakeDuration);
            if (nowB && !outB) shaker.Pulse(shakeDuration);
            if (nowT && !outT) shaker.Pulse(shakeDuration);
        }
        outL = nowL; outR = nowR; outB = nowB; outT = nowT;

        Vector3 force = Vector3.zero;

        // Если вышли за пределы — считаем точку возврата на той же глубине
        if (!Mathf.Approximately(vp.x, targetX) || !Mathf.Approximately(vp.y, targetY))
        {
            Vector3 targetVp = new Vector3(targetX, targetY, Mathf.Max(0.01f, vp.z));
            Vector3 worldTarget = cam.ViewportToWorldPoint(targetVp);
            Vector3 toInside = worldTarget - player.position;          // куда толкать
            Vector3 damping = -playerRb.linearVelocity * bounceDamping;

            force += toInside.normalized * pullStrength + damping;
        }

        // === Legacy X/Y (опционально, как раньше) ===
        if (legacyPlanarBounds)
        {
            // локаль камеры
            Vector3 local = cam.transform.InverseTransformPoint(player.position);

            float dx = local.x;
            float dy = local.y;
            Vector3 extra = Vector3.zero;

            if (dx > boundaryX)       extra += -cam.transform.right * (dx - boundaryX) * pullStrength;
            else if (dx < -boundaryX) extra +=  cam.transform.right * (-boundaryX - dx) * pullStrength;

            if (dy > boundaryY)       extra += -cam.transform.up * (dy - boundaryY) * pullStrength;
            else if (dy < -boundaryY) extra +=  cam.transform.up * (-boundaryY - dy) * pullStrength;

            force += extra;
        }

        if (force.sqrMagnitude > 0f)
            playerRb.AddForce(force, ForceMode.Acceleration);
    }
}
