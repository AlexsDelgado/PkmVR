using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class PokeballGrabInteractable : XRGrabInteractable
{
    public enum BallMode
    {
        Empty,  // can capture roaming Pokémon
        Full,   // contains a team Pokémon, can spawn it
        Team    // its Pokémon is currently out, can recall only that one
    }

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float autoReturnDelay = 2f;

    [Header("Belt")]
    [SerializeField] private PokeBeltSocketInteractor beltSocket;
    [SerializeField] private Transform beltAttach;

    [Header("State")]
    [SerializeField] private BallMode mode = BallMode.Empty;
    [SerializeField] private string assignedSpeciesKey;

    private PokeballPoolManager pokeballPool;

    // --------------------------------------------------------------------
    // Accessors used by other scripts
    // --------------------------------------------------------------------

    public void SetMode(BallMode newMode) => mode = newMode;
    public BallMode GetMode() => mode;

    public void SetAssignedSpecies(string key) => assignedSpeciesKey = key;
    public string GetAssignedSpecies() => assignedSpeciesKey;

    public void SetBeltSocket(PokeBeltSocketInteractor socket) => beltSocket = socket;
    public void SetBeltAttach(Transform t) => beltAttach = t;

    // --------------------------------------------------------------------
    // Unity / XR lifecycle
    // --------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();

        if (!rb)
            rb = GetComponent<Rigidbody>();

        pokeballPool = PokeballPoolManager.Instance;

        // Use real physics while held; we don't need XR's throw-on-detach
        movementType = MovementType.VelocityTracking;
        trackPosition = true;
        trackRotation = true;
        throwOnDetach = false;

        // Default physics state
        rb.useGravity = true;
        rb.isKinematic = false;
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        // If grabbed by belt socket -> stay kinematic and stuck to belt.
        // If grabbed by hand/controller -> dynamic for throwing.
        if (args.interactorObject is XRSocketInteractor)
        {
            MakeKinematicDocked();
        }
        else
        {
            MakeDynamicForThrow();
        }

        base.OnSelectEntering(args);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        // (Optional: start trail FX or notify pool that a ball is in use)
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        // When leaving a hand, just keep physics active; belt re-docking is
        // handled elsewhere via ReturnToPool / socket logic.
        if (!(args.interactorObject is XRSocketInteractor))
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        base.OnSelectExited(args);
    }

    // --------------------------------------------------------------------
    // Physics helpers
    // --------------------------------------------------------------------

    private void MakeKinematicDocked()
    {
        if (!rb) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void MakeDynamicForThrow()
    {
        if (!rb) return;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    /// <summary>
    /// Snap ball to ground at given horizontal position (used by capture
    /// sequence or ground impact). Optionally keep slight offset.
    /// </summary>
    public void SnapToGround(Vector3 worldPosition, float rayDistance = 2f)
    {
        Vector3 origin = worldPosition + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = worldPosition;
        }
    }

    // --------------------------------------------------------------------
    // Pool / belt re-docking
    // --------------------------------------------------------------------

    /// <summary>
    /// Called by capture sequence / ground logic when the ball is done and
    /// should go back to belt or pool.
    /// </summary>
    public void ReturnToPool()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // If this ball belongs to a belt socket, always re-dock there.
        if (beltSocket != null)
        {
            MakeKinematicDocked();

            if (beltAttach != null)
                transform.SetPositionAndRotation(beltAttach.position, beltAttach.rotation);

            TrySocketSelect();
            return;
        }

        // Otherwise, treat as pooled ball (non-belt usage)
        if (pokeballPool != null)
        {
            pokeballPool.ReturnPokeballToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Re-select this ball in its belt socket (if any).
    /// </summary>
    public void TrySocketSelect()
    {
        if (beltSocket == null)
            return;

        var interactable = this as IXRSelectInteractable;
        if (beltSocket.interactionManager != null && interactable != null)
        {
            if (beltSocket.hasSelection && beltSocket.interactablesSelected.Count > 0 &&
                beltSocket.interactablesSelected[0] != interactable)
            {
                beltSocket.interactionManager.SelectExit(beltSocket, beltSocket.interactablesSelected[0]);
            }

            if (!beltSocket.hasSelection && beltSocket.CanSelect(interactable))
            {
                beltSocket.interactionManager.SelectEnter(beltSocket, interactable);
            }
        }
    }

    // --------------------------------------------------------------------
    // Example collision for auto-return when hitting ground
    // (you can adapt to your capture logic)
    // --------------------------------------------------------------------

    private void OnCollisionEnter(Collision collision)
    {
        // Simple rule: when we hit the ground after being thrown, schedule return.
        if (collision.collider.CompareTag("Ground"))
        {
            StartCoroutine(AutoReturnAfterDelay());
        }
    }

    private IEnumerator AutoReturnAfterDelay()
    {
        yield return new WaitForSeconds(autoReturnDelay);
        ReturnToPool();
    }
}