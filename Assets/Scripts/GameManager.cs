using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject collectiblePrefab;
    public Transform player;
    public TMP_Text collectedText;
    public TMP_Text remainingText;
    public TMP_Text timerText;

    public int numberOfCollectibles = 20;
    public Vector3 playerStartPosition = new Vector3(-0.745f, 0.15f, -1.54f);

    public float collectibleY = 0.12f;
    public float minDistanceFromPlayer = 1.2f;
    public float minDistanceBetweenCollectibles = 0.7f;
    public int maxSpawnAttemptsPerObject = 50;

    private List<Collectible> activeCollectibles = new List<Collectible>();

    private int collectedCount = 0;
    private bool gameRunning = false;
    private float elapsedTime = 0f;

    private void Awake()
    {
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        // Debug.Log($"GameManager Awake on: {gameObject.name}", this);

    }

    private void Start()
    {
        Debug.Log($"GameManager Start on: {gameObject.name} | player={(player ? player.name : "NULL")} | collectiblePrefab={(collectiblePrefab ? collectiblePrefab.name : "NULL")}", this);
        StartNewGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartNewGame();
        }

        if (gameRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void StartNewGame()
    {
        ClearCollectibles();

        // reset player
        player.position = playerStartPosition;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.rotation = Quaternion.identity;
        }

        collectedCount = 0;
        elapsedTime = 0f;
        gameRunning = true;

        SpawnCollectibles();
        UpdateUI();
        UpdateTimerUI();
    }

    private void SpawnCollectibles()
    {
        List<Vector3> usedPositions = new List<Vector3>();

        for (int i = 0; i < numberOfCollectibles; i++)
        {
            bool spawned = false;

            for (int attempt = 0; attempt < maxSpawnAttemptsPerObject; attempt++)
            {
                Vector3 randomPos = GetRandomValidPoint();

                if (Vector3.Distance(randomPos, playerStartPosition) < minDistanceFromPlayer)
                    continue;

                bool tooClose = false;
                foreach (Vector3 usedPos in usedPositions)
                {
                    if (Vector3.Distance(randomPos, usedPos) < minDistanceBetweenCollectibles)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                GameObject obj = Instantiate(collectiblePrefab, randomPos, Quaternion.identity);
                Collectible collectible = obj.GetComponent<Collectible>();

                if (collectible != null)
                    collectible.SetGameManager(this);

                activeCollectibles.Add(collectible);
                usedPositions.Add(randomPos);
                spawned = true;
                break;
            }

            if (!spawned)
            {
                Debug.LogWarning($"Nu am putut genera obiectul #{i + 1} dupa prea multe incercari.");
            }
        }
    }

    private Vector3 GetRandomValidPoint()
    {
        while (true)
        {
            float x = Random.Range(-4.3f, 3f);
            float z = Random.Range(-3.6f, 1.73f);

            if (IsInsideRoomShape(x, z))
            {
                return new Vector3(x, collectibleY, z);
            }
        }
    }

    private bool IsInsideRoomShape(float x, float z)
    {
        bool inMainRect = (x >= -5f && x <= 3f && z >= -3.7f && z <= 0f);
        bool inTopRect = (x >= -2.5f && x <= 3f && z >= 0f && z <= 1.83f);

        return inMainRect || inTopRect;
    }

    public void CollectObject(Collectible collectible)
    {
        if (!gameRunning || collectible == null)
            return;

        if (activeCollectibles.Contains(collectible))
        {
            activeCollectibles.Remove(collectible);
            collectedCount++;
            Destroy(collectible.gameObject);

            UpdateUI();

            if (activeCollectibles.Count == 0)
            {
                gameRunning = false;
                Debug.Log("Joc terminat!");
            }
        }
    }

    private void ClearCollectibles()
    {
        foreach (Collectible collectible in activeCollectibles)
        {
            if (collectible != null)
                Destroy(collectible.gameObject);
        }

        activeCollectibles.Clear();
    }

    private void UpdateUI()
    {
        if (collectedText != null)
            collectedText.text = $"Obiecte colectate: {collectedCount}";

        if (remainingText != null)
            remainingText.text = $"Obiecte ramase: {activeCollectibles.Count}";
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        timerText.text = $"Timp: {minutes:00}:{seconds:00}";
    }
}