using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class Meteor : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    private PlayerStats _playerStats;
    private LayerData _layerData;

    [Inject]
    public void Construct(LayerData layerData, PlayerStats playerStats)
    {
        _playerStats = playerStats;
        _layerData = layerData;
    }

    private void OnTriggerEnter (Collider other)
    {
        if(LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
        {
            _playerStats.TakeDamage(20);
            Destroy(gameObject);
        }
    }

    public class MeteorFactory : PlaceholderFactory<LayerData, PlayerStats, Meteor>
    { }
}
