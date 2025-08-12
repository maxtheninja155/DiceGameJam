using System.Collections.Generic;
using UnityEngine;

public class RepulsionAbility : MonoBehaviour
{
    [Header("Nudge Settings")]
    public float nudgeTorqueStrength = 1.5f;

    [Header("Glow Effect Settings")]
    public float glowRadius = 0.5f; // Controls the size of the glow in the shader

    // This private class helps us store all the info we need for a repelled die.
    private class RepelledDieInfo
    {
        public Transform faceToRepel;
        public Renderer dieRenderer;
    }

    // We'll map each DiceController to its RepelledDieInfo.
    private Dictionary<DiceController, RepelledDieInfo> repelledDice;

    private void Awake()
    {
        repelledDice = new Dictionary<DiceController, RepelledDieInfo>();
    }

    // LateUpdate runs after all other Update calls, which is ideal for visual effects.
    private void LateUpdate()
    {
        // If there are no dice to update, do nothing.
        if (repelledDice.Count == 0) return;

        // Continuously update the glow center for each marked die.
        foreach (var pair in repelledDice)
        {
            RepelledDieInfo info = pair.Value;
            // Update the shader property with the face's current world position.
            info.dieRenderer.material.SetVector("_Glow_Center", info.faceToRepel.position);
        }
    }

    public void TryMarkFaceForRepulsion(Ray ray, LayerMask layersToCollideWith)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 10f , layersToCollideWith))
        {
            if (hit.collider.CompareTag("Die"))
            {
                DiceController targetDie = hit.collider.GetComponentInParent<DiceController>();
                if (targetDie != null)
                {
                    Transform closestFace = targetDie.GetClosestFace(hit.point);
                    if (closestFace != null)
                    {
                        // Use GetComponentInChildren to find the renderer on a child object if necessary.
                        Renderer targetRenderer = targetDie.GetComponentInChildren<Renderer>();

                        if (targetRenderer != null)
                        {
                            // If this die was already marked, turn off its old glow first.
                            if (repelledDice.ContainsKey(targetDie))
                            {
                                repelledDice[targetDie].dieRenderer.material.SetFloat("_Glow_Radius", 0f);
                            }

                            // Store all the info and turn on the new glow.
                            repelledDice[targetDie] = new RepelledDieInfo { faceToRepel = closestFace, dieRenderer = targetRenderer };
                            targetRenderer.material.SetFloat("_Glow_Radius", glowRadius);
                            Debug.Log($"<color=cyan>SHADER DEBUG:</color> Turning ON glow for {targetDie.name}.");


                            // Tell the DiceController which face to repel for the physics nudge.
                            targetDie.MarkFaceForRepulsion(closestFace, nudgeTorqueStrength);
                        }
                    }
                }
            }
        }
    }

    // Called by GameManager to reset at the start of a new round.
    public void ResetAll()
    {
        // Before we clear our list, turn off the glow on all dice we were tracking.
        foreach (var pair in repelledDice)
        {
            pair.Value.dieRenderer.material.SetFloat("_Glow_Radius", 0f);
            Debug.Log($"<color=orange>SHADER DEBUG:</color> Turning OFF glow for {pair.Key.name}.");
        }
        repelledDice.Clear();
    }
}