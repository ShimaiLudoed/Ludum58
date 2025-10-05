using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageObject : MonoBehaviour
{
    public PlayerStats stats;
    public int damage = 10;

    void OnTriggerEnter(Collider other)
    {
        if (stats != null && other.CompareTag("Player"))
        {
            stats.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
