using System;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

public class Meteor : MonoBehaviour
{
    private LayerData _layerData;
    [SerializeField] private Mesh[] meteorMeshes;
    [SerializeField] private Material[] meteorMaterials;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
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
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meteorMeshes != null && meteorMeshes.Length > 0)
        {
            meshFilter.mesh = meteorMeshes[Random.Range(0, meteorMeshes.Length)];
        }
        if (meteorMaterials != null && meteorMaterials.Length > 0)
        {
            meshRenderer.material = meteorMaterials[Random.Range(0, meteorMaterials.Length)];
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
