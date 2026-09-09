using UnityEngine;
using UnityEngine.UI;
public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Image healthBarFill;

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if(maxHealth <= 0)
        {
            Debug.LogWarning("Max health must be greater than zero.");
            return;
        }
        float fillAmount = currentHealth / maxHealth;
        healthBarFill.fillAmount = fillAmount;
    }
}
