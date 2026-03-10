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

    public int currentWave = 1;
    public int totalWaves = 15;

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

    void Start()
    {
        if (enemies == null || enemies.Length == 0)
        {
            enemies = FindObjectsOfType<Enemy>();
        }
        if (shopPanel != null) shopPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (battleUI != null) battleUI.SetActive(true);
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
                    Debug.Log($"Игрок атакует врага: {power} урона");

                    if (target.IsDead())
                    {
                        defeatedEnemies++;
                        Debug.Log($"Враг повержен! Осталось: {enemies.Length - defeatedEnemies}");
                        CheckWaveComplete();
                    }
                }
                break;

            case SectorType.Shield:
                player.AddShield(power);
                Debug.Log($"Игрок получает щит: {power}");
                break;

            case SectorType.Heal:
                player.Heal(power);
                Debug.Log($"Игрок лечится: {power}");
                break;

            case SectorType.Money:
                player.AddMoney(power);
                Debug.Log($"Игрок получает деньги: {power}");
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
        {
            EndPlayerTurn();
        }
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
        if (CheckAllEnemiesDefeated())
        {
            Debug.Log("Волна завершена!");
            Invoke(nameof(OpenShop), 1f);
        }
    }

    private bool CheckAllEnemiesDefeated()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
            {
                return false;
            }
        }
        return true;
    }

    private Enemy GetRandomLivingEnemy()
    {
        System.Collections.Generic.List<Enemy> livingEnemies = new System.Collections.Generic.List<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
            {
                livingEnemies.Add(enemy);
            }
        }

        if (livingEnemies.Count > 0)
        {
            return livingEnemies[Random.Range(0, livingEnemies.Count)];
        }

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

        if (player.health <= 0)
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
        Debug.Log($"Враг атакует на {damage}");
    }

    private void EnemyDefend()
    {
        Enemy target = GetRandomLivingEnemy();
        if (target != null)
        {
            int shield = 5;
            target.AddShield(shield);
            Debug.Log($"Враг усиливает защиту на {shield}");
        }
    }

    private void EndBattleCycle()
    {
        state = BattleState.WaitingForPlayer;
        AddBalls(3);
    }

    public void OpenShop()
    {
        state = BattleState.Shop;

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        if (battleUI != null)
        {
            battleUI.SetActive(false);
        }

        Debug.Log("Магазин открыт");
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (battleUI != null)
        {
            battleUI.SetActive(true);
        }

        state = BattleState.WaitingForPlayer;
        AddBalls(3);
        Debug.Log("Магазин закрыт. Новая волна!");
    }

    public void Victory()
    {
        state = BattleState.Victory;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (battleUI != null)
        {
            battleUI.SetActive(false);
        }

        Debug.Log("ПОБЕДА!");
    }

    public void GameOver()
    {
        state = BattleState.Defeat;
        Debug.Log("ИГРОК ПОГИБ");
    }

    public void NextLevel()
    {
        defeatedEnemies = 0;
        currentWave++;

        if (currentWave > totalWaves)
        {
            Victory();
        }
        else
        {
            CloseShop();
            SpawnEnemiesForWave(currentWave);
        }

        Debug.Log($"Переход на волну {currentWave}");
    }

    public void SpawnEnemiesForWave(int waveNumber)
    {
        int enemiesCount = Mathf.Min(waveNumber, enemies.Length);

        for (int i = 0; i < enemiesCount; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].gameObject.SetActive(true);
                enemies[i].health = enemies[i].maxHealth;
                enemies[i].shield = 0;
            }
        }
    }

    public void AddBalls(int count)
    {
        BallManager ballManager = FindObjectOfType<BallManager>();
        if (ballManager != null)
        {
            ballManager.AddBalls(count);
        }
    }
}