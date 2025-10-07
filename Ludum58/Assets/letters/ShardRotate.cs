using UnityEngine;

public class SelfRotator : MonoBehaviour
{
    [Header("Rotation Speed (deg/sec)")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);

    [Header("Settings")]
    [SerializeField] private bool useUnscaledTime = false;

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(rotationSpeed * dt, Space.Self);
    }
}
