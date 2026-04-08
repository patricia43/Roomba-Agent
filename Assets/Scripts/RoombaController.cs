using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RoombaController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float turnSpeed = 120f;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // W/S = forward/backward
        moveInput = Input.GetAxisRaw("Vertical");

        // A/D = rotate left/right
        turnInput = Input.GetAxisRaw("Horizontal");
    }

    private void FixedUpdate()
    {
        // Rotatie pe axa Y
        float turnAmount = turnInput * turnSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, turnAmount, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);

        // Miscare inainte/inapoi in directia in care priveste
        Vector3 moveAmount = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveAmount);
    }
}