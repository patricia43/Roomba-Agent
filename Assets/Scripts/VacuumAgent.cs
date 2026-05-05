using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class VacuumAgent : Agent
{
    [SerializeField] public float _moveSpeed = 6f;
    [SerializeField] public float _rotationSpeed = 200f;

    protected Rigidbody _rigidbody;

    protected Vector3 _movement = Vector3.zero;
    protected float _rotation = 0f;

    protected GameManager _gameManager;

    // detection settings
    public float sphereRadius = 0.2f;
    public float detectionDistance = 8f;
    public int numberOfDirections = 16;
    // public LayerMask obstacleLayerMask = 1 << 6;
    // public LayerMask obstacleLayerMask = ~0;
    public LayerMask layerMask;

    // det results
    protected float[] distances;
    protected bool[] hasHit;
    protected RaycastHit[] hits;

    // debugging
    public bool debugDrawHit = true;
    public bool debugDrawNoHit = false;

    // initialize agent
    public override void Initialize()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _rigidbody = GetComponent<Rigidbody>();

        distances = new float[numberOfDirections];
        hasHit = new bool[numberOfDirections];
        hits = new RaycastHit[numberOfDirections];
        Debug.Log("VacuumAgentInitialized");

        Physics.queriesHitTriggers = true;
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent / scene at ep start
        _gameManager.StartNewEpisode();
        Debug.Log("Episode begun");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Collect observations
        float[] distances = GetDistances();
        sensor.AddObservation(distances);
        // Debug.Log("Observations collected");
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
        // Apply actions and rewards
        if (_gameManager != null && !_gameManager.IsGameActive())
            return;

        if (_gameManager != null)
            _gameManager.RegisterStep();

        int moveAction = actions.DiscreteActions[0];
        int rotateAction = actions.DiscreteActions[1];

        // move agent
        if (moveAction == 1)
        {
            // move forward
            _movement = transform.forward * _moveSpeed * Time.fixedDeltaTime;
        }

        // rotate agent
        if (rotateAction == 1)
        {
            // rotate left
            _rotation = -_rotationSpeed * Time.fixedDeltaTime;
        }
        else if (rotateAction == 2)
        {
            // rotate right
            _rotation = _rotationSpeed * Time.fixedDeltaTime;
        }

        // small penalty for time passing
        AddReward(-0.01f);

        // Debug.Log("Action received: move " + moveAction + ", rotate " + rotateAction);
    }

    private void FixedUpdate()
    {
        // apply movement and rotation
        _rigidbody.MovePosition(_rigidbody.position + _movement);

        _rigidbody.MoveRotation(
            _rigidbody.rotation * Quaternion.Euler(0f, _rotation, 0f)
        );

        _movement = Vector3.zero;
        _rotation = 0f;

        _rigidbody.angularVelocity = Vector3.zero;

        // Debug.Log("FixedUpdate applied movement and rotation");
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

            Vector3 localDirection = new Vector3(Mathf.Cos(radian), 0f, Mathf.Sin(radian));
            Vector3 worldDirection = transform.TransformDirection(localDirection).normalized;

            Debug.DrawRay(sphereOrigin, worldDirection * detectionDistance, Color.yellow);

            hasHit[i] = Physics.SphereCast(
                sphereOrigin,
                sphereRadius,
                worldDirection,
                out hits[i],
                detectionDistance,
                layerMask,
                QueryTriggerInteraction.Collide
            );

            if (hasHit[i])
            {
                distances[i] = hits[i].distance;

                Color rayColor = hits[i].collider.CompareTag("Dirt") ? Color.magenta : Color.green;

                Debug.DrawRay(sphereOrigin, worldDirection * hits[i].distance, rayColor);
                Debug.Log("Hit: " + hits[i].collider.name + " tag=" + hits[i].collider.tag);
            }
            else
            {
                distances[i] = detectionDistance;
            }
        }
    }

    //private void DrawDebugSphere(Vector3 center, float radius, Color color)
    //{
    //    Debug.DrawLine(center + Vector3.up * radius, center - Vector3.up * radius, color);
    //    Debug.DrawLine(center + Vector3.right * radius, center - Vector3.right * radius, color);
    //    Debug.DrawLine(center + Vector3.forward * radius, center - Vector3.forward * radius, color);
    //}

    private float[] GetDistances()
    {
        DetectSurroundings();
        return distances;
    }
}