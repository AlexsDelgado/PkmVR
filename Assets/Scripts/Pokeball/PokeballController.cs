using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(XRGrabInteractable))]
public class PokeballController : MonoBehaviour
{
    public enum BallState { Loaded, Empty }

    [Header("Config")]
    [SerializeField] private string assignedSpeciesPoolKey;      
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float recallDelay = 0.12f;

    [Header("Belt")]
    [SerializeField] private XRSocketInteractor beltSocket;      
    [SerializeField] private Transform beltAttach;

    [Header("Retrieval Cooldown")]
    [SerializeField] private float retrievalCooldown = 0.8f; 
    [SerializeField] private float spawnLift = 0.2f;          
    private float nextRetrievalTime = Mathf.NegativeInfinity;

    [SerializeField] private float captureBounceForce = 4f;
    [SerializeField] private Vector2 captureSidewaysRandom = new Vector2(-0.5f, 0.5f);

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private BallState state = BallState.Loaded;
    private PokemonController activePokemon;

    [SerializeField] private BallFXController fx;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // Grab/socket events
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    void Start()
    {
        // Start docked and stable (prevents initial fall)
        StartCoroutine(InitialDockRoutine());
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }
    }

    // --- Selection events ----------------------------------------------------

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // If the thing selecting us is the belt socket, make us "stick" there
        if (args.interactorObject is XRSocketInteractor)
            MakeKinematicDocked();
        else
            MakeDynamicForThrow();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // Usually means a hand/controller grabbed & released us → resume physics
        MakeDynamicForThrow();
        fx?.OnThrowStart();
    }

    // --- Collisions / triggers ----------------------------------------------

    void OnCollisionEnter(Collision col)
    {
        if (state == BallState.Loaded && IsGround(col.gameObject.layer))
        {
            var cp = col.GetContact(0);
            var spawnPos = cp.point + cp.normal * spawnLift;
            fx?.PlayImpactSet(cp.point, cp.normal);

            // existing
            SpawnPokemonAt(spawnPos);
            state = BallState.Empty;
            nextRetrievalTime = Time.time + retrievalCooldown;
            Invoke(nameof(RecallToBelt), recallDelay);
            return;
        }

        if (state == BallState.Empty)
        {
            // Hit something that isn't our Pokémon → just recall
            Invoke(nameof(RecallToBelt), recallDelay);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (state != BallState.Empty) return;
        if (Time.time < nextRetrievalTime) return;

        var p = other.GetComponentInParent<PokemonController>();
        if (p != null && p == activePokemon)
        {
            p.Despawn();
            activePokemon = null;
            state = BallState.Loaded;
            nextRetrievalTime = Mathf.NegativeInfinity;
            CaptureBounce();
            StartCoroutine(RecallAfterDelay());
        }
    }

    // --- Helpers -------------------------------------------------------------

    private void SpawnPokemonAt(Vector3 pos)
    {
        var go = PoolManager.I.Spawn(assignedSpeciesPoolKey, pos, Quaternion.identity);
        activePokemon = go.GetComponent<PokemonController>();
        activePokemon?.Init();
    }

    private void CaptureBounce()
    {
        if (!rb) return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Up plus a little random sideways so it looks nice
        float sideX = Random.Range(captureSidewaysRandom.x, captureSidewaysRandom.y);
        float sideZ = Random.Range(captureSidewaysRandom.x, captureSidewaysRandom.y);
        Vector3 dir = new Vector3(sideX, 1f, sideZ).normalized;

        rb.AddForce(dir * captureBounceForce, ForceMode.VelocityChange);
    }

    private bool IsGround(int layer) => (groundMask.value & (1 << layer)) != 0;

    private void RecallToBelt()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        DockToBeltImmediate();
        TrySocketSelect();
        fx?.OnThrowEnd();
        fx?.PlayRecallBlink(beltAttach);
    }

    private void DockToBeltImmediate()
    {
        if (beltAttach != null)
            transform.SetPositionAndRotation(beltAttach.position, beltAttach.rotation);
        MakeKinematicDocked();
    }

    private void MakeKinematicDocked()
    {
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void MakeDynamicForThrow()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void TrySocketSelect()
    {
        if (!beltSocket) return;

        var interactable = grab as IXRSelectInteractable;
        var manager = beltSocket.interactionManager;
        if (interactable == null || manager == null) return;

        // Simulate socket grabbing the ball
        if (beltSocket.hasSelection) return;

        // Request a normal selection (transferable to hand later)
        if (beltSocket.CanSelect(interactable))
            manager.SelectEnter(beltSocket, interactable);
    }

    private IEnumerator InitialDockRoutine()
    {
        var col = GetComponent<Collider>();
        bool hadCol = col && col.enabled;
        if (col) col.enabled = false;      // prevent launching the player or bouncing

        MakeKinematicDocked();             // disable gravity/physics while docking

        // Wait for one frame so XR rig & belt follower settle
        yield return null;

        // also wait for the socket to be active
        var tEnd = Time.time + 1f;
        while (beltSocket != null && !beltSocket.isActiveAndEnabled && Time.time < tEnd)
            yield return null;

        // Snap to belt attach and let the socket take ownership
        DockToBeltImmediate();
        TrySocketSelect();

        // Re-enable collider shortly after (tiny delay avoids overlap jitters)
        yield return new WaitForSeconds(0.05f);
        if (col && hadCol) col.enabled = true;
    }

    private IEnumerator RecallAfterDelay()
    {
        // adjust to taste; enough time to see the bounce arc
        yield return new WaitForSeconds(0.35f);
        RecallToBelt();
    }
}
