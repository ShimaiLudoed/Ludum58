using UnityEngine;

public class Bounce : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    
    void FixedUpdate()
    {
        KeepInBounds();
    }
    
    void KeepInBounds()
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
        
        // Жестко телепортируем игрока обратно если он выходит за границы
        if (viewportPos.x < 0.15f) 
        {
            Vector3 newViewport = new Vector3(0.15f, viewportPos.y, viewportPos.z);
            Vector3 worldPos = mainCamera.ViewportToWorldPoint(newViewport);
            transform.position = new Vector3(worldPos.x, transform.position.y, transform.position.z);
            
            // Обнуляем скорость в направлении стены
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, rb.linearVelocity.z);
        }
        
        if (viewportPos.x > 0.85f) 
        {
            Vector3 newViewport = new Vector3(0.85f, viewportPos.y, viewportPos.z);
            Vector3 worldPos = mainCamera.ViewportToWorldPoint(newViewport);
            transform.position = new Vector3(worldPos.x, transform.position.y, transform.position.z);
            
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, rb.linearVelocity.z);
        }
        
        if (viewportPos.y < 0.15f) 
        {
            Vector3 newViewport = new Vector3(viewportPos.x, 0.15f, viewportPos.z);
            Vector3 worldPos = mainCamera.ViewportToWorldPoint(newViewport);
            transform.position = new Vector3(transform.position.x, worldPos.y, transform.position.z);
            
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }
        
        if (viewportPos.y > 0.85f) 
        {
            Vector3 newViewport = new Vector3(viewportPos.x, 0.85f, viewportPos.z);
            Vector3 worldPos = mainCamera.ViewportToWorldPoint(newViewport);
            transform.position = new Vector3(transform.position.x, worldPos.y, transform.position.z);
            
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }
    }
}