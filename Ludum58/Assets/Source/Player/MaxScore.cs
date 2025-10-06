using System;
using TMPro;
using UnityEngine;
using Zenject;

public class MaxScore : MonoBehaviour
{
  private TextData _textData;
  private const string MAX_SCORE_KEY = "MaxScore";

  [Inject]
  public void Construct(TextData textData)
  {
    _textData = textData;
  }
  void Start()
  {
    DisplayMaxScore();
  }

  public void DisplayMaxScore()
  {
    int maxScore = PlayerPrefs.GetInt(MAX_SCORE_KEY, 0);
    _textData.maxScoreText.text = $"Record: {maxScore}";
  }
  
  public void ResetMaxScore()
  {
    PlayerPrefs.DeleteKey(MAX_SCORE_KEY);
    PlayerPrefs.Save();
    DisplayMaxScore();
  }
}
