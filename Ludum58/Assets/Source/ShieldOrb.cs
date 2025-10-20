using UnityEngine;
using Zenject;

public class ShieldOrb : MonoBehaviour
{
    [Header("Settings")]
    public float rotationSpeed = 60f;
    public float shieldDuration = 5f;   // длительность щита в секундах

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        // ищем компонент PlayerStats на игроке или его родителе
        var stats = other.GetComponentInParent<PlayerStats>();
        if (!stats) return;

        stats.ActivateShield(shieldDuration);
        Destroy(gameObject);
    }

    // Zenject фабрика без параметров
    public class ShieldOrbFactory : PlaceholderFactory<ShieldOrb> { }
}
