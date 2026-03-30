using UnityEngine;
using TMPro;

public class EnemyUI : MonoBehaviour
{
    public Enemy enemy;
    public TextMeshPro healthText;
    public TextMeshPro shieldText;

    void Start()
    {
        if (enemy == null)
            enemy = GetComponentInParent<Enemy>();

        Invoke(nameof(UpdateUI), 0.1f);
    }

    public void UpdateUI()
    {
        if (enemy == null) return;

        if (healthText != null)
        {
            healthText.text = enemy.health.ToString();
            healthText.color = Color.green;
        }

        if (shieldText != null)
        {
            shieldText.text = enemy.shield.ToString();
            shieldText.gameObject.SetActive(enemy.shield > 0);
        }
    }


}