using System.Collections;
using UnityEngine;

public class SpinLever : MonoBehaviour
{
    [Header("References")]
    public BallManager ballManager;
    public Transform leverPivot;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float fallbackLaunchDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool isSpinning;

    private Quaternion initialLocalRotation;
    private Coroutine fallbackLaunchRoutine;
    private bool launchTriggered;

    private Transform ActivePivot => leverPivot != null ? leverPivot : transform;

    private void Awake()
    {
        ResolveReferences();
        initialLocalRotation = ActivePivot.localRotation;
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void OnMouseDown()
    {
        OnLeverClick();
    }

    private void OnDisable()
    {
        if (fallbackLaunchRoutine != null)
        {
            StopCoroutine(fallbackLaunchRoutine);
            fallbackLaunchRoutine = null;
        }

        ActivePivot.localRotation = initialLocalRotation;
        isSpinning = false;
        launchTriggered = false;
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (ballManager == null)
            ballManager = FindObjectOfType<BallManager>();
    }

    private void OnLeverClick()
    {
        if (isSpinning)
            return;

        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.WaitingForPlayer)
            return;

        if (ballManager == null)
        {
            Debug.LogError("[SpinLever] BallManager reference is missing.");
            return;
        }

        if (ballManager.currentBalls <= 0)
            return;

        ActivateLever();
    }

    private void ActivateLever()
    {
        isSpinning = true;
        launchTriggered = false;

        if (fallbackLaunchRoutine != null)
            StopCoroutine(fallbackLaunchRoutine);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger("Pull");
            animator.SetTrigger("Pull");
            fallbackLaunchRoutine = StartCoroutine(FallbackLaunchRoutine());
            return;
        }

        Debug.LogWarning("[SpinLever] Animator was not found, starting roulette without lever animation.");
        OnLeverPressed();
        OnAnimationComplete();
    }

    private IEnumerator FallbackLaunchRoutine()
    {
        if (fallbackLaunchDelay > 0f)
            yield return new WaitForSeconds(fallbackLaunchDelay);

        fallbackLaunchRoutine = null;

        if (!launchTriggered)
            OnLeverPressed();
    }

    public void OnAnimationComplete()
    {
        if (fallbackLaunchRoutine != null)
        {
            StopCoroutine(fallbackLaunchRoutine);
            fallbackLaunchRoutine = null;
        }

        isSpinning = false;
        launchTriggered = false;
    }

    public void OnLeverPressed()
    {
        if (launchTriggered)
            return;

        launchTriggered = true;

        if (ballManager != null)
            ballManager.LaunchAllBalls();
    }
}
