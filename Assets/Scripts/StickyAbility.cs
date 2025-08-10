// In StickyAbility.cs
using UnityEngine;

public class StickyAbility : MonoBehaviour
{
    [Header("Sticky Settings")]
    public float stickyStrength = 50f; // Increased for a noticeable pull
    public float springDamper = 5f;   // Helps to reduce oscillations

    [Header("Visual Settings")]
    public GameObject anchorMarkerPrefab; // A simple sphere or sprite prefab to mark the anchor
    private GameObject currentMarker;

    public ParticleSystem particleStreamPrefab;

    // --- Private State Variables ---
    private bool anchorIsSet = false;
    private Vector3 tableAnchorPoint;

    // Called from GhostController on Left-Click
    public void TrySetAnchorPoint(Ray ray)
    {
        // Can't set a new anchor if one is already set
        if (anchorIsSet) return;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if we hit the table (make sure your table has the "Table" tag)
            if (hit.collider.CompareTag("Table"))
            {
                anchorIsSet = true;
                tableAnchorPoint = hit.point;

                Debug.Log("Sticky Anchor set at: " + tableAnchorPoint);

                // Create a visual marker
                if (anchorMarkerPrefab != null)
                {
                    currentMarker = Instantiate(anchorMarkerPrefab, tableAnchorPoint, Quaternion.identity);
                }
            }
        }
    }

    // Called from GhostController on Right-Click
    public void TryAttachSpringToDie(Ray ray)
    {
        // Can only attach a spring if an anchor has been set first
        if (!anchorIsSet) return;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if we hit a die (make sure your dice have the "Die" tag)
            if (hit.collider.CompareTag("Die"))
            {
                DiceController targetDie = hit.collider.GetComponentInParent<DiceController>();
                if (targetDie != null)
                {
                    AttachSpring(targetDie, hit.point);
                }
            }
        }
    }

    private void AttachSpring(DiceController die, Vector3 hitPoint)
    {
        Rigidbody dieRb = die.GetComponent<Rigidbody>();
        if (dieRb == null) return;

        // Find the closest face transform on the die to our hit point
        Transform closestFace = null;
        float minDistance = float.MaxValue;
        foreach (Transform face in die.sideTransforms)
        {
            float distance = Vector3.Distance(face.position, hitPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFace = face;
            }
        }

        if (closestFace == null) return;

        // --- Create and Configure the Spring Joint ---
        SpringJoint joint = dieRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false; // We will set the world-space anchor manually
        joint.connectedAnchor = tableAnchorPoint;   // The point on the table

        // Set the anchor on the die's body relative to its center of mass
        joint.anchor = die.transform.InverseTransformPoint(closestFace.position);

        // Set spring properties
        joint.spring = stickyStrength;
        joint.damper = springDamper;

        Debug.Log($"Spring attached between table and {closestFace.name} on {die.name}");
        // --- Visual Feedback ---
        if (particleStreamPrefab != null)
        {
            // 1. Instantiate the prefab
            ParticleSystem streamInstance = Instantiate(particleStreamPrefab);

            // 2. Parent it to the die so it moves with it
            streamInstance.transform.SetParent(die.transform);

            // 3. Position it exactly on the face that was hit
            streamInstance.transform.position = closestFace.position;

            // 4. Aim the particle system AT the anchor point on the table
            streamInstance.transform.LookAt(tableAnchorPoint);

            // 5. Tell the die about its new particle system so it can clean it up later
            die.AssignActiveParticleStream(streamInstance);
        }

        // --- Cleanup and Finalize ---
        anchorIsSet = false; // Reset the ability state
        if (currentMarker != null) Destroy(currentMarker); // Remove the visual marker
        GameManager.Instance.NotifyStickyAbilityUsed(); // Tell the GameManager the ability has been used
    }
}