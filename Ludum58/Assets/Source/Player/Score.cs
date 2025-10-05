using System;
using TMPro;
using UnityEngine;
using Zenject;

public class Score : MonoBehaviour
{
  private TextData _textData;
  private int _score;
  
  [Inject]
  public void Construct(TextData textData)
  {
    _textData = textData;
  }

  public void AddScore()
  {
    _score++;
    UpdateText();
  }

  private void UpdateText()
  {
    _textData.scoreText.text = "Score :" + _score.ToString();
  }
}
