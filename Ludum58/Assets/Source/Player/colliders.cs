using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class ScreenEdgeColliders3Dd: MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // игрок (Transform с Rigidbody)

    [Header("Viewport margins (0..1)")]
    [Range(0,0.3f)] public float viewportMarginX = 0.06f;
    [Range(0,0.3f)] public float viewportMarginY = 0.06f;
    [Range(0,0.2f)] public float sideTighten     = 0.02f; // доп. сжатие по X

    [Header("Wall geometry")]
    public float thickness = 0.5f;
    public float depth     = 1000f;

    [Header("Physics")]
    public string wallsLayer = "Default";
    public PhysicsMaterial wallMaterial;
    public bool   autoEnable = true;
    public float  enableDelay = 0f;

    [Header("Spring bounce")]
    public float springK = 200f;
    public float springDamping = 35f;
    public float maxAccel = 800f;

    [Header("Contact behaviour")]
    public bool lockRotationOnHit = true;
    public float angularDampOnHit = 20f;
    [Range(0f,1f)] public float tangentDamping = 0.6f;

    [Header("Debug")]
    public bool drawGizmos = false;

    Camera _cam;
    Transform _root;
    BoxCollider _left, _right, _top, _bottom;
    SoftBouncer _bouncer;
    bool _enabled;

    void Awake()
    {
        _cam = GetComponent<Camera>();

        var go = new GameObject("ScreenWalls");
        go.layer = LayerMask.NameToLayer(wallsLayer);
        _root = go.transform;
        _root.SetParent(_cam.transform, false);

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        _left   = go.AddComponent<BoxCollider>();
        _right  = go.AddComponent<BoxCollider>();
        _top    = go.AddComponent<BoxCollider>();
        _bottom = go.AddComponent<BoxCollider>();

        foreach (var c in new[] {_left,_right,_top,_bottom})
        {
            c.isTrigger = false;
            if (wallMaterial) c.sharedMaterial = wallMaterial;
            c.enabled = false;
        }

        _bouncer = go.AddComponent<SoftBouncer>();
        _bouncer.owner = this;
        _bouncer.enabled = false;
    }

    void Start()
    {
        if (autoEnable) StartCoroutine(EnableLater());
    }

    IEnumerator EnableLater()
    {
        if (enableDelay > 0f) yield return new WaitForSeconds(enableDelay);
        EnableWallsNow();
    }

    public void EnableWallsNow()
    {
        _enabled = true;
        _bouncer.enabled = true;
        _left.enabled = _right.enabled = _top.enabled = _bottom.enabled = true;
        Physics.SyncTransforms();
    }

    void FixedUpdate()
    {
        if (!_cam || !target) return;

        // глубина цели вдоль взгляда
        float d = Vector3.Dot(target.position - _cam.transform.position, _cam.transform.forward);
        d = Mathf.Max(d, _cam.nearClipPlane + 0.5f);

        // прямоугольник внутри кадра
        float xMin = Mathf.Clamp01(viewportMarginX + sideTighten);
        float xMax = 1f - Mathf.Clamp01(viewportMarginX + sideTighten);
        float yMin = viewportMarginY;
        float yMax = 1f - viewportMarginY;

        Vector3 bl = _cam.ViewportToWorldPoint(new Vector3(xMin, yMin, d));
        Vector3 br = _cam.ViewportToWorldPoint(new Vector3(xMax, yMin, d));
        Vector3 tl = _cam.ViewportToWorldPoint(new Vector3(xMin, yMax, d));
        Vector3 tr = _cam.ViewportToWorldPoint(new Vector3(xMax, yMax, d));

        // в локальные координаты корня стен
        var t = _root;
        Vector3 blL = t.InverseTransformPoint(bl);
        Vector3 brL = t.InverseTransformPoint(br);
        Vector3 tlL = t.InverseTransformPoint(tl);
        Vector3 trL = t.InverseTransformPoint(tr);

        Vector3 right = (brL - blL).normalized;
        Vector3 up    = (tlL - blL).normalized;

        float width  = Mathf.Max(0.001f, (brL - blL).magnitude);
        float height = Mathf.Max(0.001f, (tlL - blL).magnitude);

        // лево/право
        _left.center   = (blL + tlL) * 0.5f - right * (thickness * 0.5f);
        _right.center  = (brL + trL) * 0.5f + right * (thickness * 0.5f);
        _left.size = _right.size = new Vector3(thickness, height, depth);

        // низ/верх
        _bottom.center = (blL + brL) * 0.5f - up * (thickness * 0.5f);
        _top.center    = (tlL + trL) * 0.5f + up * (thickness * 0.5f);
        _bottom.size = _top.size = new Vector3(width, thickness, depth);

        // включение
        if (_left.enabled != _enabled)
        {
            _left.enabled = _right.enabled = _top.enabled = _bottom.enabled = _enabled;
            _bouncer.enabled = _enabled;
        }

        // важная синхронизация для физики
        Physics.SyncTransforms();

        if (drawGizmos)
        {
            Debug.DrawLine(bl, br, Color.yellow, 0f, false);
            Debug.DrawLine(tl, tr, Color.yellow, 0f, false);
            Debug.DrawLine(bl, tl, Color.yellow, 0f, false);
            Debug.DrawLine(br, tr, Color.yellow, 0f, false);
        }
    }

    // контактная логика
    class SoftBouncer : MonoBehaviour
    {
        public ScreenEdgeColliders3Dd owner;
        readonly Dictionary<Rigidbody, RigidbodyConstraints> _saved = new();

        void OnCollisionEnter(Collision c)
        {
            var rb = c.rigidbody ?? c.collider.attachedRigidbody;
            if (!rb) return;

            if (owner.lockRotationOnHit && !_saved.ContainsKey(rb))
            {
                _saved[rb] = rb.constraints;
                rb.constraints |= RigidbodyConstraints.FreezeRotation;
            }
        }

        void OnCollisionStay(Collision c)
        {
            var rb = c.rigidbody ?? c.collider.attachedRigidbody;
            if (!rb) return;

            // усредняем нормаль и проникновение
            Vector3 nSum = Vector3.zero;
            float penSum = 0f;
            int cnt = c.contactCount;
            for (int i = 0; i < cnt; i++)
            {
                var cp = c.GetContact(i);
                nSum += cp.normal;
                penSum += Mathf.Max(0f, -cp.separation);
            }
            if (cnt == 0) return;

            Vector3 n = nSum.normalized;
            float penetration = penSum / cnt;

            // линейная скорость (Unity 6 совместимость)
            Vector3 v = GetVelocity(rb);
            float vN = Vector3.Dot(v, n);
            Vector3 vNorm = n * vN;
            Vector3 vTan  = v - vNorm;

            // приглушаем скольжение
            if (owner.tangentDamping < 1f)
                SetVelocity(rb, vNorm + vTan * owner.tangentDamping);

            // гасим вращение
            if (owner.lockRotationOnHit)
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, owner.angularDampOnHit * Time.fixedDeltaTime);

            // пружина
            Vector3 accel = n * (owner.springK * penetration) - n * (owner.springDamping * vN);
            float maxA = owner.maxAccel;
            if (accel.sqrMagnitude > maxA * maxA) accel = accel.normalized * maxA;

            rb.AddForce(accel, ForceMode.Acceleration);
        }

        void OnCollisionExit(Collision c)
        {
            var rb = c.rigidbody ?? c.collider.attachedRigidbody;
            if (!rb) return;

            if (_saved.TryGetValue(rb, out var cons))
            {
                rb.constraints = cons;
                _saved.Remove(rb);
            }
        }

        // совместимость velocity / linearVelocity
        static Vector3 GetVelocity(Rigidbody rb)
        {
            try { return rb.linearVelocity; } catch { return rb.linearVelocity; }
        }
        static void SetVelocity(Rigidbody rb, Vector3 v)
        {
            try { rb.linearVelocity = v; } catch { rb.linearVelocity = v; }
        }
    }
}
