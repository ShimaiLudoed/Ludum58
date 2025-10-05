using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("Игровые параметры")]
    public int maxHP = 100;
    public int currentHP;
    public int score = 0;

    [Header("UI элементы")]
    public TMP_Text hpText;
    public TMP_Text scoreText;

    void Start()
    {
        currentHP = maxHP;
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP <= 0)
        {
            currentHP = 0;
            Debug.Log("💀 Игрок погиб");
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        if (hpText != null) hpText.text = $"HP: {currentHP}";
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }
}
