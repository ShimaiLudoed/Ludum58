using System;
using UnityEngine;
using Zenject;

public class Meteor : MonoBehaviour
{
    private LayerData _layerData; 
    private void OnCollisionEnter(Collision other)
    {
        if(LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
        {
            Destroy(gameObject);
        }
    }

    public class MeteorFactory : PlaceholderFactory<LayerData,PlayerController, Meteor>
    { }
}
