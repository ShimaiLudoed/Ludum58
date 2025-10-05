using System;
using UnityEngine;
using Zenject;

public class Meteor : MonoBehaviour
{
    private LayerData _layerData;

    [Inject]
    public void Construct(LayerData layerData)
    {
        _layerData = layerData;
    }

    private void OnTriggerEnter (Collider other)
    {
        if(LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
        {
            Destroy(gameObject);
        }
    }

    public class MeteorFactory : PlaceholderFactory<LayerData, Meteor>
    { }
}
