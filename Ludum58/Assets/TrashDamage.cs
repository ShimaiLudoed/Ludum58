using UnityEngine;
using Zenject;

public class TrashDamage : MonoBehaviour
{
    [SerializeField] private int damage;
    private LayerData _layerData;
    private PlayerStats _playerStats;
    [SerializeField] private Mesh[] meteorMeshes;
    private MeshFilter meshFilter;

    [Inject]
    public void Construct(LayerData layerData, PlayerStats playerStats)
    {
        _layerData = layerData;
        _playerStats = playerStats;
    }

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        SetupRandomAppearance();
    }

    void SetupRandomAppearance()
    {
        if (meteorMeshes != null && meteorMeshes.Length > 0)
        {
            meshFilter.mesh = meteorMeshes[Random.Range(0, meteorMeshes.Length)];
        }
    }

    private void OnTriggerEnter (Collider other)
    {
        if(LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
        {
            _playerStats.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    public class TrashFactory : PlaceholderFactory<LayerData, PlayerStats, TrashDamage>
    { }
}
