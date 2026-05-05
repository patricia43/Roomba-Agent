using UnityEngine;

public class Collectible : MonoBehaviour
{
    public GameManager gameManager;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        VacuumAgent agent = other.GetComponent<VacuumAgent>();

        if (agent != null)
            agent.AddReward(1.0f);

        if (gameManager != null)
            gameManager.CollectObject(this);
    }
}