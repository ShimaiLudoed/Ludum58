using System.Collections.Generic;
using UnityEngine;

public class SingleTunnelSpawner : MonoBehaviour
{
    [Header("Игрок")]
    public Transform player;

    [Header("Параметры тоннеля")]
    public Transform tunnelCenter;
    public float innerRadius = 3f;
    public float outerRadius = 8f;
    public float tunnelLength = 20f;

    [Header("Параметры спавна")]
    public int objectsPerBatch = 10;
    public float spawnSpeed = 1.0f;       // пакетов в секунду
    public float moveSpeed = 20f;
    public float despawnZ = -30f;

    [Header("Префабы")]
    public GameObject[] damagePrefabs;     
    public GameObject[] collectiblePrefabs;

    [Range(0f, 1f)]
    public float collectibleChance = 0.3f;

    [Header("Визуализация тоннеля")]
    public Color tunnelColor = Color.cyan;

    private List<GameObject> spawned = new List<GameObject>();
    private float spawnTimer;
    private float zSpawnOffset;

    void Start()
    {
        spawnTimer = 1f / Mathf.Max(spawnSpeed, 0.01f);
    }

    void Update()
    {
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
            bool spawnCollectible = Random.value < collectibleChance;
            GameObject[] pool = spawnCollectible ? collectiblePrefabs : damagePrefabs;
            if (pool == null || pool.Length == 0) continue;

            GameObject prefab = pool[Random.Range(0, pool.Length)];

            float angle = Random.Range(0f, Mathf.PI * 2);
            float radius = Random.Range(innerRadius, outerRadius);
            float zOffset = Random.Range(40f, 60f) + zSpawnOffset;

            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                player != null ? player.position.z + zOffset : zOffset
            );

            Vector3 worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;
            GameObject obj = Instantiate(prefab, worldPos, prefab.transform.rotation);
            spawned.Add(obj);
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

            if (player != null && obj.transform.position.z < player.position.z + despawnZ)
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
}
