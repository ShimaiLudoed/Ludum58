using UnityEngine;
using Zenject;

public class Score : MonoBehaviour
{
  private int _score;
  private int _maxScore;
  private const string MAX_SCORE_KEY = "MaxScore";
    
  [Inject]
  private TextData _textData;

  void Start()
  {
    LoadMaxScore();
    UpdateText();
  }

  public void AddScore()
  {
    _score++;
        
    if (_score > _maxScore)
    {
      _maxScore = _score;
      SaveMaxScore();
    }
        
    UpdateText();
  }

  private void UpdateText()
  {
    _textData.scoreText.text = _score.ToString();
  }

  private void LoadMaxScore()
  {
    _maxScore = PlayerPrefs.GetInt(MAX_SCORE_KEY, 0);
  }

  private void SaveMaxScore()
  {
    PlayerPrefs.SetInt(MAX_SCORE_KEY, _maxScore);
    PlayerPrefs.Save();
  }

  public void ResetCurrentScore()
  {
    _score = 0;
    UpdateText();
  }
}