using System;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

public class Meteor : MonoBehaviour
{
  [SerializeField] private int damage;
  [SerializeField] private float rotationSpeed = 50f;
  [SerializeField] private Vector3 rotationAxis = Vector3.up;
    
  private LayerData _layerData;
  private PlayerStats _playerStats;
  [SerializeField] private Mesh[] meteorMeshes;
  private MeshFilter meshFilter;
  private ISound _sound;

  [Inject]
  public void Construct(LayerData layerData, PlayerStats playerStats, ISound sound)
  {
    _layerData = layerData;
    _playerStats = playerStats;
    _sound = sound;
  }

  private void Start()
  {
    meshFilter = GetComponent<MeshFilter>();
    SetupRandomAppearance();
    SetupRandomRotation();
  }

  void SetupRandomAppearance()
  {
    if (meteorMeshes != null && meteorMeshes.Length > 0)
    {
      meshFilter.mesh = meteorMeshes[Random.Range(0, meteorMeshes.Length)];
    }
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

  private void OnTriggerEnter(Collider other)
  {
    if(LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
    {
      _playerStats.TakeDamage(damage);
      _sound.PlayTakeDamage();
      Destroy(gameObject);
    }
  }

  public class MeteorFactory : PlaceholderFactory<LayerData, PlayerStats, Meteor>
  { }
}