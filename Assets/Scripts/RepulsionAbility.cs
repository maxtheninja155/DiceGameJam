// In RepulsionAbility.cs
using UnityEngine;

public class RepulsionAbility : MonoBehaviour
{
    [Header("Nudge Settings")]
    public float nudgeTorqueStrength = 1.5f;

    // Called from GhostController.
    public void TryMarkFaceForRepulsion(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Die"))
            {
                DiceController targetDie = hit.collider.GetComponentInParent<DiceController>();
                if (targetDie != null)
                {
                    Transform closestFace = targetDie.GetClosestFace(hit.point);
                    if (closestFace != null)
                    {
                        // Tell the DiceController to repel this face, passing along our settings.
                        targetDie.MarkFaceForRepulsion(closestFace, nudgeTorqueStrength);
                    }
                }
            }
        }
    }
}