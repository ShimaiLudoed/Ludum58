using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    public PlayerStats stats;
    public int points = 5;

    void OnTriggerEnter(Collider other)
    {
        if (stats != null && other.CompareTag("Player"))
        {
            stats.AddScore(points);
            Destroy(gameObject);
        }
    }
}
