using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class VacuumAgent : Agent
{
    [SerializeField] public float _moveSpeed = 3f;
    [SerializeField] public float _rotationSpeed = 200f;

    protected Rigidbody _rigidbody;

    protected Vector3 _movement = Vector3.zero;
    protected float _rotation = 0f;

    protected GameManager _gameManager;

    // detection settings
    public float sphereRadius = 0.2f;
    public float detectionDistance = 8f;
    public int numberOfDirections = 32;
    // public LayerMask obstacleLayerMask = 1 << 6;
    // public LayerMask obstacleLayerMask = ~0;
    public LayerMask layerMask;

    // det results
    protected float[] distances;
    protected float[] furnitureDistances;
    protected bool[] hasHit;
    protected RaycastHit[] hits;
    private float previousNearestDirtDistance;

    // debugging
    public bool debugDrawHit = true;
    public bool debugDrawNoHit = false;

    // initialize agent
    public override void Initialize()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _rigidbody = GetComponent<Rigidbody>();

        distances = new float[numberOfDirections];
        furnitureDistances = new float[numberOfDirections];
        hasHit = new bool[numberOfDirections];
        hits = new RaycastHit[numberOfDirections];
        Debug.Log("VacuumAgentInitialized");

        Physics.queriesHitTriggers = true;
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent / scene at ep start
        _gameManager.StartNewEpisode();

        previousNearestDirtDistance = GetNearestDirtDistance();

        Debug.Log("Episode begun");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        DetectSurroundings();

        for (int i = 0; i < numberOfDirections; i++)
        {
            sensor.AddObservation(distances[i] / detectionDistance);
            sensor.AddObservation(furnitureDistances[i] / detectionDistance);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Default: no movement, no rotation
        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;

        // Branch 0: move
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1; // move forward
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2; // backward
        }

        // Branch 1: rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1; // rotate left
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2; // rotate right
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_gameManager != null && !_gameManager.IsGameActive())
            return;

        if (_gameManager != null)
            _gameManager.RegisterStep();

        int moveAction = actions.DiscreteActions[0];
        int rotateAction = actions.DiscreteActions[1];

        _movement = Vector3.zero;
        _rotation = 0f;

        // movement
        if (moveAction == 1)
        {
            // forward
            _movement = transform.forward * _moveSpeed;
        }
        else if (moveAction == 2)
        {
            // backward
            _movement = -transform.forward * _moveSpeed;
        }

        if (rotateAction == 1)
        {
            _rotation = -_rotationSpeed;
        }
        else if (rotateAction == 2)
        {
            _rotation = _rotationSpeed;
        }

        AddReward(-0.01f);
        // update reward in ui
        _gameManager.currentRewards -= 0.01f;
        _gameManager.UpdateAllUI();
    }

    private void FixedUpdate()
    {
        Vector3 velocity = _movement;
        velocity.y = _rigidbody.linearVelocity.y;

        _rigidbody.linearVelocity = velocity;

        if (_rotation != 0f)
        {
            Quaternion deltaRotation = Quaternion.Euler(
                0f,
                _rotation * Time.fixedDeltaTime,
                0f
            );

            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
        }

        _movement = Vector3.zero;
        _rotation = 0f;

        _rigidbody.angularVelocity = Vector3.zero;
    }

    private void Update()
    {
        DetectSurroundings();
    }

    private void DetectSurroundings()
    {
        Vector3 sphereOrigin = transform.position + Vector3.up * 0.2f;

        for (int i = 0; i < numberOfDirections; i++)
        {
            float angle = (360f / numberOfDirections) * i;
            float radian = angle * Mathf.Deg2Rad;

            Vector3 localDirection = new Vector3(
                Mathf.Cos(radian),
                0f,
                Mathf.Sin(radian)
            );

            Vector3 worldDirection =
                transform.TransformDirection(localDirection).normalized;

            distances[i] = detectionDistance;
            furnitureDistances[i] = detectionDistance;

            RaycastHit[] allHits = Physics.SphereCastAll(
                sphereOrigin,
                sphereRadius,
                worldDirection,
                detectionDistance,
                layerMask,
                QueryTriggerInteraction.Collide
            );

            foreach (RaycastHit hit in allHits)
            {
                if (hit.collider.CompareTag("Dirt"))
                {
                    distances[i] = Mathf.Min(distances[i], hit.distance);
                }
                else if (hit.collider.GetComponentInParent<ResettableObject>() != null)
                {
                    furnitureDistances[i] = Mathf.Min(furnitureDistances[i], hit.distance);
                }
            }

            if (distances[i] < detectionDistance)
            {
                Debug.DrawRay(sphereOrigin, worldDirection * distances[i], Color.magenta);
                DrawDebugSphere(sphereOrigin + worldDirection * distances[i], sphereRadius, Color.magenta);
            }

            if (furnitureDistances[i] < detectionDistance)
            {
                Debug.DrawRay(sphereOrigin, worldDirection * furnitureDistances[i], Color.cyan);
                DrawDebugSphere(sphereOrigin + worldDirection * furnitureDistances[i], sphereRadius, Color.cyan);
            }
        }
    }

    private void DrawDebugSphere(Vector3 center, float radius, Color color)
    {
        Debug.DrawLine(center + Vector3.up * radius, center - Vector3.up * radius, color);
        Debug.DrawLine(center + Vector3.right * radius, center - Vector3.right * radius, color);
        Debug.DrawLine(center + Vector3.forward * radius, center - Vector3.forward * radius, color);
    }

    private float[] GetDistances()
    {
        DetectSurroundings();
        return distances;
    }

    private float GetNearestDirtDistance()
    {
        GameObject[] dirtObjects = GameObject.FindGameObjectsWithTag("Dirt");

        if (dirtObjects.Length == 0)
            return 0f;

        float nearestDistance = float.MaxValue;

        foreach (GameObject dirt in dirtObjects)
        {
            float distance = Vector3.Distance(transform.position, dirt.transform.position);

            if (distance < nearestDistance)
                nearestDistance = distance;
        }

        return nearestDistance;
    }
}