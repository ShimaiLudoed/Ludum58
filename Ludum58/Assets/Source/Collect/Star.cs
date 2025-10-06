using System;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

public class Star : MonoBehaviour
{
  private LayerData _layerData; 
  private PlayerController _playerController;
  private Score _score;
  private ISound _sound;
  [SerializeField] private Vector3 rotationAxis = Vector3.up;
  [SerializeField] private float rotationSpeed = 50f;
  
  [Inject]
  public void Construct(LayerData layerData, PlayerController playerController, Score score)
  {
    _layerData = layerData;
    _playerController = playerController;
    _score = score;
   // _sound = sound;
  }
    private void Start()
  {
    SetupRandomRotation();
  }
  private void OnTriggerEnter(Collider other)
  {
    if(LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
    {
      _playerController.OnTakeStar += TakeStar;
      Debug.Log("Зашёл");
    }
  }
  private void OnDestroy()
  {
    if (_playerController != null)
    {
      _playerController.OnTakeStar -= TakeStar; 
    }
  }

  private void TakeStar()
  {
    Destroy(gameObject);
    Debug.Log("piy");
    _score.AddScore();
    
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
  public class StarFactory : PlaceholderFactory<LayerData,PlayerController, Star>
  { }
}
