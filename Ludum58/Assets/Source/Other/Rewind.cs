using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class SceneRewindManager : MonoBehaviour
{
    [Header("Обычные катсцены")]
    public PlayableDirector normal1;
    public PlayableDirector normal2;

    [Header("Реверсивные версии")]
    public PlayableDirector reverse1;
    public PlayableDirector reverse2;

    private PlayableDirector lastNormalPlayed;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (lastNormalPlayed != null)
            {
                if (lastNormalPlayed == normal1 && reverse1 != null)
                {
                    PlayReverse(reverse1);
                }
                else if (lastNormalPlayed == normal2 && reverse2 != null)
                {
                    PlayReverse(reverse2);
                }
            }
        }
    }

    public void PlayNormal1()
    {
        PlayNormal(normal1);
    }

    public void PlayNormal2()
    {
        PlayNormal(normal2);
    }

    private void PlayNormal(PlayableDirector pd)
    {
        if (pd == null) return;
        pd.timeUpdateMode = DirectorUpdateMode.GameTime;
        pd.time = 0;
        pd.Play();
        lastNormalPlayed = pd;
    }

    private void PlayReverse(PlayableDirector pd)
    {
        StartCoroutine(DoReverse(pd));
    }

    private IEnumerator DoReverse(PlayableDirector pd)
    {
        // предполагаем, что reverse версия уже настроена в режиме Hold
        pd.timeUpdateMode = DirectorUpdateMode.Manual;

        double t = pd.duration;
        while (t > 0)
        {
            t -= Time.deltaTime;
            pd.time = Mathf.Max((float) t, 0f);
            pd.Evaluate();
            yield return null;
        }

        pd.Stop();
        pd.timeUpdateMode = DirectorUpdateMode.GameTime;
    }
}
