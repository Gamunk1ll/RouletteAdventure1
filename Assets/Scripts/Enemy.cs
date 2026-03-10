using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 50;
    public int health = 50;
    public int shield = 0;

    private EnemyUI enemyUI;

    void Start()
    {
        enemyUI = GetComponent<EnemyUI>();
        if (enemyUI == null)
            enemyUI = GetComponentInChildren<EnemyUI>();
        Invoke(nameof(UpdateUI), 0.1f);
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
        Destroy(gameObject);
    }

}