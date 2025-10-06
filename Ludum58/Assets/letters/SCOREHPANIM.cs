using System.Collections;
using UnityEngine;
using static LeanTween;
public class CanvasShowController : MonoBehaviour
{
    [SerializeField] GameObject targetRoot;      // выключенный Canvas/панель
    [SerializeField, Min(0f)] float delay = 0.3f;
    [SerializeField] float duration = 0.6f;
    [SerializeField] float flyDistance = 280f;
    [SerializeField] LeanTweenType ease = LeanTweenType.easeOutCubic;

    public void Show() => StartCoroutine(ShowRoutine());

    IEnumerator ShowRoutine()
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay); // КЛЮЧЕВОЕ

        if (!targetRoot) yield break;
        targetRoot.SetActive(true);                                      // включаем после задержки

        var rt = targetRoot.GetComponent<RectTransform>();
        var cg = targetRoot.GetComponent<CanvasGroup>() ?? targetRoot.AddComponent<CanvasGroup>();

        Vector2 end = rt.anchoredPosition;
        Vector2 off = new Vector2(flyDistance, -flyDistance);            // из правого-нижнего
        rt.anchoredPosition = end + off;
        rt.localScale = Vector3.one * 0.97f;
        cg.alpha = 0f;

        LeanTween.cancel(rt);
        LeanTween.cancel(targetRoot);

        LeanTween.move(rt, end, duration).setEase(ease).setIgnoreTimeScale(true);
        LeanTween.scale(rt, Vector3.one, duration).setEase(LeanTweenType.easeOutCubic).setIgnoreTimeScale(true);
        LeanTween.value(targetRoot, 0f, 1f, duration)
                 .setOnUpdate(a => cg.alpha = a)
                 .setIgnoreTimeScale(true);
    }
}
