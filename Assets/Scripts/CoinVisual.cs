using UnityEngine;
using System.Collections.Generic;

public class CoinVisual : MonoBehaviour
{
    public static CoinVisual Instance;

    public GameObject coinPrefab;

    public Transform spawnArea;
    public Vector2 spawnAreaSize = new Vector2(5f, 3f);
    public float spawnHeight = 0.5f;
    public int maxCoins = 50;
    public float updateDelay = 0.5f;

    private List<GameObject> activeCoins = new List<GameObject>();
    private int lastCoinCount = 0;
    private float lastUpdateTime = 0f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (Time.time - lastUpdateTime < updateDelay)
            return;

        lastUpdateTime = Time.time;

        Player player = FindObjectOfType<Player>();
        if (player == null) return;

        int targetCoinCount = Mathf.Min(player.money, maxCoins);

        if (targetCoinCount != lastCoinCount)
        {
            UpdateCoinVisuals(targetCoinCount);
            lastCoinCount = targetCoinCount;
        }
    }

    void UpdateCoinVisuals(int targetCount)
    {
        while (activeCoins.Count > targetCount)
        {
            int lastIndex = activeCoins.Count - 1;
            GameObject coin = activeCoins[lastIndex];
            activeCoins.RemoveAt(lastIndex);
            Destroy(coin);
        }

        while (activeCoins.Count < targetCount)
        {
            SpawnCoin();
        }
    }

    void SpawnCoin()
    {
        if (coinPrefab == null) return;

        Vector3 spawnPos;
        if (spawnArea != null)
        {
            spawnPos = spawnArea.position;
            spawnPos.x += Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
            spawnPos.z += Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);
            spawnPos.y += 3f;
        }
        else
        {
            spawnPos = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                3f,
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
            );
        }

        GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.Euler(90, Random.Range(0, 360), 0));
        Rigidbody rb = coin.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), ForceMode.Impulse);
        }

        activeCoins.Add(coin);
    }

    public void ForceUpdate()
    {
        lastCoinCount = -1;
    }
}