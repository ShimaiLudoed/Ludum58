using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Игровые параметры")]
    public int maxHP = 3;
    public int currentHP;
    
    [Header("Спрайты жизней")]
    public SpriteRenderer[] heartSprites;

    void Start()
    {
        currentHP = maxHP;
        UpdateHeartsUI();
    }

    public void TakeDamage(int amount)
    {
        int previousHP = currentHP;
        currentHP -= amount;
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        StartCoroutine(AnimateHeartLoss(previousHP));
    }

    IEnumerator AnimateHeartLoss(int previousHP)
    {
        for (int i = currentHP; i < previousHP && i < heartSprites.Length; i++)
        {
            if (heartSprites[i] != null)
            {
                SpriteRenderer heart = heartSprites[i];
                
                float duration = 0.5f;
                float timer = 0f;
                
                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    float alpha = 1f - (timer / duration);
                    Color color = heart.color;
                    color.a = alpha;
                    heart.color = color;
                    yield return null;
                }
                
                heart.enabled = false;
            }
        }
    }

    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartSprites.Length; i++)
        {
            if (heartSprites[i] != null)
            {
                bool shouldBeEnabled = (i < currentHP);
                
                if (shouldBeEnabled)
                {
                    heartSprites[i].enabled = true;
                    Color color = heartSprites[i].color;
                    color.a = 1f;
                    heartSprites[i].color = color;
                }
                else
                {
                    heartSprites[i].enabled = false;
                }
            }
        }
    }
}