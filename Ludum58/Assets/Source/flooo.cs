using UnityEngine;

public class SideViewCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float zOffset = -10f;
    
    void Start()
    {
        transform.position = new Vector3(0, player.position.y, player.position.z + zOffset);
    }
    
    void LateUpdate()
    {
        FollowPlayerZAxis();
    }
    
    void FollowPlayerZAxis()
    {
        float targetZ = player.position.z + zOffset;
        float currentZ = transform.position.z;
        
        float newZ = Mathf.Lerp(currentZ, targetZ, followSpeed * Time.deltaTime);
        
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
    }
}