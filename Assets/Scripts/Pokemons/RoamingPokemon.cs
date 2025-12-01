using System.Collections;
using UnityEngine;

public class RoamingPokemon : MonoBehaviour
{
    private enum State
    {
        Roam,
        Alert
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform modelRoot; 
    [SerializeField] private LineOfSight lineOfSight;

    [Tooltip("Target to check LoS against (VR camera or player rig root).")]
    [SerializeField] private Transform playerTarget;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.6f;
    [SerializeField] private float rotationSpeed = 6f; // how fast it turns toward player

    [SerializeField] private float minWalkDuration = 1.5f;
    [SerializeField] private float maxWalkDuration = 3.0f;

    [SerializeField] private float minIdleDuration = 1.0f;
    [SerializeField] private float maxIdleDuration = 2.5f;

    [Header("Detection")]
    [Tooltip("How often (seconds) we check LoS. 0.1–0.2 is usually enough.")]
    [SerializeField] private float losCheckInterval = 0.15f;

    [Tooltip("Approximate length of the intimidate animation (seconds).")]
    [SerializeField] private float intimidateDuration = 1.0f;

    [Header("Animator Parameters")]
    [SerializeField] private string walkBoolName = "IsWalking";
    [SerializeField] private string intimidateTriggerName = "Intimidate";

    [Header("Obstacles")]
    [SerializeField] private LayerMask obstacleMask;    // walls, rocks, etc.
    [SerializeField] private float obstacleRayHeight = 0.5f;
    [SerializeField] private float obstacleSkin = 0.05f;

    // Animator hashes to avoid string lookups every call
    private int walkBoolHash;
    private int intimidateTriggerHash;

    // State & coroutines
    private State currentState = State.Roam;
    private Coroutine roamRoutine;
    private Coroutine alertRoutine;

    private void Awake()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        if (!modelRoot)
            modelRoot = transform;

        if (!lineOfSight)
            lineOfSight = GetComponent<LineOfSight>();

        // Auto-assign player target: use main camera (VR HMD)
        if (!playerTarget)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                playerTarget = cam.transform;
            }
            else
            {
                // fallback: try tag "Player" 
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    playerTarget = playerGO.transform;
            }
        }

        walkBoolHash = Animator.StringToHash(walkBoolName);
        intimidateTriggerHash = Animator.StringToHash(intimidateTriggerName);

        currentState = State.Roam;
    }

    private void OnEnable()
    {
        StartRoaming();
    }

    private void OnDisable()
    {
        // Kill coroutines so their IEnumerator instances can be collected
        if (roamRoutine != null)
        {
            StopCoroutine(roamRoutine);
            roamRoutine = null;
        }

        if (alertRoutine != null)
        {
            StopCoroutine(alertRoutine);
            alertRoutine = null;
        }
    }

    // Line of Sight helper

    private bool HasPlayerInSight()
    {
        if (!playerTarget || !lineOfSight)
            return false;

        if (!lineOfSight.CheckRange(playerTarget))
            return false;

        if (!lineOfSight.CheckAngle(playerTarget))
            return false;

        if (!lineOfSight.CheckView(playerTarget))
            return false;

        return true;
    }

    // Roaming logic

    private void StartRoaming()
    {
        currentState = State.Roam;

        if (roamRoutine != null)
            StopCoroutine(roamRoutine);

        roamRoutine = StartCoroutine(RoamLoop());
    }

    private IEnumerator RoamLoop()
    {
        float losTimer = 0f;

        while (currentState == State.Roam)
        {
            // --- choose random horizontal direction ---
            Vector3 dir = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            );

            if (dir.sqrMagnitude < 0.0001f)
                dir = modelRoot.forward;

            dir.Normalize();

            modelRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // --- WALK PHASE ---
            float walkDuration = Random.Range(minWalkDuration, maxWalkDuration);
            float t = walkDuration;

            animator.SetBool(walkBoolHash, true);

            while (t > 0f && currentState == State.Roam)
            {
                float dt = Time.deltaTime;
                t -= dt;

                // Move
                float step = walkSpeed * dt;

                // Ray from a small height on the model root
                Vector3 rayOrigin = modelRoot.position + Vector3.up * obstacleRayHeight;

                bool blocked = Physics.Raycast(
                    rayOrigin,
                    dir,
                    step + obstacleSkin,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                if (!blocked)
                {
                    transform.position += dir * step;
                }
                else
                {
                    // Hit a wall: stop this walk early so we choose a new dir next time
                    t = 0f;
                }

                // LoS check
                losTimer -= dt;
                if (losTimer <= 0f)
                {
                    losTimer = losCheckInterval;
                    if (HasPlayerInSight())
                    {
                        StartAlert();
                        animator.SetBool(walkBoolHash, false);
                        yield break;
                    }
                }

                yield return null;
            }

            animator.SetBool(walkBoolHash, false);

            if (currentState != State.Roam)
                yield break;

            // --- IDLE PHASE ---
            float idleDuration = Random.Range(minIdleDuration, maxIdleDuration);
            t = idleDuration;

            while (t > 0f && currentState == State.Roam)
            {
                float dt = Time.deltaTime;
                t -= dt;

                losTimer -= dt;
                if (losTimer <= 0f)
                {
                    losTimer = losCheckInterval;
                    if (HasPlayerInSight())
                    {
                        StartAlert();
                        yield break;
                    }
                }

                yield return null;
            }
        }
    }

    // Alert / Intimidate state

    private void StartAlert()
    {
        currentState = State.Alert;

        if (roamRoutine != null)
        {
            StopCoroutine(roamRoutine);
            roamRoutine = null;
        }

        if (alertRoutine != null)
            StopCoroutine(alertRoutine);

        alertRoutine = StartCoroutine(AlertLoop());
    }

    private IEnumerator AlertLoop()
    {
        animator.SetBool(walkBoolHash, false);
        animator.ResetTrigger(intimidateTriggerHash);
        animator.SetTrigger(intimidateTriggerHash);

        float intimidateTimer = intimidateDuration;
        float losTimer = 0f;

        while (true)
        {
            float dt = Time.deltaTime;

            // Always try to look at the player
            FacePlayer(dt);

            // Run intimidate timer once
            if (intimidateTimer > 0f)
            {
                intimidateTimer -= dt;
            }

            // LoS check
            losTimer -= dt;
            if (losTimer <= 0f)
            {
                losTimer = losCheckInterval;

                // Player left our LoS? go back to roaming
                if (!HasPlayerInSight())
                {
                    alertRoutine = null;
                    StartRoaming();
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void FacePlayer(float dt)
    {
        if (!playerTarget)
            return;

        Vector3 dir = playerTarget.position - modelRoot.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        modelRoot.rotation = Quaternion.Slerp(
            modelRoot.rotation,
            targetRot,
            rotationSpeed * dt
        );
    }
}
