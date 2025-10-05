using System;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

public class Meteor : MonoBehaviour
{
    private LayerData _layerData;
    [SerializeField] private Mesh[] meteorMeshes;
    private MeshFilter meshFilter;

    [Inject]
    public void Construct(LayerData layerData)
    {
        _layerData = layerData;
    }

    private void Start()
    {
        SetupRandomAppearance();
    }

    void SetupRandomAppearance()
    {
        meshFilter = GetComponent<MeshFilter>();
        
        if (meteorMeshes != null && meteorMeshes.Length > 0)
        {
            meshFilter.mesh = meteorMeshes[Random.Range(0, meteorMeshes.Length)];
        }
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
