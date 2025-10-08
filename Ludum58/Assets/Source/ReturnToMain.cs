using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMain : MonoBehaviour
{
  public void StartGame()
  {
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }
}
