using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    [Header(" Stats")]
    [SerializeField] private int maxHealth = 60;
    [SerializeField] private int currentHealth = 60;
    [SerializeField] private int maxShield = 30;
    [SerializeField] private int currentShield = 0;
    [SerializeField] private int money = 0;

    public int Health => currentHealth;
    public int MaxHealth => maxHealth;
    public int Shield => currentShield;
    public int MaxShield => maxShield;
    public int Money => money;

    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
        UpdateMoneyUI();
    }

    [Header(" 3D Bar References")]
    [SerializeField] private Transform healthBar;
    [SerializeField] private Transform shieldBar;
    [SerializeField] private TextMeshPro healthText;
    [SerializeField] private TextMeshPro shieldText;
    [SerializeField] private TextMeshPro moneyText;

    [Header(" Settings")]
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private bool healthBarAnchorLeft = true;
    [SerializeField] private bool shieldBarAnchorLeft = true;

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
            healthBarOriginalPosition = healthBar.localPosition;
        }

        if (shieldBar != null)
        {
            shieldBarOriginalScale = shieldBar.localScale;
            shieldBarMaxWidth = Mathf.Abs(shieldBarOriginalScale.x);
            shieldBarOriginalPosition = shieldBar.localPosition;
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
            UpdateBarTransform(
                healthBar,
                healthBarOriginalScale,
                healthBarOriginalPosition,
                healthBarMaxWidth,
                healthPercent,
                healthBarAnchorLeft
            );
        }

        if (shieldBar != null)
        {
            float shieldPercent = Mathf.Clamp01(visualShield / (float)maxShield);
            UpdateBarTransform(
                shieldBar,
                shieldBarOriginalScale,
                shieldBarOriginalPosition,
                shieldBarMaxWidth,
                shieldPercent,
                shieldBarAnchorLeft
            );
        }
    }

    void UpdateBarTransform(
        Transform bar,
        Vector3 originalScale,
        Vector3 originalPosition,
        float maxWidth,
        float percent,
        bool anchorLeft
    )
    {
        float targetWidth = maxWidth * percent;
        float widthDelta = maxWidth - targetWidth;
        float scaleSign = Mathf.Sign(originalScale.x);

        Vector3 targetScale = originalScale;
        targetScale.x = scaleSign * targetWidth;

        Vector3 targetPosition = originalPosition;
        targetPosition.x = anchorLeft
            ? originalPosition.x + (widthDelta * 0.5f)
            : originalPosition.x - (widthDelta * 0.5f);

        float barLerpSpeed = Time.deltaTime * animationSpeed * 2f;
        bar.localScale = Vector3.Lerp(bar.localScale, targetScale, barLerpSpeed);
        bar.localPosition = Vector3.Lerp(bar.localPosition, targetPosition, barLerpSpeed);
    }

    void UpdateText()
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.FloorToInt(visualHealth)}/{maxHealth}";
        }

        if (shieldText != null)
        {
            shieldText.text = $"{Mathf.FloorToInt(visualShield)}/{maxShield}";
        }

        if (moneyText != null)
        {
            moneyText.text = money.ToString();
        }
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
        visualHealth = currentHealth;
        visualShield = currentShield;
        UpdateBars();
        UpdateText();
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = money.ToString();
        }
    }

    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetShield() => currentShield;
    public int GetMaxShield() => maxShield;
    public int GetMoney() => money;
}
