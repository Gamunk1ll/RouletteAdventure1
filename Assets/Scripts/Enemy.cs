using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 50;
    public int health = 50;
    public int shield = 0;

    private EnemyUI enemyUI;
    private int baseMaxHealth;

    void Start()
    {
        if (baseMaxHealth <= 0)
            baseMaxHealth = Mathf.Max(1, maxHealth);

        enemyUI = GetComponent<EnemyUI>();
        if (enemyUI == null)
            enemyUI = GetComponentInChildren<EnemyUI>();
        Invoke(nameof(UpdateUI), 0.1f);
    }

    private void Awake()
    {
        baseMaxHealth = Mathf.Max(1, maxHealth);
    }

    public bool IsDead()
    {
        return health <= 0;
    }

    public void TakeDamage(int amount)
    {
        int absorbed = Mathf.Min(shield, amount);
        shield -= absorbed;
        health -= (amount - absorbed);
        health = Mathf.Max(0, health);

        UpdateUI();

        if (IsDead())
        {
            OnDeath();
        }
    }

    public void AddShield(int amount)
    {
        shield += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (enemyUI != null)
        {
            enemyUI.UpdateUI();
        }
    }

    private void OnDeath()
    {
        gameObject.SetActive(false);
    }

    public void ResetForWave(int waveNumber, int healthGrowthPerWave)
    {
        int waveBonus = Mathf.Max(0, waveNumber - 1) * Mathf.Max(0, healthGrowthPerWave);
        maxHealth = baseMaxHealth + waveBonus;
        health = maxHealth;
        shield = 0;
        UpdateUI();
    }

}
