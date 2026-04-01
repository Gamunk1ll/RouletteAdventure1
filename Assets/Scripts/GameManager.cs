using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BattleState
{
    WaitingForPlayer,
    PlayerTurn,
    EnemyTurn,
    Shop,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Player player;
    public Enemy[] enemies;

    public BattleState state = BattleState.WaitingForPlayer;
    public GameObject shopPanel;
    public GameObject battleUI;
    public GameObject victoryPanel;
    public Shop shop;
    public BattleShopStageController stageController;

    public int currentWave = 1;
    public int totalWaves = 15;
    public int waveClearRewardBase = 8;
    public int waveClearRewardPerWave = 2;
    public int enemyHealthGrowthPerWave = 12;
    public int enemyAttackBase = 8;
    public int enemyAttackGrowthPerWave = 2;
    public int enemyShieldBase = 5;
    public int enemyShieldGrowthPerWave = 1;
    [Min(1)] public int minEnemiesPerWave = 1;
    [Min(1)] public int maxEnemiesPerWave = 3;

    public float shopOpenDelay = 0.8f;
    public float shopCloseDelay = 0.8f;

    private int pendingBalls = 0;
    private int defeatedEnemies = 0;
    private bool waveRewardGranted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (enemies == null || enemies.Length == 0)
            enemies = FindObjectsOfType<Enemy>(true);

        if (shop == null)
            shop = FindObjectOfType<Shop>(true);

        if (stageController == null)
            stageController = FindObjectOfType<BattleShopStageController>(true);

        if (shopPanel != null) shopPanel.SetActive(false);
        if (shop != null) shop.Close();
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (battleUI != null) battleUI.SetActive(true);

        if (stageController != null)
            stageController.ApplyBattleStateImmediate();

        SpawnEnemiesForWave(currentWave);
    }

    public void StartPlayerTurn(int ballsCount)
    {
        if (state != BattleState.WaitingForPlayer)
            return;

        state = BattleState.PlayerTurn;
        pendingBalls = ballsCount;
    }

    public void ResolveSector(BaseSector sector)
    {
        if (state != BattleState.PlayerTurn)
            return;

        int power = sector.GetPower();

        switch (sector.data.Type)
        {
            case SectorType.Attack:
                Enemy target = GetRandomLivingEnemy();
                if (target != null)
                {
                    target.TakeDamage(power);

                    if (target.IsDead())
                    {
                        defeatedEnemies++;
                        CheckWaveComplete();
                    }
                }
                break;

            case SectorType.Shield:
                player.AddShield(power);
                break;

            case SectorType.Heal:
                player.Heal(power);
                break;

            case SectorType.Money:
                player.AddMoney(power);
                break;
        }

        FinishBall();
    }

    public void ResolveEmptySlot()
    {
        if (state != BattleState.PlayerTurn)
            return;

        FinishBall();
    }

    private void FinishBall()
    {
        pendingBalls--;

        if (pendingBalls <= 0)
            EndPlayerTurn();
    }

    private void EndPlayerTurn()
    {
        if (CheckAllEnemiesDefeated())
        {
            OpenShop();
            return;
        }

        state = BattleState.EnemyTurn;
        Invoke(nameof(EnemyTurn), 1f);
    }

    private void CheckWaveComplete()
    {
        if (!CheckAllEnemiesDefeated())
            return;

        Invoke(nameof(OpenShop), 1f);
    }

    private bool CheckAllEnemiesDefeated()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead())
                return false;
        }
        return true;
    }

    private Enemy GetRandomLivingEnemy()
    {
        List<Enemy> livingEnemies = new();

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead())
                livingEnemies.Add(enemy);
        }

        if (livingEnemies.Count > 0)
            return livingEnemies[Random.Range(0, livingEnemies.Count)];

        return null;
    }

    private void EnemyTurn()
    {
        if (CheckAllEnemiesDefeated())
        {
            OpenShop();
            return;
        }

        int dice = Random.Range(1, 7);

        if (dice <= 3)
            EnemyAttack();
        else
            EnemyDefend();

        if (player.GetHealth() <= 0)
        {
            GameOver();
            return;
        }

        EndBattleCycle();
    }

    private void EnemyAttack()
    {
        int damage = Mathf.Max(0, enemyAttackBase + (currentWave - 1) * enemyAttackGrowthPerWave);
        player.TakeDamage(damage);
    }

    private void EnemyDefend()
    {
        Enemy target = GetRandomLivingEnemy();
        if (target == null)
            return;

        int shield = Mathf.Max(0, enemyShieldBase + (currentWave - 1) * enemyShieldGrowthPerWave);
        target.AddShield(shield);
    }

    private void EndBattleCycle()
    {
        state = BattleState.WaitingForPlayer;
        AddBalls(3);
    }

    public void OpenShop()
    {
        StopAllCoroutines();
        StartCoroutine(OpenShopRoutine());
    }

    public void CloseShop()
    {
        StopAllCoroutines();
        StartCoroutine(CloseShopRoutine());
    }

    private IEnumerator OpenShopRoutine()
    {
        state = BattleState.Shop;

        GrantWaveClearRewardOnce();

        if (battleUI != null)
            battleUI.SetActive(false);

        if (shopPanel != null)
            shopPanel.SetActive(true);

        if (stageController != null)
            stageController.FlipToShop();

        if (shopOpenDelay > 0f)
            yield return new WaitForSeconds(shopOpenDelay);

        if (shop != null)
            shop.Open();
    }

    private IEnumerator CloseShopRoutine()
    {
        if (shop != null)
            shop.Close();

        if (stageController != null)
            stageController.FlipToBattle();

        if (shopCloseDelay > 0f)
            yield return new WaitForSeconds(shopCloseDelay);

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (battleUI != null)
            battleUI.SetActive(true);

        if (!PrepareNextWave())
            yield break;

        state = BattleState.WaitingForPlayer;
        AddBalls(3);
    }

    public void Victory()
    {
        state = BattleState.Victory;

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (battleUI != null)
            battleUI.SetActive(false);
    }

    public void GameOver()
    {
        state = BattleState.Defeat;
    }

    public void NextLevel()
    {
        CloseShop();
    }

    public void SpawnEnemiesForWave(int waveNumber)
    {
        if (enemies == null || enemies.Length == 0)
            return;

        waveRewardGranted = false;

        int minCount = Mathf.Clamp(minEnemiesPerWave, 1, enemies.Length);
        int maxCount = Mathf.Clamp(maxEnemiesPerWave, minCount, enemies.Length);
        int enemiesCount = waveNumber == 1
            ? 1
            : Random.Range(minCount, maxCount + 1);

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                availableIndices.Add(i);
        }

        enemiesCount = Mathf.Min(enemiesCount, availableIndices.Count);
        HashSet<int> activeIndices = new HashSet<int>();

        for (int i = 0; i < enemiesCount; i++)
        {
            int randomPick = Random.Range(0, availableIndices.Count);
            activeIndices.Add(availableIndices[randomPick]);
            availableIndices.RemoveAt(randomPick);
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            bool shouldBeActive = activeIndices.Contains(i);
            enemies[i].gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
                continue;

            enemies[i].ResetForWave(waveNumber, enemyHealthGrowthPerWave);
        }
    }

    public void AddBalls(int count)
    {
        BallManager ballManager = FindObjectOfType<BallManager>();
        if (ballManager != null)
            ballManager.AddBalls(count);
    }

    private void GrantWaveClearRewardOnce()
    {
        if (waveRewardGranted || player == null)
            return;

        int reward = Mathf.Max(0, waveClearRewardBase + (currentWave - 1) * waveClearRewardPerWave);
        if (reward > 0)
            player.AddMoney(reward);

        waveRewardGranted = true;
    }

    private bool PrepareNextWave()
    {
        defeatedEnemies = 0;
        currentWave++;

        if (currentWave > totalWaves)
        {
            Victory();
            return false;
        }

        SpawnEnemiesForWave(currentWave);
        return true;
    }
}