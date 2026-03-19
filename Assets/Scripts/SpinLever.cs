using UnityEngine;

public class SpinLever : MonoBehaviour
{
    [Header("References")]
    public BallManager ballManager;
    public Transform leverPivot;

    [Header("Debug")]
    [SerializeField] private bool isSpinning;

    private Quaternion initialLocalRotation;
    private Animator animator;

    private Transform ActivePivot => leverPivot != null ? leverPivot : transform;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[SpinLever] Animator component not found!");
        }

        initialLocalRotation = ActivePivot.localRotation;

        if (ballManager == null)
        {
            ballManager = FindObjectOfType<BallManager>();
        }
    }

    private void OnMouseDown()
    {
        OnLeverClick();
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.enabled = false;
            animator.enabled = true;
        }

        ActivePivot.localRotation = initialLocalRotation;
        isSpinning = false;
    }

    private void OnLeverClick()
    {
        if (isSpinning) return;

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.state != BattleState.WaitingForPlayer) return;

        if (ballManager == null) return;
        if (ballManager.currentBalls <= 0) return;

        ActivateLever();
    }

    private void ActivateLever()
    {
        isSpinning = true;

        if (animator != null)
        {
            animator.SetTrigger("Pull");
        }
    }

    public void OnAnimationComplete()
    {
        isSpinning = false;
    }

    public void OnLeverPressed()
    {
        if (ballManager != null)
        {
            ballManager.LaunchAllBalls();
        }
    }
}