using UnityEngine;
using Zenject;

public class Score : MonoBehaviour
{
  private int _score;
    
  [Inject]
  private TextData _textData;

  void Start()
  {
    UpdateText();
  }

  public void AddScore()
  {
    _score++;
    
    UpdateText();
  }

  private void UpdateText()
  {
    _textData.scoreText.text = _score.ToString();
  }
  
}