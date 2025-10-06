using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;
using Zenject;
using static LeanTween;

public class CutsceneOnClick : MonoBehaviour
{
    [Header("Катсцена")]
    public PlayableDirector cutscene;

    [Header("Панель (коричневая полоса)")]
    public GameObject panelToHide;
    public float stripDuration = 0.65f;
    public float hideDelay = 0.15f;

    [Header("START (только Highlighted в Animator)")]
    public bool waitForStart = true;          // ← галка: ждать клип клика на START
    public Animator startButtonAnimator;      // Animator кнопки
    public string clickStateName = "ClickAnim";
    public int clickLayer = 0;
    public float clickFallback = 0.35f;

    [Header("Остальные кнопки (фейд-аут)")]
    public Graphic[] otherButtons;
    public float otherFadeDuration = 0.45f;

    [Header("Игрок")]
    public GameObject playerRoot;

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
    
    public void OnClick()
    {
        if (startButtonAnimator)
            startButtonAnimator.CrossFadeInFixedTime(clickStateName, 0f, clickLayer, 0f);

        // включаем игрока и катсцену
        if (playerRoot) playerRoot.SetActive(true);
        if (cutscene) { cutscene.time = 0; cutscene.Play(); }

        // последовательность
        if (panelToHide) StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        if (waitForStart)
            yield return WaitClipFinished(startButtonAnimator, clickStateName, clickLayer, clickFallback);
        yield return AnimateAndHide();
    }

    // Ждём длительность клипа (unscaled)
    private static IEnumerator WaitClipFinished(Animator anim, string stateName, int layer, float fallback)
    {
        if (!anim) { yield return new WaitForSecondsRealtime(fallback); yield break; }

        var prevMode = anim.updateMode;
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;

        // дать кадр войти в стейт
        yield return null;

        // попытка прочитать длину активного клипа
        float len = 0f;
        var infos = anim.GetCurrentAnimatorClipInfo(layer);
        if (infos != null && infos.Length > 0 && infos[0].clip)
        {
            var st = anim.GetCurrentAnimatorStateInfo(layer);
            float sp = Mathf.Approximately(st.speed, 0f) ? 1f : st.speed;
            len = infos[0].clip.length / sp;
        }
        if (len <= 0f) len = fallback;

        float t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < len) yield return null;

        // дождаться завершения переходов
        while (anim.IsInTransition(layer)) yield return null;

        anim.updateMode = prevMode;
    }

    private IEnumerator AnimateAndHide()
    {
        var panelRt = panelToHide.GetComponent<RectTransform>();
        if (!panelRt) yield break;

        var cgPanel = panelToHide.GetComponent<CanvasGroup>() ?? panelToHide.AddComponent<CanvasGroup>();
        cgPanel.interactable = false; 
        cgPanel.blocksRaycasts = false;

        // фейд остальных
        if (otherButtons != null)
        {
            foreach (var g in otherButtons)
            {
                if (!g) continue;
                var go = g.gameObject;
                var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
                cg.interactable = false; 
                cg.blocksRaycasts = false;

                LeanTween.alphaCanvas(cg, 0f, otherFadeDuration).setIgnoreTimeScale(true);
                g.CrossFadeAlpha(0f, otherFadeDuration, true);
                LeanTween.delayedCall(go, otherFadeDuration, () => go.SetActive(false)).setIgnoreTimeScale(true);
            }
        }

        // полоса: снизу-вверх через offsetMin.y
        _ = panelToHide.GetComponent<RectMask2D>() ?? panelToHide.AddComponent<RectMask2D>();

        Vector2 prevMin = panelRt.anchorMin, prevMax = panelRt.anchorMax;
        bool stretchY = Mathf.Approximately(prevMin.y, 0f) && Mathf.Approximately(prevMax.y, 1f);
        if (!stretchY)
        {
            panelRt.anchorMin = new Vector2(prevMin.x, 0f);
            panelRt.anchorMax = new Vector2(prevMax.x, 1f);
        }

        float startOffset = panelRt.offsetMin.y;
        float targetOffset = panelRt.rect.height + startOffset;

        LeanTween.value(panelRt.gameObject, startOffset, targetOffset, stripDuration)
                 .setEase(LeanTweenType.easeInCubic)
                 .setIgnoreTimeScale(true)
                 .setOnUpdate(v =>
                 {
                     var off = panelRt.offsetMin; off.y = v; panelRt.offsetMin = off;
                 });

        float wait = Mathf.Max(stripDuration, otherFadeDuration) + hideDelay;
        float t1 = Time.unscaledTime; 
        while (Time.unscaledTime - t1 < wait) yield return null;

        panelToHide.SetActive(false);

        if (!stretchY)
        {
            panelRt.anchorMin = prevMin;
            panelRt.anchorMax = prevMax;
        }
    }
}
