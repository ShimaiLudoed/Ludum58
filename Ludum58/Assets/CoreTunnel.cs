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

    [Header("Вероятности спавна")]
    [Range(0f, 1f)]
    [SerializeField] private float starChance = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float meteorChance = 0.4f;
    [Range(0f, 1f)]
    [SerializeField] private float spaceJunkChance = 0.2f;

    [Header("Визуализация тоннеля")]
    [SerializeField] private Color tunnelColor = Color.cyan;
    [SerializeField] private Color starZoneColor = Color.yellow;
    [SerializeField] private Color dangerZoneColor = Color.red;

    private List<GameObject> spawned = new List<GameObject>();
    private float spawnTimer;
    private float zSpawnOffset;

    // Zenject зависимости
    private PlayerController _playerController;
    private Meteor.MeteorFactory _meteorFactory;
    private Star.StarFactory _starFactory;
    private TrashDamage.TrashFactory _spaceTrashFactory;
    private LayerData _layerData;
    private PlayerStats _playerStats;

    [Inject]
    public void Construct(
        PlayerController playerController,
        Star.StarFactory starFactory,
        LayerData layerData,
        Meteor.MeteorFactory meteorFactory,
        PlayerStats playerStats,
        TrashDamage.TrashFactory trashFactory)
    {
        _playerController = playerController;
        _starFactory = starFactory;
        _layerData = layerData;
        _meteorFactory = meteorFactory;
        _playerStats = playerStats;
        _spaceTrashFactory = trashFactory;
    }
    
    void Start()
    {
        spawnTimer = 1f / Mathf.Max(spawnSpeed, 0.01f);
        
        if (_playerController == null)
        {
            Debug.LogError("PlayerController not injected!");
        }
    }

    void Update()
    {
        if (_playerController == null) return;
        
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnBatch();
            spawnTimer = 1f / Mathf.Max(spawnSpeed, 0.01f);
        }

        MoveObjects();
        CleanupObjects();
        zSpawnOffset += moveSpeed * Time.deltaTime;
    }

    void SpawnBatch()
    {
        for (int i = 0; i < objectsPerBatch; i++)
        {
            float randomValue = Random.value;
            float totalChance = starChance + meteorChance + spaceJunkChance;
            
            float normalizedStarChance = starChance / totalChance;
            float normalizedMeteorChance = meteorChance / totalChance;
            
            bool spawnStar = randomValue < normalizedStarChance;
            bool spawnMeteor = randomValue >= normalizedStarChance && randomValue < normalizedStarChance + normalizedMeteorChance;
            bool spawnSpaceJunk = randomValue >= normalizedStarChance + normalizedMeteorChance;
            
            if (!spawnStar && !spawnMeteor && !spawnSpaceJunk) continue;

            float angle = Random.Range(0f, Mathf.PI * 2);
            float zOffset = Random.Range(40f, 60f) + zSpawnOffset;

            Vector3 localPos;
            Vector3 worldPos;

            if (spawnStar)
            {
                // Звезды появляются в своем круге
                float radius = Random.Range(starMinRadius, starMaxRadius);
                localPos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    _playerController.transform.position.z + zOffset
                );
                worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;

                Star star = _starFactory.Create(_layerData, _playerController);
                star.transform.position = worldPos;
                star.transform.rotation = Quaternion.identity;
                spawned.Add(star.gameObject);
            }
            else 
            {
                // Метеориты и мусор появляются в своем круге
                float radius = Random.Range(dangerMinRadius, dangerMaxRadius);
                localPos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    _playerController.transform.position.z + zOffset
                );
                worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;

                if (spawnMeteor)
                {
                    Meteor meteor = _meteorFactory.Create(_layerData, _playerStats);
                    meteor.transform.position = worldPos;
                    meteor.transform.rotation = Quaternion.identity;
                    spawned.Add(meteor.gameObject);
                }
                else if (spawnSpaceJunk)
                {
                    TrashDamage spaceJunk = _spaceTrashFactory.Create(_layerData, _playerStats);
                    spaceJunk.transform.position = worldPos;
                    spaceJunk.transform.rotation = Quaternion.identity;
                    spawned.Add(spaceJunk.gameObject);
                }
            }
        }
    }

    void MoveObjects()
    {
        foreach (var obj in spawned)
        {
            if (obj != null)
                obj.transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    void CleanupObjects()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawned[i];
            if (obj == null)
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

    void OnDrawGizmos()
    {
        if (tunnelCenter == null) return;
        
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
}