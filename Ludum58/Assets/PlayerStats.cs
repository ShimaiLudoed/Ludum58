using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;
using Zenject;

public class PlayerStats : MonoBehaviour
{
    private Score _score;
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private GameObject replayPanel;
    [Header("Игровые параметры")] public int maxHP = 3;
    public int currentHP;

    [Header("Неуязвимость")] [SerializeField]
    private float invulnerabilityTime;

    private bool isInvulnerable = false;

    [Header("Спрайты жизней")] public SpriteRenderer[] heartSprites;

    [Header("Cinemachine Shake")] [SerializeField]
    CinemachineBrain cinemachineBrain;

    [SerializeField, Min(0f)] float shakeDuration = 0.5f;
    [SerializeField, Min(0f)] float shakeAmplitude = 2.0f;
    [SerializeField, Min(0f)] float shakeFrequency = 2.0f;

    [Header("Флэш сердец при уроне")] [SerializeField]
    Color heartsFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    [SerializeField, Min(0f)] float heartsFlashDuration = 0.5f;

    Coroutine flashCo;

    [Inject]
    public void Construct(Score score)
    {
        _score = score;
    }

void Start()
    {
        currentHP = maxHP;
        UpdateHeartsUI();
        if (!cinemachineBrain) cinemachineBrain = FindObjectOfType<CinemachineBrain>();
    }

    public void TakeDamage(int amount)
    {
        if (isInvulnerable) return;
        int previousHP = currentHP;
        currentHP -= amount;

        if (currentHP <= 0)
        {
            currentHP = 0;
            particleSystem.Play();
            replayPanel.gameObject.SetActive(true);
            _score.FinishScore();
            Destroy(gameObject);
        }
        
        StartCoroutine(CinemachineShake(shakeDuration, shakeAmplitude, shakeFrequency));
        StartCoroutine(AnimateHeartLoss(previousHP));
        StartCoroutine(InvulnerabilityRoutine());
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }

    IEnumerator AnimateHeartLoss(int previousHP)
    {
        for (int i = currentHP; i < previousHP && i < heartSprites.Length; i++)
        {
            if (heartSprites[i] != null)
            {
                if (flashCo != null) StopCoroutine(flashCo);
                flashCo = StartCoroutine(FlashRemainingHearts(heartsFlashDuration));

                SpriteRenderer heart = heartSprites[i];
                float duration = 0.5f;
                float timer = 0f;

                Color baseColor = heart.color;

                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    float alpha = 1f - (timer / duration);
                    Color c = baseColor; c.a = alpha;
                    heart.color = c;
                    yield return null;
                }

                heart.enabled = false;
            }
        }
    }

    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartSprites.Length; i++)
        {
            if (heartSprites[i] != null)
            {
                bool on = (i < currentHP);
                if (on)
                {
                    heartSprites[i].enabled = true;
                    var col = heartSprites[i].color; col.a = 1f;
                    heartSprites[i].color = col;
                }
                else heartSprites[i].enabled = false;
            }
        }
    }

    IEnumerator CinemachineShake(float dur, float amp, float freq)
    {
        var perlin = GetActivePerlin();         
        if (perlin == null) yield break;

        float origAmp = perlin.AmplitudeGain;
        float origFreq = perlin.FrequencyGain;

        perlin.AmplitudeGain = amp;
        perlin.FrequencyGain = freq;

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;   
            yield return null;
        }

        perlin.AmplitudeGain = origAmp;
        perlin.FrequencyGain = origFreq;
    }

    CinemachineBasicMultiChannelPerlin GetActivePerlin()
    {
        if (!cinemachineBrain) cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (!cinemachineBrain) return null;

        ICinemachineCamera icam = cinemachineBrain.ActiveVirtualCamera;
        
        var comp = icam as Component;
        GameObject vcamGO = comp ? comp.gameObject : null;
        
        if (!vcamGO)
        {
            CinemachineCamera best = null;
            int bestPriority = int.MinValue;
            foreach (var cm in FindObjectsOfType<CinemachineCamera>(true))
            {
                if (!cm || !cm.isActiveAndEnabled) continue;
                if (cm.Priority >= bestPriority)
                {
                    best = cm;
                    bestPriority = cm.Priority;
                }
            }
            if (best) vcamGO = best.gameObject;
        }

        if (!vcamGO) return null;
        
        var perlin = vcamGO.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (!perlin) perlin = vcamGO.AddComponent<CinemachineBasicMultiChannelPerlin>();
        return perlin;
    }
    
    IEnumerator FlashRemainingHearts(float dur)
    {
        if (dur <= 0f || heartSprites == null) yield break;

        int count = Mathf.Clamp(currentHP, 0, heartSprites.Length);
        if (count <= 0) yield break;

        var originals = new Color[count];
        for (int i = 0; i < count; i++)
        {
            var sr = heartSprites[i];
            if (!sr) continue;
            originals[i] = sr.color;
            var flash = heartsFlashColor; flash.a = originals[i].a;
            sr.color = flash;
        }

        float t = 0f;
        while (t < dur)
        {
            float a = t / dur;
            for (int i = 0; i < count; i++)
            {
                var sr = heartSprites[i];
                if (!sr) continue;
                var from = heartsFlashColor; from.a = originals[i].a;
                sr.color = Color.Lerp(from, originals[i], a);
            }
            t += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            var sr = heartSprites[i];
            if (!sr) continue;
            sr.color = originals[i];
        }
    }
}