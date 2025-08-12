using UnityEngine;

public class SlowRotate : MonoBehaviour
{
    [Tooltip("The speed of the rotation in degrees per second.")]
    public float rotationSpeed = 10.0f;

    // A private variable to store the random axis of rotation
    private Vector3 rotationAxis;

    void Start()
    {
        // On start, pick a single, random direction vector to rotate around.
        // Normalizing it makes it a pure direction.
        rotationAxis = Random.onUnitSphere;
    }

    void Update()
    {
        // Continuously rotate the object around its unique random axis.
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }
}