using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [Header("Игровые параметры")]
    public int HP = 100;
    public int score = 0;

    [Header("UI элементы")]
    public TMP_Text hpText;
    public TMP_Text scoreText;

    void Start()
    {
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        HP -= amount;
        if (HP <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        if (hpText != null) hpText.text = $"HP: {HP}";
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }
}
