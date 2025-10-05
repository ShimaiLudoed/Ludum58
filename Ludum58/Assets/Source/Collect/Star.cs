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
  
  [Inject]
  public void Construct(LayerData layerData, PlayerController playerController, Score score)
  {
    _layerData = layerData;
    _playerController = playerController;
    _score = score;
   // _sound = sound;
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
  public class StarFactory : PlaceholderFactory<LayerData,PlayerController, Star>
  { }
}
