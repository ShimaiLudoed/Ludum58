using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class PlayerController : MonoBehaviour
{
    [Header("Камера и эффекты")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private ParticleSystem jetpackParticles;
    [SerializeField] private AudioSource jetpackAudio;

    // === Новые управляемые параметры ===
    [Header("Движение — таргет-скорость")]
    [SerializeField] private float maxSpeed = 12f;          // макс. горизонтальная скорость
    [SerializeField] private float accel = 30f;             // ускорение при нажатии
    [SerializeField] private float decel = 40f;             // торможение без ввода
    [SerializeField] private float maxVerticalSpeed = 6f;   // макс. вертикальная
    [SerializeField] private float verticalAccel = 25f;     // ускорение по Y

    [Header("Фильтры ввода")]
    [SerializeField] private float deadZone = 0.1f;         // отсечь дрожание стика/клавиш
    [SerializeField] private bool normalizeDiagonal = true; // фикс WA/WD спайка

    // === Твои исходные данные ===
    private FloatData _floatData;
    private PlayerInput _playerInput;
    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector2 _lookInput; // оставил, вдруг используешь позже
    private bool _isBoosting = false;

    private Vector3 _thrustDirection; // для эффектов
    public event Action OnTakeStar;

    [Inject]
    public void Construct(FloatData floatData)
    {
        _floatData = floatData;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();

        _rb.linearDamping = 0.05f;
        _rb.angularDamping = 0.3f;
        _rb.useGravity = false;

        Cursor.lockState = CursorLockMode.Locked;
        _rb.linearVelocity = Vector3.forward * _floatData.startForce;
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    // Чтение ввода — в Update
    private void Update()
    {
        _moveInput = _playerInput.actions["Move"].ReadValue<Vector2>();
        _isBoosting = _playerInput.actions["Jump"].IsPressed();

        // Для эффектов и старины
        BuildThrustDirectionForVFX();

        HandleStabilization(); // оставил как было
        UpdateJetpackEffects();

        if (_playerInput.actions["Attack"].triggered)
            OnTakeStar?.Invoke();
    }

    // Физика — в FixedUpdate
    private void FixedUpdate()
    {
        if (playerCamera == null) return;

        // Базисы камеры по полу
        Vector3 f = playerCamera.forward; f.y = 0f; f.Normalize();
        Vector3 r = playerCamera.right;   r.y = 0f; r.Normalize();

        // Ввод с мёртвой зоной
        Vector2 in2 = _moveInput;
        if (in2.sqrMagnitude < deadZone * deadZone) in2 = Vector2.zero;

        // Нормализация диагонали (фикс WA/WD)
        if (normalizeDiagonal && in2.sqrMagnitude > 1f) in2.Normalize();

        // Горизонтальный таргет
        float boostMul = _isBoosting ? _floatData.boostMultiplier : 1f;
        float targetPlanarSpeed = maxSpeed * boostMul;
        Vector3 dirPlanar = (f * in2.y + r * in2.x);
        if (dirPlanar.sqrMagnitude > 1e-6f) dirPlanar.Normalize(); // ключ к фиксированию диагонали

        Vector3 v = _rb.linearVelocity;
        Vector3 vPlanar = new Vector3(v.x, 0f, v.z);
        Vector3 targetPlanarVel = dirPlanar * targetPlanarSpeed;

        // Вычислить требуемое изменение скорости на этот тик
        Vector3 deltaPlanar = targetPlanarVel - vPlanar;
        float allowedAccel = (dirPlanar == Vector3.zero ? decel : accel) * Time.fixedDeltaTime;

        if (deltaPlanar.magnitude > allowedAccel)
            deltaPlanar = deltaPlanar.normalized * allowedAccel;

        // Вертикаль (Jump/Crouch)
        float verticalInput = 0f;
        if (_playerInput.actions["Jump"].IsPressed()) verticalInput = 1f;
        else if (_playerInput.actions["Crouch"].IsPressed()) verticalInput = -1f;

        float targetVy = verticalInput * maxVerticalSpeed;
        float dvY = Mathf.Clamp(targetVy - v.y, -verticalAccel * Time.fixedDeltaTime, verticalAccel * Time.fixedDeltaTime);

        // Применить как VelocityChange, без зависимости от массы
        Vector3 deltaV = new Vector3(deltaPlanar.x, dvY, deltaPlanar.z);
        _rb.AddForce(deltaV, ForceMode.VelocityChange);

        // Жёсткий кап скорости на всякий
        Vector3 capped = _rb.linearVelocity;
        Vector3 cappedPlanar = new Vector3(capped.x, 0f, capped.z);
        if (cappedPlanar.magnitude > targetPlanarSpeed)
        {
            cappedPlanar = cappedPlanar.normalized * targetPlanarSpeed;
            capped = new Vector3(cappedPlanar.x, capped.y, cappedPlanar.z);
        }
        capped.y = Mathf.Clamp(capped.y, -maxVerticalSpeed, maxVerticalSpeed);
        _rb.linearVelocity = capped;
    }

    // ===== Служебные: визуал, стабилизация =====

    private void BuildThrustDirectionForVFX()
    {
        Vector3 cameraForward = playerCamera ? playerCamera.forward : Vector3.forward;
        Vector3 cameraRight = playerCamera ? playerCamera.right : Vector3.right;

        cameraForward.y = 0f; cameraRight.y = 0f;
        cameraForward.Normalize(); cameraRight.Normalize();

        Vector3 planar = cameraForward * _moveInput.y + cameraRight * _moveInput.x;
        if (normalizeDiagonal && planar.sqrMagnitude > 1f) planar.Normalize();

        float verticalThrust = 0f;
        if (_playerInput.actions["Jump"].IsPressed()) verticalThrust = 1f;
        else if (_playerInput.actions["Crouch"].IsPressed()) verticalThrust = -1f;

        _thrustDirection = planar + Vector3.up * verticalThrust;
    }

    private void HandleStabilization()
    {
        if (_lookInput.magnitude < 0.1f)
        {
            Vector3 currentAngularVelocity = _rb.angularVelocity;
            currentAngularVelocity = Vector3.Lerp(currentAngularVelocity, Vector3.zero, _floatData.stabilizerForce * Time.deltaTime);
            _rb.angularVelocity = currentAngularVelocity;
        }

        // Лёгкое демпфирование, когда совсем нет ввода и скорость мала
        if (_thrustDirection.magnitude < 0.1f && _rb.linearVelocity.magnitude < 0.5f)
        {
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, _floatData.stabilizerForce * Time.deltaTime);
        }
    }

    private void UpdateJetpackEffects()
    {
        if (jetpackParticles != null)
        {
            var emission = jetpackParticles.emission;
            bool shouldEmit = _thrustDirection.magnitude > 0.1f;
            emission.enabled = shouldEmit;

            if (shouldEmit)
            {
                var main = jetpackParticles.main;
                main.startSpeed = _isBoosting ? 8f : 4f;
                main.startLifetime = _isBoosting ? 1f : 0.5f;
            }
        }

        if (jetpackAudio != null)
        {
            if (_thrustDirection.magnitude > 0.1f)
            {
                if (!jetpackAudio.isPlaying) jetpackAudio.Play();

                jetpackAudio.volume = Mathf.Lerp(jetpackAudio.volume, _isBoosting ? 1f : 0.6f, 5f * Time.deltaTime);
                jetpackAudio.pitch  = Mathf.Lerp(jetpackAudio.pitch,  _isBoosting ? 1.3f : 1f,  5f * Time.deltaTime);
            }
            else
            {
                jetpackAudio.volume = Mathf.Lerp(jetpackAudio.volume, 0f, 5f * Time.deltaTime);
                if (jetpackAudio.volume < 0.1f) jetpackAudio.Stop();
            }
        }
    }
}
