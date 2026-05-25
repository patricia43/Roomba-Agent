using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ResettableObject : MonoBehaviour
{
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody rb;

    public GameManager gameManager;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        rb = GetComponent<Rigidbody>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void ResetObject()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.Sleep();
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerCollision(collision);
    }

    public void HandlePlayerCollision(Collision collision)
    {
        VacuumAgent agent = collision.collider.GetComponentInParent<VacuumAgent>();

        if (agent == null)
        {
            // Debug.Log("Collision ignored, no VacuumAgent found on " + collision.collider.name, this);
            return;
        }

        gameManager.RegisterFurnitureHit();

        float displacement = Vector3.Distance(transform.position, startPosition);

        float basePenalty = -0.5f;
        float displacementPenalty = -3f * displacement;
        float totalPenalty = basePenalty + displacementPenalty;

        agent.AddReward(totalPenalty);

        if (gameManager != null)
        {
            gameManager.currentRewards += totalPenalty;
            gameManager.UpdateAllUI();
        }

        Debug.Log($"Furniture hit: {name}, displacement={displacement:0.00}, penalty={totalPenalty:0.00}", this);
    }
}