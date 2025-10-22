using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SingleTunnelSpawner : MonoBehaviour
{
    [Header("Параметры тоннеля")]
    [SerializeField] private Transform tunnelCenter;
    [SerializeField] private float innerRadius = 3f;
    [SerializeField] private float outerRadius = 8f;
    [SerializeField] private float tunnelLength = 20f;

    [Header("Радиусы для разных объектов")]
    [SerializeField] private float starMinRadius = 4f;
    [SerializeField] private float starMaxRadius = 6f;
    [SerializeField] private float dangerMinRadius = 3f;
    [SerializeField] private float dangerMaxRadius = 8f;

    [Header("Параметры спавна")]
    [SerializeField] private int objectsPerBatch = 10;
    [SerializeField] private float spawnSpeed = 1.0f;
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float despawnZ = -30f;

    [Header("Ускорение игры")]
    [SerializeField] private float accelerationRate = 0.1f;

    [Header("Общая скорость игры")]
    [Tooltip("Множитель, влияющий на общую скорость движения всех объектов")]
    [SerializeField] private float gameSpeedMultiplier = 1f; // Новый параметр
    public float GameSpeedMultiplier
    {
        get => gameSpeedMultiplier;
        set => gameSpeedMultiplier = value;
    }

    [Header("Вероятности спавна")]
    [Range(0f, 1f)] [SerializeField] private float starChance = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float meteorChance = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float spaceJunkChance = 0.2f;

    [Header("Хилки")]
    [SerializeField] private float healSpawnInterval = 30f;
    [Range(0f, 1f)] [SerializeField] private float healChance = 1f;
    private float _healTimer;

    [Header("Щиты")]
    [SerializeField] private float shieldSpawnInterval = 45f;
    [Range(0f, 1f)] [SerializeField] private float shieldChance = 0.7f;
    private float _shieldTimer;

    [Header("Замедление времени (SlowOrb)")]
    [SerializeField] private float slowSpawnInterval = 60f;
    [Range(0f, 1f)] [SerializeField] private float slowChance = 0.8f;
    private float _slowTimer;

    [Header("Исчезание у камеры")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private float fadeStartDistance = 6f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float killDistance = 1.5f;
    [SerializeField] private bool disableCollidersOnFade = true;

    [Header("Визуализация тоннеля")]
    [SerializeField] private Color tunnelColor = Color.cyan;
    [SerializeField] private Color starZoneColor = Color.yellow;
    [SerializeField] private Color dangerZoneColor = Color.red;

    private readonly List<GameObject> spawned = new();
    private float spawnTimer;
    private float zSpawnOffset;
    private float currentMoveSpeed;

    // === публичные свойства для управления скоростью ===
    public float MoveSpeed { get => currentMoveSpeed; set => currentMoveSpeed = value; }
    public float BaseMoveSpeed => moveSpeed;

    // Zenject зависимости
    private PlayerController _playerController;
    private Meteor.MeteorFactory _meteorFactory;
    private Star.StarFactory _starFactory;
    private TrashDamage.TrashFactory _spaceTrashFactory;
    private HealOrb.HealOrbFactory _healFactory;
    private ShieldOrb.ShieldOrbFactory _shieldFactory;
    private SlowOrb.SlowOrbFactory _slowFactory;
    private LayerData _layerData;
    private PlayerStats _playerStats;

    [Inject]
    public void Construct(
        PlayerController playerController,
        Star.StarFactory starFactory,
        LayerData layerData,
        Meteor.MeteorFactory meteorFactory,
        PlayerStats playerStats,
        TrashDamage.TrashFactory trashFactory,
        HealOrb.HealOrbFactory healFactory,
        ShieldOrb.ShieldOrbFactory shieldFactory,
        SlowOrb.SlowOrbFactory slowFactory)
    {
        _playerController = playerController;
        _starFactory = starFactory;
        _layerData = layerData;
        _meteorFactory = meteorFactory;
        _playerStats = playerStats;
        _spaceTrashFactory = trashFactory;
        _healFactory = healFactory;
        _shieldFactory = shieldFactory;
        _slowFactory = slowFactory;
    }

    void Start()
    {
        if (!gameplayCamera) gameplayCamera = Camera.main;

        spawnTimer = 1f / Mathf.Max(spawnSpeed, 0.01f);
        currentMoveSpeed = moveSpeed;
        _healTimer = Mathf.Max(0.01f, healSpawnInterval);
        _shieldTimer = Mathf.Max(0.01f, shieldSpawnInterval);
        _slowTimer = Mathf.Max(0.01f, slowSpawnInterval);
    }

    void Update()
    {
        if (_playerController == null) return;

        currentMoveSpeed += accelerationRate * Time.deltaTime;

        spawnTimer -= Time.deltaTime * gameSpeedMultiplier;
        if (spawnTimer <= 0f)
        {
            SpawnBatch();
            spawnTimer = 1f / Mathf.Max(spawnSpeed, 0.01f);
        }

        _healTimer -= Time.deltaTime * gameSpeedMultiplier;
        if (_healTimer <= 0f)
        {
            TrySpawnHealOrb();
            _healTimer = Mathf.Max(0.01f, healSpawnInterval);
        }

        _shieldTimer -= Time.deltaTime * gameSpeedMultiplier;
        if (_shieldTimer <= 0f)
        {
            TrySpawnShieldOrb();
            _shieldTimer = Mathf.Max(0.01f, shieldSpawnInterval);
        }

        _slowTimer -= Time.deltaTime * gameSpeedMultiplier;
        if (_slowTimer <= 0f)
        {
            TrySpawnSlowOrb();
            _slowTimer = Mathf.Max(0.01f, slowSpawnInterval);
        }

        MoveObjects();
        CleanupObjects();
        zSpawnOffset += currentMoveSpeed * Time.deltaTime * gameSpeedMultiplier;
    }

    void SpawnBatch()
    {
        for (int i = 0; i < objectsPerBatch; i++)
        {
            float totalChance = starChance + meteorChance + spaceJunkChance;
            if (totalChance <= 0f) continue;

            float r = Random.value;
            float pStar = starChance / totalChance;
            float pMeteor = meteorChance / totalChance;

            bool spawnStar = r < pStar;
            bool spawnMeteor = r >= pStar && r < pStar + pMeteor;
            bool spawnSpaceJunk = r >= pStar + pMeteor;

            float angle = Random.Range(0f, Mathf.PI * 2);
            float zOffset = Random.Range(40f, 60f) + zSpawnOffset;

            Vector3 localPos;
            Vector3 worldPos;

            if (spawnStar)
            {
                float radius = Random.Range(starMinRadius, starMaxRadius);
                localPos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, _playerController.transform.position.z + zOffset);
                worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;

                var star = _starFactory.Create(_layerData, _playerController);
                star.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
                ArmDisappear(star.gameObject);
                spawned.Add(star.gameObject);
            }
            else
            {
                float radius = Random.Range(dangerMinRadius, dangerMaxRadius);
                localPos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, _playerController.transform.position.z + zOffset);
                worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;

                if (spawnMeteor)
                {
                    var meteor = _meteorFactory.Create(_layerData, _playerStats);
                    meteor.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
                    ArmDisappear(meteor.gameObject);
                    spawned.Add(meteor.gameObject);
                }
                else if (spawnSpaceJunk)
                {
                    var trash = _spaceTrashFactory.Create(_layerData, _playerStats);
                    trash.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
                    ArmDisappear(trash.gameObject);
                    spawned.Add(trash.gameObject);
                }
            }
        }
    }

    void TrySpawnHealOrb()
    {
        if (Random.value > healChance) return;

        float angle = Random.Range(0f, Mathf.PI * 2);
        float radius = Random.Range(starMinRadius, starMaxRadius);
        float zOffset = Random.Range(40f, 60f) + zSpawnOffset;

        Vector3 localPos = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, _playerController.transform.position.z + zOffset);
        Vector3 worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;

        var orb = _healFactory.Create();
        orb.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
        ArmDisappear(orb.gameObject);
        spawned.Add(orb.gameObject);
    }

    void TrySpawnShieldOrb()
    {
        if (Random.value > shieldChance) return;

        float angle = Random.Range(0f, Mathf.PI * 2);
        float radius = Random.Range(starMinRadius, starMaxRadius);
        float zOffset = Random.Range(40f, 60f) + zSpawnOffset;

        Vector3 localPos = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, _playerController.transform.position.z + zOffset);
        Vector3 worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;

        var orb = _shieldFactory.Create();
        orb.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
        ArmDisappear(orb.gameObject);
        spawned.Add(orb.gameObject);
    }

    void TrySpawnSlowOrb()
    {
        if (Random.value > slowChance) return;

        float angle = Random.Range(0f, Mathf.PI * 2);
        float radius = Random.Range(starMinRadius, starMaxRadius);
        float zOffset = Random.Range(40f, 60f) + zSpawnOffset;

        Vector3 localPos = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, _playerController.transform.position.z + zOffset);
        Vector3 worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;

        var orb = _slowFactory.Create();
        orb.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
        ArmDisappear(orb.gameObject);
        spawned.Add(orb.gameObject);
    }

    void MoveObjects()
    {
        foreach (var obj in spawned)
        {
            if (obj != null)
                obj.transform.Translate(Vector3.back * currentMoveSpeed * Time.deltaTime * gameSpeedMultiplier, Space.World);
        }
    }

    void CleanupObjects()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawned[i];
            if (!obj)
            {
                spawned.RemoveAt(i);
                continue;
            }

            if (obj.transform.position.z < _playerController.transform.position.z + despawnZ)
            {
                Destroy(obj);
                spawned.RemoveAt(i);
            }
        }
    }

    void ArmDisappear(GameObject go)
    {
        if (!go) return;

        var d = go.GetComponent<DisappearNearCamera>();
        if (!d) d = go.AddComponent<DisappearNearCamera>();

        d.player = _playerController ? _playerController.transform : null;
        d.targetCamera = gameplayCamera ? gameplayCamera : Camera.main;
        d.fadeStartDistance = fadeStartDistance;
        d.fadeDuration = fadeDuration;
        d.killDistance = killDistance;
        d.disableCollidersOnFade = disableCollidersOnFade;
    }

    void OnDrawGizmos()
    {
        if (!tunnelCenter) return;
        Gizmos.color = tunnelColor;
        DrawTunnelGizmo(tunnelCenter.position, tunnelCenter.rotation, outerRadius, tunnelLength);
        Gizmos.color = starZoneColor;
        DrawCircleGizmo(tunnelCenter.position, tunnelCenter.rotation, starMinRadius);
        DrawCircleGizmo(tunnelCenter.position, tunnelCenter.rotation, starMaxRadius);
        Gizmos.color = dangerZoneColor;
        DrawCircleGizmo(tunnelCenter.position, tunnelCenter.rotation, dangerMinRadius);
        DrawCircleGizmo(tunnelCenter.position, tunnelCenter.rotation, dangerMaxRadius);
    }

    void DrawTunnelGizmo(Vector3 center, Quaternion rotation, float radius, float length)
    {
        const int segments = 36;
        Vector3[] circle = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            circle[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -length / 2);
        }

        for (int j = 0; j < segments; j++)
        {
            Vector3 p1 = center + rotation * circle[j];
            Vector3 p2 = center + rotation * circle[j + 1];
            Vector3 p3 = p1 + rotation * (Vector3.forward * length);
            Vector3 p4 = p2 + rotation * (Vector3.forward * length);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p1, p3);
        }
    }

    void DrawCircleGizmo(Vector3 center, Quaternion rotation, float radius)
    {
        const int segments = 36;
        Vector3[] circle = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            circle[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
        for (int j = 0; j < segments; j++)
        {
            Vector3 p1 = center + rotation * circle[j];
            Vector3 p2 = center + rotation * circle[j + 1];
            Gizmos.DrawLine(p1, p2);
        }
    }

    public float AccelerationRate
    {
        get => accelerationRate;
        set => accelerationRate = value;
    }
}
