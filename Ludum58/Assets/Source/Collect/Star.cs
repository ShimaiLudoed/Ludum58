using System;
using System.Collections;
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
    
    [Header("Анимация сбора")]
    [SerializeField] private float collectAnimationTime = 0.5f;
    
    private Vector3 originalScale;
    private bool isCollecting = false;

    [Inject]
    public void Construct(LayerData layerData, PlayerController playerController, ISound sound, Score score)
    {
        _layerData = layerData;
        _playerController = playerController;
        _score = score;
        _sound = sound;
    }

    private void Start()
    {
        originalScale = transform.localScale;
        SetupRandomRotation();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(LayerMaskCheck.ContainsLayer(_layerData.player, other.gameObject.layer))
        {
            TakeStar();
        }
    }

    private void TakeStar()
    {
        if (isCollecting) return;
        
        isCollecting = true;
        StartCoroutine(CollectAnimation());
        _sound.PlayTakeStar();
    }

    private IEnumerator CollectAnimation()
    {
        float timer = 0f;
        
        while (timer < collectAnimationTime)
        {
            timer += Time.deltaTime;
            float progress = timer / collectAnimationTime;
            
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
            
            float scaleProgress = EaseInCubic(progress);
            transform.localScale = originalScale * (1f - scaleProgress);
            
            yield return null;
        }
        
        CompleteCollection();
    }

    private void CompleteCollection()
    {
        _score?.AddScore();
        Destroy(gameObject);
    }
    
    float EaseInCubic(float x)
    {
        return x * x * x;
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
    
    public class StarFactory : PlaceholderFactory<LayerData, PlayerController, Star>
    { }
}