using UnityEngine;

public class FollowForwardProxy : MonoBehaviour
{
    public Transform target;                 // игрок
    public enum AxisMode { WorldZ, WorldX, WorldY, TargetForward }
    public AxisMode axis = AxisMode.WorldZ;  // «вперёд»
    public float smooth = 8f;                // плавность

    Vector3 startProxyPos;       // фиксируем X/Y (или всё кроме оси)
    Vector3 startTargetPos;
    Vector3 axisDir;

    void Start()
    {
        startProxyPos  = transform.position;
        startTargetPos = target ? target.position : startProxyPos;
        axisDir = GetAxisDir();
    }

    void LateUpdate()
    {
        if (!target) return;

        // пересчитываем ось, если выбрано TargetForward
        if (axis == AxisMode.TargetForward)
            axisDir = target.forward.normalized;

        // сколько таргет сместился вдоль оси с момента старта
        float t = Vector3.Dot(target.position - startTargetPos, axisDir);

        // новая позиция прокси = базовая + смещение только по выбранной оси
        Vector3 goal = startProxyPos + axisDir * t;

        // плавно
        transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }

    Vector3 GetAxisDir()
    {
        return axis switch
        {
            AxisMode.WorldX => Vector3.right,
            AxisMode.WorldY => Vector3.up,
            AxisMode.WorldZ => Vector3.forward,
            AxisMode.TargetForward => (target ? target.forward.normalized : Vector3.forward),
            _ => Vector3.forward
        };
    }
}
