using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [Header("Игровые параметры")]
    public int HP = 100;
    
    public TMP_Text hpText;

    void Start()
    {
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
    }
}
