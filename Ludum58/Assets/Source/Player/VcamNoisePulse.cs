using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class VCamNoisePulse : MonoBehaviour
{
    [SerializeField] CinemachineCamera vcam;
    [SerializeField] float duration = 0.5f;

    CinemachineBasicMultiChannelPerlin noise;
    bool busy;

    void Awake()
    {
        if (!vcam) vcam = FindObjectOfType<CinemachineCamera>();
        noise = vcam ? vcam.GetComponent<CinemachineBasicMultiChannelPerlin>() : null;
        if (noise) noise.enabled = false; // по умолчанию выкл
    }

    public void Pulse(float dur = -1f)
    {
        if (!noise || busy) return;
        StartCoroutine(DoPulse(dur > 0f ? dur : duration));
    }

    IEnumerator DoPulse(float dur)
    {
        busy = true;
        noise.enabled = true;
        yield return new WaitForSeconds(dur);
        noise.enabled = false;
        busy = false;
    }
}