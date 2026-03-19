using System.Collections;
using UnityEngine;

public class SpinLever : MonoBehaviour
{
    [Header("References")]
    public BallManager ballManager;
    public Renderer hoverRenderer;
    public Transform leverPivot;

    [Header("Interaction")]
    public Color hoverColor = Color.yellow;

    [Header("Animation")]
    [Min(0f)] public float pressAngle = 35f;
    [Min(0.01f)] public float pressDuration = 0.12f;
    [Min(0.01f)] public float releaseDuration = 0.2f;
    public Vector3 pressAxis = Vector3.forward;

    [Header("Debug")]
    [SerializeField] private bool isSpinning;

    private Color originalColor;
    private Renderer cachedRenderer;
    private bool isHovering;
    private Quaternion initialLocalRotation;
    private Coroutine leverAnimationRoutine;

    private Renderer ActiveRenderer => hoverRenderer != null ? hoverRenderer : cachedRenderer;
    private Transform ActivePivot => leverPivot != null ? leverPivot : transform;

    private void Start()
    {
        Debug.Log("[SpinLever] Initializing...");

        cachedRenderer = GetComponent<Renderer>();
        Renderer rendererToUse = ActiveRenderer;
        if (rendererToUse != null)
        {
            originalColor = rendererToUse.material.color;
            Debug.Log($"[SpinLever] Original color cached: {originalColor}");
        }
        else
        {
            Debug.LogWarning("[SpinLever] Renderer not found for hover feedback.");
        }

        initialLocalRotation = ActivePivot.localRotation;

        if (ballManager == null)
        {
            ballManager = FindObjectOfType<BallManager>();
        }

        if (ballManager != null)
        {
            Debug.Log($"[SpinLever] BallManager found. Balls available: {ballManager.currentBalls}");
        }
        else
        {
            Debug.LogError("[SpinLever] BallManager not found.");
        }

        Debug.Log("[SpinLever] Ready.");
    }

    private void OnMouseDown()
    {
        OnLeverClick();
    }

    private void OnMouseEnter()
    {
        isHovering = true;
        RefreshVisualState();
    }

    private void OnMouseExit()
    {
        isHovering = false;
        RefreshVisualState();
    }

    private void OnDisable()
    {
        if (leverAnimationRoutine != null)
        {
            StopCoroutine(leverAnimationRoutine);
            leverAnimationRoutine = null;
        }

        ActivePivot.localRotation = initialLocalRotation;
        isSpinning = false;
        ApplyBaseColor();
    }

    private void OnLeverClick()
    {
        Debug.Log("[SpinLever] Lever click received.");

        if (isSpinning)
        {
            Debug.Log("[SpinLever] Lever is already animating.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[SpinLever] GameManager.Instance is null.");
            return;
        }

        Debug.Log($"[SpinLever] Current battle state: {GameManager.Instance.state}");

        if (GameManager.Instance.state != BattleState.WaitingForPlayer)
        {
            Debug.Log($"[SpinLever] Lever blocked. State: {GameManager.Instance.state}");
            return;
        }

        if (ballManager == null)
        {
            Debug.LogError("[SpinLever] ballManager is null.");
            return;
        }

        Debug.Log($"[SpinLever] Balls available: {ballManager.currentBalls}");

        if (ballManager.currentBalls <= 0)
        {
            Debug.Log("[SpinLever] No balls left.");
            return;
        }

        ActivateLever();
    }

    private void ActivateLever()
    {
        Debug.Log("[SpinLever] Activating lever...");
        isSpinning = true;
        SetHighlight(true);

        if (leverAnimationRoutine != null)
        {
            StopCoroutine(leverAnimationRoutine);
        }

        leverAnimationRoutine = StartCoroutine(AnimateLeverPress());
    }

    private IEnumerator AnimateLeverPress()
    {
        Transform pivot = ActivePivot;
        Quaternion pressedRotation = initialLocalRotation * Quaternion.AngleAxis(pressAngle, pressAxis.normalized);

        yield return RotateLever(pivot, initialLocalRotation, pressedRotation, pressDuration);

        if (ballManager != null)
        {
            Debug.Log("[SpinLever] Launching balls.");
            ballManager.LaunchAllBalls();
        }

        yield return RotateLever(pivot, pressedRotation, initialLocalRotation, releaseDuration);

        ResetLever();
    }

    private IEnumerator RotateLever(Transform pivot, Quaternion from, Quaternion to, float duration)
    {
        if (duration <= 0f)
        {
            pivot.localRotation = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            pivot.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        pivot.localRotation = to;
    }

    private void ResetLever()
    {
        Debug.Log("[SpinLever] Resetting lever.");
        isSpinning = false;
        leverAnimationRoutine = null;
        ApplyBaseColor();
    }

    private void RefreshVisualState()
    {
        bool canHighlight = !isSpinning &&
                            isHovering &&
                            GameManager.Instance != null &&
                            GameManager.Instance.state == BattleState.WaitingForPlayer &&
                            ballManager != null &&
                            ballManager.currentBalls > 0;

        SetHighlight(canHighlight);
    }

    private void SetHighlight(bool highlighted)
    {
        Renderer rendererToUse = ActiveRenderer;
        if (rendererToUse == null)
        {
            return;
        }

        rendererToUse.material.color = highlighted ? hoverColor : originalColor;
    }

    private void ApplyBaseColor()
    {
        RefreshVisualState();
    }
}
