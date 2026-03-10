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

        if (GameManager.Instance.state != BattleState.WaitingForPlayer)
            return;

        if (currentBalls <= 0)
            return;

        isSpinning = true;

        int ballsToLaunch = currentBalls;
        currentBalls = 0;

        GameManager.Instance.StartPlayerTurn(ballsToLaunch);

        for (int i = 0; i < ballsToLaunch; i++)
        {
            roulette.TriggerRandomSlot();
        }

        isSpinning = false;
    }
    public void AddBalls(int amount)
    {
        currentBalls += amount;
    }
}
