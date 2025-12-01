using System.Collections;
using UnityEngine;

public class CaughtPokemon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;          
    [SerializeField] private Transform modelRoot;        
    [SerializeField] private LineOfSight lineOfSight;    
    [SerializeField] private Transform playerTarget;     

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float rotationSpeed = 6f;
    [Tooltip("Desired distance to keep from the player (meters).")]
    [SerializeField] private float stopDistance = 1.2f;

    [Header("Animator Parameters")]
    [SerializeField] private string walkBoolName = "IsWalking";

    private int walkBoolHash;
    private Coroutine followRoutine;

    // --------------------------------------------------------------------

    private void Awake()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        if (!modelRoot)
            modelRoot = transform;

        if (!lineOfSight)
            lineOfSight = GetComponent<LineOfSight>();

        // Auto-assign playerTarget if not set (VR Main Camera)
        if (!playerTarget)
        {
            Camera cam = Camera.main;
            if (cam != null)
                playerTarget = cam.transform;
        }

        walkBoolHash = Animator.StringToHash(walkBoolName);
    }

    private void OnEnable()
    {
        if (followRoutine != null)
            StopCoroutine(followRoutine);

        followRoutine = StartCoroutine(FollowLoop());
    }

    private void OnDisable()
    {
        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }

        if (animator)
            animator.SetBool(walkBoolHash, false);
    }

    // --------------------------------------------------------------------

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

    private IEnumerator FollowLoop()
    {
        while (true)
        {
            if (!playerTarget)
            {
                // Try to recover the camera if it wasn't ready yet
                Camera cam = Camera.main;
                if (cam != null)
                    playerTarget = cam.transform;

                yield return null;
                continue;
            }

            float dt = Time.deltaTime;

            // Direction & distance on horizontal plane
            Vector3 toPlayer = playerTarget.position - modelRoot.position;
            toPlayer.y = 0f;
            float sqrDist = toPlayer.sqrMagnitude;

            bool inSight = HasPlayerInSight();
            float stopDistSqr = stopDistance * stopDistance;

            if (inSight && sqrDist <= stopDistSqr)
            {
                // Player in LoS and close enough: idle + look at player
                animator.SetBool(walkBoolHash, false);

                if (sqrDist > 0.0001f)
                {
                    Vector3 dir = toPlayer.normalized;
                    Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                    modelRoot.rotation = Quaternion.Slerp(
                        modelRoot.rotation,
                        targetRot,
                        rotationSpeed * dt
                    );
                }
            }
            else
            {
                // Not in LoS or too far: move toward player
                if (sqrDist > 0.0001f)
                {
                    Vector3 dir = toPlayer.normalized;

                    Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                    modelRoot.rotation = Quaternion.Slerp(
                        modelRoot.rotation,
                        targetRot,
                        rotationSpeed * dt
                    );

                    if (sqrDist > stopDistSqr)
                    {
                        animator.SetBool(walkBoolHash, true);
                        transform.position += dir * (moveSpeed * dt);
                    }
                    else
                    {
                        animator.SetBool(walkBoolHash, false);
                    }
                }
                else
                {
                    animator.SetBool(walkBoolHash, false);
                }
            }

            yield return null;
        }
    }
}
