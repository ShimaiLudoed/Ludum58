using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class DamageObject : MonoBehaviour
{
    public PlayerStats stats;
    public int damage = 10;
    private LayerData _layerData;

    [Inject]
    public void Construct(LayerData layerData)
    {
        _layerData = layerData;
    }
    void OnTriggerEnter(Collider other)
    {
        if (stats != null && LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            stats.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
