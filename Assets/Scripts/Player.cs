using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    [SerializeField] private int maxHealth = 60;
    [SerializeField] private int currentHealth = 60;
    [SerializeField] private int maxShield = 30;
    [SerializeField] private int currentShield = 0;
    [SerializeField] private int money = 0;
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private Transform healthBar;
    [SerializeField] private Transform shieldBar;
    [SerializeField] private TextMeshPro healthText;
    [SerializeField] private TextMeshPro shieldText;
    [SerializeField] private TextMeshPro moneyText;

    public int Health => currentHealth;
    public int MaxHealth => maxHealth;
    public int Shield => currentShield;
    public int MaxShield => maxShield;
    public int Money => money;

    private float visualHealth;
    private float visualShield;

    private float healthBarMaxWidth;
    private float shieldBarMaxWidth;
    private Vector3 healthBarOriginalScale;
    private Vector3 shieldBarOriginalScale;
    private Vector3 healthBarOriginalPosition;
    private Vector3 shieldBarOriginalPosition;

    void Start()
    {
        visualHealth = currentHealth;
        visualShield = currentShield;

        if (healthBar != null)
        {
            healthBarOriginalScale = healthBar.localScale;
            healthBarMaxWidth = Mathf.Abs(healthBarOriginalScale.x);
            healthBarOriginalPosition = healthBar.position;
        }

        if (shieldBar != null)
        {
            shieldBarOriginalScale = shieldBar.localScale;
            shieldBarMaxWidth = Mathf.Abs(shieldBarOriginalScale.x);
            shieldBarOriginalPosition = shieldBar.position;
        }

        UpdateUI();
    }

    void Update()
    {
        visualHealth = Mathf.Lerp(visualHealth, currentHealth, Time.deltaTime * animationSpeed);
        visualShield = Mathf.Lerp(visualShield, currentShield, Time.deltaTime * animationSpeed);

        UpdateBars();
        UpdateText();
    }

    void UpdateBars()
    {
        if (healthBar != null)
        {
            float healthPercent = Mathf.Clamp01(visualHealth / (float)maxHealth);
            UpdateBarScale(healthBar, healthBarOriginalScale, healthBarOriginalPosition, healthBarMaxWidth, healthPercent);
        }

        if (shieldBar != null)
        {
            float shieldPercent = Mathf.Clamp01(visualShield / (float)maxShield);
            UpdateBarScale(shieldBar, shieldBarOriginalScale, shieldBarOriginalPosition, shieldBarMaxWidth, shieldPercent);
        }
    }

    void UpdateBarScale(Transform bar, Vector3 originalScale, Vector3 originalPosition, float maxWidth, float percent)
    {
        float targetWidth = maxWidth * percent;
        float scaleSign = Mathf.Sign(originalScale.x);

        Vector3 targetScale = originalScale;
        targetScale.x = scaleSign * targetWidth;

        float lerpSpeed = Time.deltaTime * animationSpeed * 2f;
        bar.localScale = Vector3.Lerp(bar.localScale, targetScale, lerpSpeed);

        // Опционально: центрирование бара, если нужно
        // Vector3 targetLocalPosition = originalPosition;
        // float halfWidthDelta = (maxWidth - targetWidth) * 0.5f;
        // targetLocalPosition.x = originalPosition.x - (halfWidthDelta * scaleSign);
        // bar.position = Vector3.Lerp(bar.position, targetLocalPosition, lerpSpeed);
    }

    void UpdateText()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        if (shieldText != null)
        {
            shieldText.text = $"{currentShield}/{maxShield}";
        }

        if (moneyText != null)
        {
            moneyText.text = money.ToString();
        }
    }

    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
        UpdateMoneyUI();
    }

    public void TakeDamage(int amount)
    {
        int damage = amount;

        if (currentShield > 0)
        {
            int shieldDamage = Mathf.Min(currentShield, damage);
            currentShield -= shieldDamage;
            damage -= shieldDamage;
        }

        if (damage > 0)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
        }

        UpdateUI();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateUI();
    }

    public void AddShield(int amount)
    {
        currentShield = Mathf.Min(maxShield, currentShield + amount);
        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateMoneyUI();
    }

    public void IncreaseMaxHealth(int amount, bool healFull = false)
    {
        maxHealth += amount;
        if (healFull)
        {
            currentHealth = maxHealth;
        }
        UpdateUI();
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateUI();
    }

    public void SetShield(int value)
    {
        currentShield = Mathf.Clamp(value, 0, maxShield);
        UpdateUI();
    }

    public void UpdateUI()
    {
        UpdateBars();
        UpdateText();
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"${money}";
        }
    }

    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetShield() => currentShield;
    public int GetMaxShield() => maxShield;
    public int GetMoney() => money;
}