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

    [Header("Progression")]
    public int currentWave = 1;
    public int totalWaves = 15;

    [Header("Shop Transition")]
    public float shopOpenDelay = 0.8f;
    public float shopCloseDelay = 0.8f;

    private int pendingBalls = 0;
    private int defeatedEnemies = 0;

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
        int damage = 8;
        player.TakeDamage(damage);
    }

    private void EnemyDefend()
    {
        Enemy target = GetRandomLivingEnemy();
        if (target == null)
            return;

        int shield = 5;
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
        defeatedEnemies = 0;
        currentWave++;

        if (currentWave > totalWaves)
            Victory();
        else
        {
            CloseShop();
            SpawnEnemiesForWave(currentWave);
        }
    }

    public void SpawnEnemiesForWave(int waveNumber)
    {
        if (enemies == null || enemies.Length == 0)
            return;

        int enemiesCount = Mathf.Min(waveNumber, enemies.Length);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            bool shouldBeActive = i < enemiesCount;
            enemies[i].gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
                continue;

            enemies[i].health = enemies[i].maxHealth;
            enemies[i].shield = 0;
        }
    }

    public void AddBalls(int count)
    {
        BallManager ballManager = FindObjectOfType<BallManager>();
        if (ballManager != null)
            ballManager.AddBalls(count);
    }
}