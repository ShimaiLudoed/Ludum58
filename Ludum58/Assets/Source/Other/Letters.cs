using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static LeanTween;


public class LettersFlyAway : MonoBehaviour
{
    [Header("UI-группы")]
    public RectTransform[] up;     // улетают вверх
    public RectTransform[] down;   // улетают вниз

    [Header("Рандом-диапазоны")]
    public Vector2 distanceRange = new Vector2(900f, 1500f);  // по Y, px
    public Vector2 durationRange = new Vector2(0.7f, 1.2f);   // сек
    public Vector2 delayRange    = new Vector2(0.0f, 0.25f);  // сек
    public Vector2 xJitterRange  = new Vector2(-120f, 120f);  // боковой разброс, px
    public Vector2 rotRangeDeg   = new Vector2(-12f, 12f);    // поворот Z, градусы

    [Header("Гашение")]
    public bool fadeOut = true;
    public Vector2 fadeMulRange = new Vector2(0.8f, 1.0f);    // доля от duration

    [Header("Tweens")]
    public LeanTweenType ease = LeanTweenType.easeInCubic;
    public bool useUnscaledTime = true;

    [Header("Layout-страховка")]
    public bool disableParentLayouts = true;   // выключаем LayoutGroup/CSF на корне и НЕ включаем обратно
    public bool ignoreLayoutOnLetters = true;  // ставим LayoutElement.ignoreLayout на буквах

    [Header("Поведение по завершению")]
    public bool deactivateOnComplete = true;   // буквы исчезают и не возвращаются

    System.Random rng;
    private ISound _sound;

    [Inject]
    public void Construct(ISound sound)
    {
        _sound = sound;
    }

    public void PlaySound()
    {
        _sound.PlayButtonClick();
    }
    
    public void Play()
    {
        rng = new System.Random((GetInstanceID() ^ System.Environment.TickCount));

        RectTransform root = GetComponentInParent<RectTransform>();
        if (disableParentLayouts && root)
        {
            var h = root.GetComponent<HorizontalLayoutGroup>(); if (h) h.enabled = false;
            var v = root.GetComponent<VerticalLayoutGroup>();   if (v) v.enabled = false;
            var g = root.GetComponent<GridLayoutGroup>();       if (g) g.enabled = false;
            var c = root.GetComponent<ContentSizeFitter>();     if (c) c.enabled = false;
        }

        AnimateSet(up,   +1f);
        AnimateSet(down, -1f);
    }

    void AnimateSet(RectTransform[] arr, float dir)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            AnimateOne(arr[i], dir);
    }

    void AnimateOne(RectTransform rt, float dir)
    {
        if (!rt) return;

        if (ignoreLayoutOnLetters)
        {
            var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        LeanTween.cancel(rt.gameObject);

        float dist   = Rand(distanceRange.x, distanceRange.y) * dir;
        float dur    = Rand(durationRange.x, durationRange.y);
        float delay  = Rand(delayRange.x,    delayRange.y);
        float xjit   = Rand(xJitterRange.x,  xJitterRange.y);
        float rotZ   = Rand(rotRangeDeg.x,   rotRangeDeg.y);
        float fadeMul= Mathf.Clamp01(Rand(fadeMulRange.x, fadeMulRange.y));

        Vector2 from = rt.anchoredPosition;
        Vector2 to   = from + new Vector2(xjit, dist);

        // движение
        LeanTween.value(rt.gameObject, from, to, dur)
                 .setDelay(delay)
                 .setEase(ease)
                 .setIgnoreTimeScale(useUnscaledTime)
                 .setOnUpdate((Vector2 p) => rt.anchoredPosition = p)
                 .setOnComplete(() => { if (deactivateOnComplete) rt.gameObject.SetActive(false); });

        // поворот
        Quaternion rotFrom = rt.localRotation;
        Quaternion rotTo   = Quaternion.Euler(0f, 0f, rotZ) * rotFrom;
        LeanTween.value(rt.gameObject, 0f, 1f, dur)
                 .setDelay(delay)
                 .setEase(ease)
                 .setIgnoreTimeScale(useUnscaledTime)
                 .setOnUpdate(t => rt.localRotation = Quaternion.Slerp(rotFrom, rotTo, t));

        // фейд
        if (fadeOut)
        {
            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            LeanTween.alphaCanvas(cg, 0f, dur * fadeMul)
                     .setDelay(delay)
                     .setIgnoreTimeScale(useUnscaledTime);
        }
    }

    float Rand(float a, float b)
    {
        if (a > b) { var t = a; a = b; b = t; }
        return a + (float)rng.NextDouble() * (b - a);
    }
}
