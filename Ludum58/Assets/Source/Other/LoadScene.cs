using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnButton1 : MonoBehaviour
{
    [SerializeField]
    string sceneName;  // имя сцены, установить в инспекторе

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
