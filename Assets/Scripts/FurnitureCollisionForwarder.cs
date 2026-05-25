using UnityEngine;

public class FurnitureCollisionForwarder : MonoBehaviour
{
    private ResettableObject parentResettable;

    private void Awake()
    {
        parentResettable = GetComponentInParent<ResettableObject>();

        if (parentResettable == null)
            Debug.LogError("No ResettableObject found in parent for " + gameObject.name, this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Forwarder collision on " + gameObject.name + " with " + collision.collider.name, this);

        if (parentResettable != null)
            parentResettable.HandlePlayerCollision(collision);
    }
}