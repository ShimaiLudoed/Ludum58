using UnityEngine;

public class ForwardCameraController : MonoBehaviour
{
    [Header("Камера и движение")]
    public float cameraSpeed = 10f;         // скорость движения камеры вперёд
    public Vector3 cameraDirection = Vector3.forward; // направление движения

    [Header("Игрок и ограничители")]
    public Transform player;
    public float boundaryX = 5f;            // константа граница по X от центра камеры
    public float boundaryY = 3f;            // по Y
    public float bounceStrength = 5f;       // сила отталкивания
    public float bounceDamping = 2f;        // демпфирование

    private Rigidbody playerRb;

    void Start()
    {
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        // камера движется равномерно вперёд
        transform.position += cameraDirection.normalized * cameraSpeed * Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (player == null || playerRb == null) return;

        // позиция игрока относительно камеры
        Vector3 local = transform.InverseTransformPoint(player.position);

        float dx = local.x;
        float dy = local.y;

        Vector3 force = Vector3.zero;

        if (dx > boundaryX)
            force += -transform.right * (dx - boundaryX) * bounceStrength - playerRb.linearVelocity * bounceDamping;
        else if (dx < -boundaryX)
            force += transform.right * (-boundaryX - dx) * bounceStrength - playerRb.linearVelocity * bounceDamping;

        if (dy > boundaryY)
            force += -transform.up * (dy - boundaryY) * bounceStrength - playerRb.linearVelocity * bounceDamping;
        else if (dy < -boundaryY)
            force += transform.up * (-boundaryY - dy) * bounceStrength - playerRb.linearVelocity * bounceDamping;

        if (force.sqrMagnitude > 0f)
            playerRb.AddForce(force, ForceMode.Acceleration);
    }
}
