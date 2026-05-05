using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject collectiblePrefab;
    public Transform player;

    // ui 
    public TMP_Text episodeText;
    public TMP_Text stepsText;
    public TMP_Text timerText;
    public TMP_Text bestTimeText;
    public TMP_Text rewardsText;
    public TMP_Text remainingText;

    // game settings
    public int numberOfCollectibles = 20;
    public int maxStepsPerEpisode = 1000;
    public Vector3 playerStartPosition = new Vector3(-0.745f, 0.15f, -1.54f);

    // spawn settings
    public float collectibleY = 0.22f;
    public float minDistanceFromPlayer = 1.2f;
    public float minDistanceBetweenCollectibles = 0.7f;
    public int maxSpawnAttemptsPerObject = 50;

    private readonly List<Collectible> activeCollectibles = new List<Collectible>();
    private ResettableObject[] resettableObjects;

    private int currentEpisode = 0;
    private int currentSteps = 0;
    private float currentRewards = 0f;

    private bool gameRunning = false;
    private float elapsedTime = 0f;
    private float bestTime = -1f;

    private VacuumAgent _agent;

    private void Awake()
    {
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        _agent = FindFirstObjectByType<VacuumAgent>();

        resettableObjects = FindObjectsByType<ResettableObject>(FindObjectsSortMode.None);
    }

    private void Start()
    {
        // StartNewEpisode();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartNewEpisode();
        }

        if (!gameRunning)
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    public void StartNewEpisode()
    {
        currentEpisode++;
        currentSteps = 0;
        currentRewards = 0f;
        elapsedTime = 0f;
        gameRunning = true;

        ClearCollectibles();
        ResetPlayer();
        ResetSceneObjects();
        SpawnCollectibles();
        UpdateAllUI();
    }

    private void ResetPlayer()
    {
        if (player == null)
        {
            Debug.LogError("Player reference missing in GameManager.");
            return;
        }

        player.position = playerStartPosition;
        player.rotation = Quaternion.identity;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ResetSceneObjects()
    {
        foreach (ResettableObject obj in resettableObjects)
        {
            if (obj != null)
                obj.ResetObject();
        }
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
                {
                    collectible.SetGameManager(this);
                    activeCollectibles.Add(collectible);
                }

                usedPositions.Add(randomPos);
                spawned = true;
                break;
            }

            if (!spawned)
            {
                Debug.LogWarning($"Could not spawn collectible #{i + 1}.");
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
                return new Vector3(x, collectibleY, z);
        }
    }

    private bool IsInsideRoomShape(float x, float z)
    {
        bool inMainRect = x >= -5f && x <= 3f && z >= -3.7f && z <= 0f;
        bool inTopRect = x >= -2.5f && x <= 3f && z >= 0f && z <= 1.83f;

        return inMainRect || inTopRect;
    }

    public void CollectObject(Collectible collectible)
    {
        if (!gameRunning || collectible == null)
            return;

        if (!activeCollectibles.Contains(collectible))
            return;

        activeCollectibles.Remove(collectible);
        Destroy(collectible.gameObject);

        currentRewards += 1f;

        UpdateAllUI();

        if (activeCollectibles.Count == 0)
        {
            EndEpisode(true);
        }
    }

    private void EndEpisode(bool won)
    {
        gameRunning = false;

        if (won)
        {
            // big reward for finishing
            if (_agent != null)
                _agent.AddReward(100f);

            Debug.Log("Episode finished successfully!");

            if (bestTime < 0f || elapsedTime < bestTime)
                bestTime = elapsedTime;
        }
        else
        {
            // optional penalty
            //if (_agent != null)
            //    _agent.AddReward(-10f);
        }

        UpdateAllUI();

        // tell ML-Agents episode is done
        if (_agent != null)
            _agent.EndEpisode();
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

    public void RegisterStep()
    {
        if (!gameRunning)
            return;

        currentSteps++;

        if (currentSteps >= maxStepsPerEpisode)
        {
            EndEpisode(false);
        }

        UpdateAllUI();
    }

    private void UpdateAllUI()
    {
        UpdateEpisodeUI();
        UpdateStepsUI();
        UpdateTimerUI();
        UpdateBestTimeUI();
        UpdateRewardsUI();
        UpdateRemainingUI();
    }

    private void UpdateEpisodeUI()
    {
        if (episodeText != null)
            episodeText.text = $"Episode: {currentEpisode}";
    }

    private void UpdateStepsUI()
    {
        if (stepsText != null)
            stepsText.text = $"Steps: {currentSteps}/{maxStepsPerEpisode}";
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = $"Time: {FormatTime(elapsedTime)}";
    }

    private void UpdateBestTimeUI()
    {
        if (bestTimeText != null)
            bestTimeText.text = bestTime < 0f ? "Best time: --:--" : $"Best time: {FormatTime(bestTime)}";
    }

    private void UpdateRewardsUI()
    {
        if (rewardsText != null)
            rewardsText.text = $"Rewards: {currentRewards:0.00}";
    }

    private void UpdateRemainingUI()
    {
        if (remainingText != null)
            remainingText.text = $"Remaining objects: {activeCollectibles.Count}";
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        return $"{minutes:00}:{seconds:00}";
    }

    public bool IsGameActive()
    {
        return gameRunning;
    }
}