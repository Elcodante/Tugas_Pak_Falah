using UnityEngine;
using UnityEngine.InputSystem;
public class DummyHealthTester : MonoBehaviour
{
    [Header("Script Health UI Reference")]
    public HealthUI healthUI;

    [Header("Data Dummy")]
    public float maxHp = 100f;
    private float currentHp;

    private void Start()
    {
        currentHp = maxHp;
        healthUI.UpdateHealthBar(currentHp, maxHp);
    }

    private void Update()
    {
        if(Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            currentHp -= 15f;
            if (currentHp < 0) currentHp = 0;

            healthUI.UpdateHealthBar(currentHp, maxHp);
            Debug.Log("Dummy Player kena damage! HP Sekarang: " + currentHp);
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            currentHp = 20f;
            if(currentHp > maxHp)
            {
                currentHp = maxHp;
            }
            healthUI.UpdateHealthBar(currentHp, maxHp);
            Debug.Log($"Current HP reset to: {currentHp}/{maxHp}");
        }
    }
}

