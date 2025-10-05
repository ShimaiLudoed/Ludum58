using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class ScreenEdgeColliders3D : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;                 // игрок с Rigidbody

    [Header("Границы кадра (в долях экрана)")]
    [Range(0f, 0.3f)] public float viewportMarginX = 0.06f;
    [Range(0f, 0.3f)] public float viewportMarginY = 0.06f;
    [Range(0f, 0.2f)] public float sideTighten = 0.02f; // доп. сжатие по бокам

    [Header("Геометрия стен")]
    public float thickness = 0.25f;
    public float depth = 1000f;

    [Header("Физика стен")]
    public string wallsLayer = "Default";
    public PhysicsMaterial wallMaterial;

    [Header("Пружинный отскок")]
    public float springK = 220f;
    public float springDamping = 30f;
    public float maxAccel = 800f;

    [Header("Контакт без вращения")]
    public bool lockRotationOnHit = true;
    public float angularDampOnHit = 20f;      // насколько быстро гасить кручение
    [Range(0f,1f)] public float tangentDamping = 0.6f; // 0=полностью убрать скольжение вдоль стены

    [Header("Активация")]
    public float enableDelay = 1.0f;
    public bool autoEnable = true;

    Camera _cam;
    Transform _root;
    BoxCollider _left, _right, _top, _bottom;
    SoftBouncer _bouncer;
    bool _enabledWalls;

    void Awake()
    {
        _cam = GetComponent<Camera>();

        var go = new GameObject("ScreenWalls");
        go.layer = LayerMask.NameToLayer(wallsLayer);
        _root = go.transform;
        _root.SetParent(_cam.transform, false);

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;

        _left   = go.AddComponent<BoxCollider>();
        _right  = go.AddComponent<BoxCollider>();
        _top    = go.AddComponent<BoxCollider>();
        _bottom = go.AddComponent<BoxCollider>();
        foreach (var c in new[] { _left, _right, _top, _bottom })
        {
            c.isTrigger = false;
            if (wallMaterial) c.sharedMaterial = wallMaterial;
            c.enabled = false;
        }

        _bouncer = go.AddComponent<SoftBouncer>();
        _bouncer.enabled = false;
        _bouncer.owner = this; // доступ к настройкам
    }

    void Start()
    {
        if (autoEnable) StartCoroutine(EnableAfterDelay());
    }

    public void EnableWallsNow()
    {
        _enabledWalls = true;
        _bouncer.enabled = true;
        _left.enabled = _right.enabled = _top.enabled = _bottom.enabled = true;
    }

    IEnumerator EnableAfterDelay()
    {
        if (enableDelay > 0f) yield return new WaitForSeconds(enableDelay);
        EnableWallsNow();
    }

    void LateUpdate()
    {
        if (!_cam || !target) return;

        // глубина игрока
        float d = Vector3.Dot(target.position - _cam.transform.position, _cam.transform.forward);
        d = Mathf.Max(d, _cam.nearClipPlane + 0.5f);

        // внутренний прямоугольник кадра (с доп. сжатием по X)
        float xMin = Mathf.Clamp01(viewportMarginX + sideTighten);
        float xMax = 1f - Mathf.Clamp01(viewportMarginX + sideTighten);
        float yMin = viewportMarginY;
        float yMax = 1f - viewportMarginY;

        Vector3 bl = _cam.ViewportToWorldPoint(new Vector3(xMin, yMin, d));
        Vector3 br = _cam.ViewportToWorldPoint(new Vector3(xMax, yMin, d));
        Vector3 tl = _cam.ViewportToWorldPoint(new Vector3(xMin, yMax, d));
        Vector3 tr = _cam.ViewportToWorldPoint(new Vector3(xMax, yMax, d));

        var t = _root;
        Vector3 blL = t.InverseTransformPoint(bl);
        Vector3 brL = t.InverseTransformPoint(br);
        Vector3 tlL = t.InverseTransformPoint(tl);
        Vector3 trL = t.InverseTransformPoint(tr);

        Vector3 right = (brL - blL).normalized;
        Vector3 up    = (tlL - blL).normalized;

        float width  = (brL - blL).magnitude;
        float height = (tlL - blL).magnitude;

        _left.center   = (blL + tlL) * 0.5f - right * (thickness * 0.5f);
        _right.center  = (brL + trL) * 0.5f + right * (thickness * 0.5f);
        _left.size = _right.size = new Vector3(thickness, height, depth);

        _bottom.center = (blL + brL) * 0.5f - up * (thickness * 0.5f);
        _top.center    = (tlL + trL) * 0.5f + up * (thickness * 0.5f);
        _bottom.size = _top.size = new Vector3(width, thickness, depth);

        // включение/синхронизация
        if (_left.enabled != _enabledWalls)
        {
            _left.enabled = _right.enabled = _top.enabled = _bottom.enabled = _enabledWalls;
            _bouncer.enabled = _enabledWalls;
        }
    }

    // ---------- контактная логика ----------
    class SoftBouncer : MonoBehaviour
    {
        public ScreenEdgeColliders3D owner;

        void OnCollisionStay(Collision c)
        {
            if (!owner) return;
            var rb = c.rigidbody;
            if (!rb) return;

            // усредняем нормаль и проникновение
            Vector3 nSum = Vector3.zero;
            float penSum = 0f;
            int cnt = c.contactCount;

            for (int i = 0; i < cnt; i++)
            {
                var cp = c.GetContact(i);
                nSum += cp.normal;                         
                float pen = Mathf.Max(0f, -cp.separation); 
                penSum += pen;
            }
            if (cnt == 0) return;

            Vector3 n = nSum.normalized;
            float penetration = penSum / cnt;

            // скорость разбиваем на нормальную и тангенциальную
            Vector3 v = rb.linearVelocity;
            float vN = Vector3.Dot(v, n);
            Vector3 vNorm = n * vN;
            Vector3 vTan  = v - vNorm;

            // демпфируем тангенциальную составляющую, чтобы не крутило и не "скребло"
            if (owner.tangentDamping < 1f)
                rb.linearVelocity = vNorm + vTan * owner.tangentDamping;

            // гасим вращение, если включено
            if (owner.lockRotationOnHit)
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, owner.angularDampOnHit * Time.fixedDeltaTime);

            // пружинная сила к центру (по нормали)
            Vector3 accel = n * (owner.springK * penetration) - n * (owner.springDamping * vN);

            // лимит
            float maxA = owner.maxAccel;
            if (accel.sqrMagnitude > maxA * maxA)
                accel = accel.normalized * maxA;

            rb.AddForce(accel, ForceMode.Acceleration);
        }
    }
}
