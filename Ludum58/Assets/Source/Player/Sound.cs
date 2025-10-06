using UnityEngine;
using Zenject;

public class Sound : ISound
{
  private readonly AudioSource _source;
  private readonly AudioClip _buttonClick;
  private readonly AudioClip _takeStar;
  private readonly AudioClip _takeDamage;
 private readonly AudioClip deathmom3;

  [Inject]
  public Sound(AudioData audioData)
  {
    _source =  audioData.AudioSource;
    _buttonClick = audioData.ButtonClick;
    _takeStar = audioData.TakeStar;
    _takeDamage = audioData.TakeDamage;
    deathmom3 = audioData.deathmom4;
  }

  public void PlayButtonClick()
  {
    _source.PlayOneShot(_buttonClick);
  }
  public void PlayTakeStar()
  {
    _source.PlayOneShot(_takeStar);
  }
  public void PlayTakeDamage()
  {
    _source.PlayOneShot(_takeDamage);
  }
  public void deathmom2()
  {
    _source.PlayOneShot(deathmom3);
  }
}
