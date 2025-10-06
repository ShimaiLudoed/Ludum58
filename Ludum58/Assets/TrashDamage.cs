using UnityEngine;
using Zenject;

public class TrashDamage : MonoBehaviour
{
    [SerializeField] private int damage;
    private LayerData _layerData;
    private PlayerStats _playerStats;
    [SerializeField] private Mesh[] TrashMeshes;
    private MeshFilter meshFilter;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 50f;

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
        SetupRandomRotation();
    }
      void SetupRandomRotation()
  {
    rotationSpeed = Random.Range(30f, 80f);
        
    rotationAxis = new Vector3(
      Random.Range(-1f, 1f),
      Random.Range(-1f, 1f), 
      Random.Range(-1f, 1f)
    ).normalized;
    
    transform.rotation = Random.rotation;
  }

  private void Update()
  {
    transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
  }

    void SetupRandomAppearance()
    {
        if (TrashMeshes != null && TrashMeshes.Length > 0)
        {
            meshFilter.mesh = TrashMeshes[Random.Range(0, TrashMeshes.Length)];
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
