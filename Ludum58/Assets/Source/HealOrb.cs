using System.Collections.Generic;
using UnityEngine;
using Zenject;
public class HealOrb : MonoBehaviour
{
    public float rotationSpeed = 60f;
    public int healAmount = 25;

    void Update() => transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.Heal(healAmount);
        Destroy(gameObject);
    }

    // БЕЗ параметров
    public class HealOrbFactory : Zenject.PlaceholderFactory<HealOrb> { }
}
