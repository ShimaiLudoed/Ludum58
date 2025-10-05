using System.Collections.Generic;
using UnityEngine;

public class TripleTunnelSpawner : MonoBehaviour
{
    [Header("Игрок (может стоять на месте)")]
    public Transform player;

    [Header("Параметры тоннелей")]
    public Transform tunnelCenter;
    public float innerRadius = 3f;   // первый тоннель — пустой
    public float midRadius = 6f;     // второй — спавн 1
    public float outerRadius = 9f;   // третий — спавн 2
    public float tunnelLength = 20f;

    // ==== НАСТРОЙКИ 2-ГО ТОННЕЛЯ ====
    [Header("Тоннель №2 (средний)")]
    public GameObject[] midTunnelPrefabs;
    [Tooltip("Сколько объектов создаётся за один спавн-пакет")]
    public int midObjectsPerBatch = 10;
    [Tooltip("Сколько пакетов создаётся в секунду (скорость спавна)")]
    public float midSpawnSpeed = 1.0f; // 1 пакет в секунду
    [Tooltip("Скорость движения объектов тоннеля")]
    public float midMoveSpeed = 20f;
    [Tooltip("Z-координата, на которой объекты удаляются")]
    public float midDespawnZ = -30f;

    // ==== НАСТРОЙКИ 3-ГО ТОННЕЛЯ ====
    [Header("Тоннель №3 (внешний)")]
    public GameObject[] outerTunnelPrefabs;
    public int outerObjectsPerBatch = 8;
    public float outerSpawnSpeed = 0.8f; // 0.8 пакета в секунду
    public float outerMoveSpeed = 15f;
    public float outerDespawnZ = -30f;

    [Header("Цвета гизмосов")]
    public Color innerColor = Color.green;
    public Color midColor = Color.yellow;
    public Color outerColor = Color.red;

    // ====== приватные данные ======
    private List<GameObject> midObjects = new List<GameObject>();
    private List<GameObject> outerObjects = new List<GameObject>();
    private float midTimer;
    private float outerTimer;
    private float zSpawnOffset;

    void Start()
    {
        midTimer = 1f / Mathf.Max(midSpawnSpeed, 0.01f);
        outerTimer = 1f / Mathf.Max(outerSpawnSpeed, 0.01f);
    }

    void Update()
    {
        midTimer -= Time.deltaTime;
        outerTimer -= Time.deltaTime;

        if (midTimer <= 0f)
        {
            SpawnBatch(midTunnelPrefabs, innerRadius, midRadius, midObjectsPerBatch, midObjects);
            midTimer = 1f / Mathf.Max(midSpawnSpeed, 0.01f);
        }

        if (outerTimer <= 0f)
        {
            SpawnBatch(outerTunnelPrefabs, midRadius, outerRadius, outerObjectsPerBatch, outerObjects);
            outerTimer = 1f / Mathf.Max(outerSpawnSpeed, 0.01f);
        }

        MoveAndCleanup(midObjects, midMoveSpeed, midDespawnZ);
        MoveAndCleanup(outerObjects, outerMoveSpeed, outerDespawnZ);

        zSpawnOffset += Mathf.Max(midMoveSpeed, outerMoveSpeed) * Time.deltaTime;
    }

    void SpawnBatch(GameObject[] prefabs, float minRadius, float maxRadius, int count, List<GameObject> storage)
    {
        if (prefabs == null || prefabs.Length == 0 || tunnelCenter == null) return;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            float radius = Random.Range(minRadius, maxRadius);
            float zOffset = Random.Range(40f, 60f) + zSpawnOffset;

            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                player != null ? player.position.z + zOffset : zOffset
            );

            Vector3 worldPos = tunnelCenter.position + tunnelCenter.rotation * localPos;
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);
            storage.Add(obj);
        }
    }

    void MoveAndCleanup(List<GameObject> list, float speed, float despawnZ)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            GameObject obj = list[i];
            if (obj == null)
            {
                list.RemoveAt(i);
                continue;
            }

            obj.transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

            if (player != null && obj.transform.position.z < player.position.z + despawnZ)
            {
                Destroy(obj);
                list.RemoveAt(i);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (tunnelCenter == null) return;

        Gizmos.color = innerColor;
        DrawTunnelGizmo(tunnelCenter.position, tunnelCenter.rotation, innerRadius, tunnelLength);

        Gizmos.color = midColor;
        DrawTunnelGizmo(tunnelCenter.position, tunnelCenter.rotation, midRadius, tunnelLength);

        Gizmos.color = outerColor;
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
