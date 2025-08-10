using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class DiceController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Physics Settings")]
    public float rollForce = 10f;
    public float torqueForce = 10f;
    public Transform startPosition;
    public float settleTimeThreshold = 0.25f;

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


    public enum DiceState { Idle, Rolling, Stopped }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (startPosition == null)
            startPosition = transform; // Use initial position as the default start
    }

    void FixedUpdate()
    {
        //Updated logic for checking if the die has stopped.
        if (currentState == DiceState.Rolling && !rb.isKinematic)
        {
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

    public void RollDice()
    {
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
        if (TryGetComponent<SpringJoint>(out SpringJoint joint))
            {
                Destroy(joint);
                Debug.Log("Sticky Spring Joint removed from " + gameObject.name);
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
}