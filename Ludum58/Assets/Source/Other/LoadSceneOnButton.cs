using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class FadeByImageLoader : MonoBehaviour
{
    [SerializeField] string sceneName;
    [SerializeField] GameObject fadePanel;   // панель, изначально выключена
    [SerializeField] Image fadeImage;         // дочерний Image панели (черный)
    [SerializeField] float fadeDuration = 1f;

    void Awake()
    {
        if (fadePanel != null)
        {
            fadePanel.SetActive(false);
        }
    }

    public void OnButtonPressed()
    {
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        if (fadePanel == null || fadeImage == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // включить панель
        fadePanel.SetActive(true);

        // удостовериться что цвет начальный (черный, альфа = 0)
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;

        // фейд (0 → 1)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / fadeDuration);
            Color nc = fadeImage.color;
            nc.a = a;
            fadeImage.color = nc;
            yield return null;
        }
        // установить окончательную альфу = 1
        Color finalColor = fadeImage.color;
        finalColor.a = 1f;
        fadeImage.color = finalColor;

        // (не обязательно) краткая задержка
        yield return new WaitForSeconds(0.1f);

        // загрузка сцены асинхронно
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            yield return null;
        }
    }
}
