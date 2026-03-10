using UnityEngine;
using UnityEngine.UI; // Äëÿ Slider è Text

public class Player : MonoBehaviour
{
    public int maxHealth = 60;
    public int health = 60;
    public int shield = 0;
    public int money = 0;
    public Slider playerShields;
    public Slider playerHP;
    public Text moneyText;
    public float smoothSpeed = 5f;

    private float currentHealthVisual;
    private float currentShieldVisual;

    void Start()
    {
        currentHealthVisual = health;
        currentShieldVisual = shield;
        if (playerHP != null)
        {
            playerHP.maxValue = maxHealth;
            playerHP.value = health;
        }

        if (playerShields != null)
        {
            playerShields.maxValue = maxHealth;
            playerShields.value = shield;
        }

        UpdateUI();
    }

    void Update()
    {
        currentHealthVisual = Mathf.Lerp(currentHealthVisual, health, Time.deltaTime * smoothSpeed);
        if (playerHP != null)
        {
            playerHP.value = currentHealthVisual;
        }
        currentShieldVisual = Mathf.Lerp(currentShieldVisual, shield, Time.deltaTime * smoothSpeed);
        if (playerShields != null)
        {
            playerShields.value = currentShieldVisual;
        }
        UpdateMoneyUI();
    }

    public void TakeDamage(int amount)
    {
        int absorbed = Mathf.Min(shield, amount);
        shield -= absorbed;
        health -= (amount - absorbed);
        health = Mathf.Max(0, health);
        shield = Mathf.Max(0, shield);
    }

    public void Heal(int amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
    }

    public void AddShield(int amount)
    {
        shield += amount;
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }

    public void IncreaseMaxHealth(int amount, bool healFull = false)
    {
        maxHealth += amount;
        if (playerHP != null)
        {
            playerHP.maxValue = maxHealth;
        }
        if (healFull) health = maxHealth;
    }
    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"{money}";
        }
    }

    void UpdateUI()
    {
        UpdateMoneyUI();
    }
}