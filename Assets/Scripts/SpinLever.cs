using System.Collections;
using UnityEngine;

public class SpinLever : MonoBehaviour
{
    public BallManager ballManager;
    public Transform leverPivot;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float fallbackLaunchDelay = 0.2f;
    [SerializeField, Min(0f)] private float fallbackResetDelay = 0.8f;
    [SerializeField] private bool isSpinning;

    // === ÍÎÂÛÅ ÏÎËß ÄËß ÑÂÅ×ÅÍÈß ===
    [Header("Glow Settings")]
    [SerializeField] private float glowIntensity = 3f;
    [SerializeField] private Color glowColor = Color.yellow;

    private Renderer objectRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int emissionID;
    // =================================

    private Quaternion initialLocalRotation;
    private Coroutine fallbackLaunchRoutine;
    private Coroutine fallbackResetRoutine;
    private bool launchTriggered;

    private Transform ActivePivot => leverPivot != null ? leverPivot : transform;

    private void Awake()
    {
        ResolveReferences();
        initialLocalRotation = ActivePivot.localRotation;
        InitGlow();
    }

    private void Start()
    {
        ResolveReferences();
    }
    private void InitGlow()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
            objectRenderer = GetComponentInChildren<Renderer>();

        propertyBlock = new MaterialPropertyBlock();
        emissionID = Shader.PropertyToID("_EmissionColor");
        SetGlow(false);
    }

    private void OnMouseEnter()
    {
        SetGlow(true);
    }

    private void OnMouseExit()
    {
        SetGlow(false);
    }

    private void SetGlow(bool enable)
    {
        if (objectRenderer == null) return;

        objectRenderer.GetPropertyBlock(propertyBlock);

        Color emissionColor = enable ? glowColor * glowIntensity : Color.black;
        propertyBlock.SetColor(emissionID, emissionColor);

        objectRenderer.SetPropertyBlock(propertyBlock);
    }

    public void TriggerGlowAnimation()
    {
        SetGlow(true);
    }

    public void StopGlowAnimation()
    {
        SetGlow(false);
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

        if (fallbackResetRoutine != null)
        {
            StopCoroutine(fallbackResetRoutine);
            fallbackResetRoutine = null;
        }

        ActivePivot.localRotation = initialLocalRotation;
        isSpinning = false;
        launchTriggered = false;

        SetGlow(false);
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

        if (fallbackResetRoutine != null)
            StopCoroutine(fallbackResetRoutine);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger("Pull");
            animator.SetTrigger("Pull");
            fallbackLaunchRoutine = StartCoroutine(FallbackLaunchRoutine());
            fallbackResetRoutine = StartCoroutine(FallbackResetRoutine());
            return;
        }
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

    private IEnumerator FallbackResetRoutine()
    {
        if (fallbackResetDelay > 0f)
            yield return new WaitForSeconds(fallbackResetDelay);

        fallbackResetRoutine = null;
        OnAnimationComplete();
    }

    public void OnAnimationComplete()
    {
        if (fallbackLaunchRoutine != null)
        {
            StopCoroutine(fallbackLaunchRoutine);
            fallbackLaunchRoutine = null;
        }

        if (fallbackResetRoutine != null)
        {
            StopCoroutine(fallbackResetRoutine);
            fallbackResetRoutine = null;
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