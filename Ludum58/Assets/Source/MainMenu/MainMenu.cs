using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
  [SerializeField] private GameObject settingPanel;

  public void LaunchLevel(string sceneName)
  {
    SceneManager.LoadScene(sceneName);
  }

  public void StartGame()
  {
    LaunchLevel("GameScene");
  }

  public void OpenSettings()
  {
    settingPanel.SetActive(true);
  }

  public void CloseSettings()
  {
    settingPanel.SetActive(false);
  }

  private void Exit()
  {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Escape))
    {
      if (settingPanel != null && settingPanel.activeSelf)
      {
        CloseSettings();
      }
      else
      {
        Exit();
      }
    }
  }
}
