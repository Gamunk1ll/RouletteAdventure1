using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    public Enemy enemy;
    public Text healthText;
    public Text shieldText;

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
            if (shieldText.gameObject != null)
            {
                shieldText.gameObject.SetActive(enemy.shield > 0);
            }
        }
    }

    //void LateUpdate()
    //{
    //    if (Camera.main != null)
    //    {
    //        transform.forward = Camera.main.transform.forward;
    //    }
    //}
}