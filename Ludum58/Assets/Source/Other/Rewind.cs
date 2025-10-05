using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class CinemachineSwitcher : MonoBehaviour
{
    [Header("Виртуальные камеры")]
    public CinemachineCamera camA;
    public CinemachineCamera camB;

    [Header("Кнопки переключения")]
    public Button buttonToB;
    public Button buttonToA;

    [Header("Приоритет")]
    public int priorityA = 10;
    public int priorityB = 20;

    void Awake()
    {
        if (buttonToB != null)
            buttonToB.onClick.AddListener(() => SwitchTo(camB, camA));

        if (buttonToA != null)
            buttonToA.onClick.AddListener(() => SwitchTo(camA, camB));
    }

    void SwitchTo(CinemachineCamera toCam, CinemachineCamera fromCam)
    {
        if (toCam == null || fromCam == null) return;

        // Дать большей приоритет новой
        toCam.Priority = priorityB;
        fromCam.Priority = priorityA;
    }
}
