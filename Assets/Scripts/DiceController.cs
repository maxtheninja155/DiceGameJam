using System.Collections.Generic;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

public class DiceController : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private float originalBounciness;


    [Header("Physics Settings")]
    public float rollForce = 10f;
    public float torqueForce = 10f;
    public Transform startPosition;
    public float settleTimeThreshold = 0.25f;
    public float reducedBounciness = 0.5f;

    [Header("Side Detection")]
    public Transform[] sideTransforms = new Transform[6];


    [Header("State")]
    [SerializeField] private DiceState currentState;
    public DiceState CurrentState => currentState; // Public property to get the state

    [Header("Ghost Die Settings")]
    public bool isGhostDie = false;

    // --- Stored Physics Data for Time Freeze ---
    public Vector3 storedVelocity;
    public Vector3 storedAngularVelocity;

    private float timeSinceLowVelocity;

    private ParticleSystem activeParticleStream;

    // --- Repulsion State Variables ---
    private Transform repelledFace;
    private float currentNudgeStrength;
    private bool hasBeenNudgedThisRoll;

    [Header("Subtle Nudge Settings")]
    [SerializeField] private float settlingVelocityThreshold = 0.8f;


    public enum DiceState { Idle, Rolling, Stopped }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        originalBounciness = col.material.bounciness;
    }

    void FixedUpdate()
    {
        //Updated logic for checking if the die has stopped.
        if (currentState == DiceState.Rolling && !rb.isKinematic)
        {

            CheckAndApplySubtleNudge();

            if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
            {
                // If velocity is low, start counting up.
                timeSinceLowVelocity += Time.fixedDeltaTime;
            }
            else
            {
                // If the die moves at all, reset the timer.
                timeSinceLowVelocity = 0f;
            }

            // If the die has been at rest for long enough, change its state.
            if (timeSinceLowVelocity >= settleTimeThreshold)
            {
                currentState = DiceState.Stopped;
                Debug.Log(gameObject.name + " has officially settled.");

                DestroySpringJoint();
            }
        }
    }

    private void CheckAndApplySubtleNudge()
    {
        if (repelledFace == null || hasBeenNudgedThisRoll) return;

        if (rb.linearVelocity.magnitude < settlingVelocityThreshold)
        {
            if (GetBottomFace() == repelledFace)
            {
                Vector3 nudgeAxis = transform.up;
                rb.AddTorque(nudgeAxis * currentNudgeStrength, ForceMode.Impulse);
                hasBeenNudgedThisRoll = true;
            }
        }
    }
    public void MarkFaceForRepulsion(Transform face, float nudgeStrength)
    {
        repelledFace = face;
        currentNudgeStrength = nudgeStrength;
        // Optional: Add a visual effect to the face's material here
        Debug.Log($"Marked {face.name} on {gameObject.name} to be repelled with strength {nudgeStrength}.");
    }

    // A method for the GameManager to call to reset the state.
    public void ResetRepulsion()
    {
        repelledFace = null;
        hasBeenNudgedThisRoll = false;
    }

    public void RollDice()
    {
        ResetRepulsion();
        timeSinceLowVelocity = 0f;

        currentState = DiceState.Rolling;
        transform.position = startPosition.position;
        transform.rotation = Random.rotation;

        rb.isKinematic = false;
        rb.AddForce(Vector3.up * rollForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
    }

    public void ResetDie()
    {
        timeSinceLowVelocity = 0f;
        currentState = DiceState.Idle;
        rb.isKinematic = true;
    }

    public int GetTopFace()
    {
        Transform highestSide = sideTransforms[0];
        float highestHeight = sideTransforms[0].position.y;
        // Cast rays from each side to find which one is facing up
        for (int i = 1; i < sideTransforms.Length; i++)
        {
            if (highestHeight < sideTransforms[i].position.y)
            {
                highestSide = sideTransforms[i];
                highestHeight = sideTransforms[i].position.y;
            }
        }

        return System.Array.IndexOf(sideTransforms, highestSide) + 1;
    }

    public Transform GetBottomFace()
    {
        Transform lowestSide = sideTransforms[0];
        for (int i = 1; i < sideTransforms.Length; i++)
        {
            if (sideTransforms[i].position.y < lowestSide.position.y)
            {
                lowestSide = sideTransforms[i];
            }
        }
        return lowestSide;
    }

    public Transform GetClosestFace(Vector3 worldPoint)
    {
        Transform closestFace = null;
        float minDistance = float.MaxValue;
        foreach (Transform face in sideTransforms)
        {
            float distance = Vector3.Distance(face.position, worldPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFace = face;
            }
        }
        return closestFace;
    }

    public void StartRigging()
    {
        // This creates a unique physics material instance for this die
        // and sets its bounciness to almost zero for the rig.
        col.material.bounciness = reducedBounciness;
    }

    public void AssignActiveParticleStream(ParticleSystem stream)
    {
        // If there was an old stream for some reason, stop it first.
        if (activeParticleStream != null)
        {
            activeParticleStream.Stop();
        }
        activeParticleStream = stream;
    }

    public void DestroySpringJoint()
    {

        col.material.bounciness = originalBounciness;
        if (TryGetComponent<SpringJoint>(out SpringJoint joint))
        {
            Destroy(joint);
        }

        // --- NEW: Stop the particle stream ---
        if (activeParticleStream != null)
        {
            // Calling Stop() will let existing particles finish their life,
            // and then the system will destroy itself because we set the Stop Action.
            activeParticleStream.Stop();
            activeParticleStream = null; // Clear the reference
        }
    }
    
    public void StartFloatingBack(float duration)
    {
        // Start the coroutine that will handle the movement over time.
        StartCoroutine(FloatBackCoroutine(duration));
    }

    private IEnumerator FloatBackCoroutine(float duration)
    {
        rb.isKinematic = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPosition.position;
        
        // --- NEW: Calculate the target rotation ---
        Quaternion startRot = transform.rotation;
        // We want the die to face the same general direction on the Y-axis as it started.
        // But we keep the final X and Z tumble rotation for a cool effect.
        Quaternion endRot = Quaternion.Euler(
            startRot.eulerAngles.x,         // Keep the final X rotation
            startPosition.rotation.eulerAngles.y, // Reset to the starting Y rotation
            startRot.eulerAngles.z          // Keep the final Z rotation
        );

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            // Smoothly move position AND rotation
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it ends at the exact destination
        transform.position = endPos;
        transform.rotation = endRot;
    }
}