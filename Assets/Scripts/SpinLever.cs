using UnityEngine;

public class SpinLever : MonoBehaviour
{
    [Header("References")]
    public BallManager ballManager;

    [Header("Debug")]
    private bool isSpinning = false;
    private Color originalColor;
    private Renderer leverRenderer;
    private bool isHovering = false;

    void Start()
    {
        Debug.Log("[SpinLever] Инициализация...");

        leverRenderer = GetComponent<Renderer>();
        if (leverRenderer != null)
        {
            originalColor = leverRenderer.material.color;
            Debug.Log("[SpinLever] Оригинальный цвет: " + originalColor);
        }
        else
        {
            Debug.LogWarning("[SpinLever] Renderer не найден!");
        }

        if (ballManager == null)
        {
            ballManager = FindObjectOfType<BallManager>();
        }

        if (ballManager != null)
        {
            Debug.Log("[SpinLever] BallManager найден! Шаров: " + ballManager.currentBalls);
        }
        else
        {
            Debug.LogError("[SpinLever] BallManager НЕ найден!");
        }

        Debug.Log("[SpinLever] Готов к работе!");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && isHovering)
        {
            OnLeverClick();
        }
    }

    void OnMouseEnter()
    {
        isHovering = true;

        if (!isSpinning && leverRenderer != null &&
            GameManager.Instance != null &&
            GameManager.Instance.state == BattleState.WaitingForPlayer &&
            ballManager != null && ballManager.currentBalls > 0)
        {
            leverRenderer.material.color = Color.yellow;
        }
    }

    void OnMouseExit()
    {
        isHovering = false;

        if (leverRenderer != null && !isSpinning)
        {
            leverRenderer.material.color = originalColor;
        }
    }

    void OnLeverClick()
    {
        Debug.Log("[SpinLever] Клик по рычагу!");

        if (isSpinning)
        {
            Debug.Log("Уже крутится!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance = null!");
            return;
        }

        Debug.Log("[SpinLever] Состояние игры: " + GameManager.Instance.state);

        if (GameManager.Instance.state != BattleState.WaitingForPlayer)
        {
            Debug.Log("Нельзя крутить! Состояние: " + GameManager.Instance.state);
            return;
        }

        if (ballManager == null)
        {
            Debug.LogError("ballManager = null!");
            return;
        }

        Debug.Log("[SpinLever] Текущее количество шаров: " + ballManager.currentBalls);

        if (ballManager.currentBalls <= 0)
        {
            Debug.Log("Нет шаров!");
            return;
        }

        Debug.Log("ЗАПУСК РЫЧАГА!");
        ActivateLever();
    }

    void ActivateLever()
    {
        Debug.Log("[SpinLever] Активация рычага...");
        isSpinning = true;

        if (leverRenderer != null)
        {
            leverRenderer.material.color = Color.yellow;
            Debug.Log("[SpinLever] Цвет изменен на желтый");
        }

        if (ballManager != null)
        {
            Debug.Log("[SpinLever] Вызов LaunchAllBalls()");
            ballManager.LaunchAllBalls();
        }

        Invoke(nameof(ResetLever), 1f);
    }

    void ResetLever()
    {
        Debug.Log("[SpinLever] Сброс рычага");
        isSpinning = false;

        if (leverRenderer != null)
        {
            leverRenderer.material.color = originalColor;
        }
    }
}