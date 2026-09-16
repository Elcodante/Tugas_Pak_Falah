using UnityEngine;
using UnityEngine.UI;
public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Image healthBarFill;

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null)
        {
            Debug.LogError("Gagal update UI: Komponen Image 'HealthBarFill' belum dimasukkan ke Inspector!");
            return; 
        }

        if (maxHealth <= 0)
        {
            Debug.LogWarning("Max health must be greater than zero.");
            return;
        }
        float fillAmount = currentHealth / maxHealth;
        healthBarFill.fillAmount = fillAmount;
    }
}
