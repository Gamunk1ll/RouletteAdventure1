using UnityEngine;

public class BallManager : MonoBehaviour
{
    public int currentBalls = 3;
    public RouletteManager roulette;

    private bool isSpinning = false;

    public void LaunchAllBalls()
    {
        if (isSpinning)
            return;

        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.WaitingForPlayer)
            return;

        if (currentBalls <= 0)
            return;

        if (roulette == null)
        {
            Debug.LogError("BallManager: RouletteManager reference is missing.");
            return;
        }

        isSpinning = true;

        int ballsToLaunch = currentBalls;
        currentBalls = 0;

        GameManager.Instance.StartPlayerTurn(ballsToLaunch);

        for (int i = 0; i < ballsToLaunch; i++)
        {
            if (!roulette.TriggerRandomSlot())
            {
                Debug.LogWarning("BallManager: failed to trigger slot, resolving as empty.");
                GameManager.Instance.ResolveEmptySlot();
            }
        }

        isSpinning = false;
    }

    public void AddBalls(int amount)
    {
        currentBalls += amount;
    }
}
