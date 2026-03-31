using System.Collections;
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
            return;
        }

        int ballsToLaunch = currentBalls;
        currentBalls = 0;

        GameManager.Instance.StartPlayerTurn(ballsToLaunch);
        StartCoroutine(LaunchBallsRoutine(ballsToLaunch));
    }

    private IEnumerator LaunchBallsRoutine(int ballsToLaunch)
    {
        isSpinning = true;

        if (ballsToLaunch <= 0)
        {
            isSpinning = false;
            yield break;
        }
        if (roulette.wheelRoot != null)
        {
            yield return roulette.SpinVisualOnly();
        }
        for (int i = 0; i < ballsToLaunch; i++)
        {
            if (!roulette.TriggerRandomSlot())
                GameManager.Instance.ResolveEmptySlot();
        }

        isSpinning = false;
    }

    public void AddBalls(int amount)
    {
        currentBalls += amount;
    }
}
