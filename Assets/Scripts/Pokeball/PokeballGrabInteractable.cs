using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PokeballGrabInteractable : XRGrabInteractable
{
    public enum BallMode
    {
        Empty,  // generic capture ball, no species assigned
        Full,   // team ball with pokemon "inside"
        Team    // team ball whose pokemon is currently out
    }

    [Header("Config")]
    [SerializeField] private BallMode mode = BallMode.Empty;
    [SerializeField] private string assignedSpeciesPoolKey;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float recallDelay = 0.12f;

    [Header("Ground detection")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundReturnDelay = 0.8f;

    [Header("Belt Socket")]
    [SerializeField] private PokeBeltSocketInteractor beltSocket;
    [SerializeField] private Transform beltAttach;

    [Header("Retrieval Cooldown")]
    [SerializeField] private float retrievalCooldown = 0.8f;
    [SerializeField] private float spawnLift = 0.2f;
    private float nextRetrievalTime = float.NegativeInfinity;

    [Header("Capture Motion")]
    [SerializeField] private float captureBounceForce = 4f;
    [SerializeField] private Vector2 captureSidewaysRandom = new Vector2(-0.5f, 0.5f);

    [Header("Capture Chances")]
    [Tooltip("Per-shake chance the Pokémon stays in the ball. 1 = always stays, 0 = always escapes.")]
    [Range(0f, 1f)]
    [SerializeField] private float stayChancePerShake = 0.75f;

    [Tooltip("Number of shakes before a capture is considered successful.")]
    [SerializeField] private int shakesCount = 3;

    [Header("Shake Animation")]
    [SerializeField] private float timeBetweenShakes = 0.8f;
    [SerializeField] private float shakeAngle = 15f;
    [SerializeField] private float groundSnapRayHeight = 1.0f;
    [SerializeField] private float groundSnapRayDistance = 3.0f;
    [SerializeField] private float groundOffset = 0.02f;

    private Rigidbody rb;
    private PokemonController activePokemon;   // for team balls
    private bool isCapturing;                  // for empty capture flow
    private Coroutine captureRoutine;

    [SerializeField] private BallFXController fx;
    private PokeballPoolManager pokeballPool;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        pokeballPool = PokeballPoolManager.Instance ?? FindFirstObjectByType<PokeballPoolManager>();

        // VR-friendly grab behaviour (from cleaned script)
        movementType = MovementType.VelocityTracking;
        trackPosition = true;
        trackRotation = true;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }
    }

    private void Start()
    {
        // Start docked/stable when spawned under a belt socket.
        StartCoroutine(InitialDockRoutine());
    }

    // XR selection ---------------------------------------------------------

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
        {
            // Belt socket: keep parent (already set by ConfigureBallForSocket)
            MakeKinematicDocked();
            base.OnSelectEntered(args);
        }
        else
        {
            // Hand/controller: detach from belt so it can be thrown
            transform.SetParent(null, true);   // keep world pose

            MakeDynamicForThrow();
            base.OnSelectEntered(args);
            fx?.OnThrowStart();

            if (mode == BallMode.Empty && beltSocket != null)
            {
                StartCoroutine(NotifyPokeballGrabbedDelayed());
            }
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if (!(args.interactorObject is XRSocketInteractor))
        {
            // released from a hand/controller -> ensure physics is active
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        base.OnSelectExited(args);
    }

    private IEnumerator NotifyPokeballGrabbedDelayed()
    {
        // wait one frame so the grab is fully registered
        yield return null;
        pokeballPool?.OnPokeballGrabbed();
    }

    // Collisions / triggers -----------------------------------------------

    private void OnCollisionEnter(Collision col)
    {
        // Ignore while docked in the belt (kinematic) or grabbed by a socket
        if (rb != null && rb.isKinematic) return;
        if (isSelected && interactorsSelecting.Count > 0)
        {
            foreach (var interactor in interactorsSelecting)
            {
                if (interactor is XRSocketInteractor)
                    return;
            }
        }

        // Only react to configured ground layers
        if (((1 << col.gameObject.layer) & groundLayers.value) == 0)
            return;

        StartCoroutine(AutoReturnAfterDelay());
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore triggers while kinematic (docked) or held by a socket
        if (rb != null && rb.isKinematic) return;
        if (isSelected && interactorsSelecting.Count > 0)
        {
            foreach (var interactor in interactorsSelecting)
            {
                if (interactor is XRSocketInteractor)
                    return;
            }
        }

        if (Time.time < nextRetrievalTime)
            return;

        var pokemon = other.GetComponentInParent<PokemonController>();
        if (pokemon == null)
            return;

        switch (mode)
        {
            case BallMode.Empty:
                HandleEmptyBallPokemonTrigger(pokemon);
                break;

            case BallMode.Full:
                // full team ball never captures or recalls via trigger
                break;

            case BallMode.Team:
                HandleTeamBallPokemonTrigger(pokemon);
                break;
        }
    }

    // TEAM (pokemon out, ball linked) -------------------------------------

    private void HandleTeamBallPokemonTrigger(PokemonController pokemon)
    {
        if (activePokemon == null || pokemon != activePokemon)
            return;

        // recall: despawn and mark as stored
        activePokemon.Despawn();
        activePokemon = null;

        mode = BallMode.Full;
        nextRetrievalTime = float.NegativeInfinity;

        CaptureBounce();
        Invoke(nameof(ReturnToPool), recallDelay);
    }

    // EMPTY (wild capture) -------------------------------------------------

    private void HandleEmptyBallPokemonTrigger(PokemonController pokemon)
    {
        if (isCapturing) return;

        var behavior = pokemon.GetComponent<PokemonBehaviorManager>();
        if (behavior != null && behavior.CurrentState != PokemonState.Roaming)
            return; // only wild/roaming can be captured

        if (pokemon == activePokemon) return;

        captureRoutine = StartCoroutine(CaptureSequence(pokemon));
    }

    private IEnumerator CaptureSequence(PokemonController pokemon)
    {
        isCapturing = true;
        activePokemon = pokemon;

        string speciesKey = GetPokemonSpeciesKey(pokemon);
        Vector3 pokemonPos = pokemon.transform.position;

        // Pokemon enters ball
        pokemon.Despawn();

        // nice bounce
        CaptureBounce();

        float t = 0.25f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        SnapToGround();

        Quaternion baseRot = transform.rotation;
        bool escaped = false;

        for (int i = 0; i < shakesCount; i++)
        {
            float prePause = timeBetweenShakes * 0.25f;
            t = prePause;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                yield return null;
            }

            float duration = timeBetweenShakes * 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float n = elapsed / duration;
                float angle = Mathf.Sin(n * Mathf.PI * 2f) * shakeAngle;
                transform.rotation = baseRot * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            transform.rotation = baseRot;

            float roll = Random.value;
            if (roll > stayChancePerShake)
            {
                escaped = true;
                break;
            }

            float postPause = timeBetweenShakes * 0.25f;
            t = postPause;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                yield return null;
            }
        }

        transform.rotation = baseRot;

        if (escaped)
        {
            // pokemon breaks free
            var go = PoolManager.I.Spawn(speciesKey, pokemonPos, Quaternion.identity);
            var newPokemon = go.GetComponent<PokemonController>();
            newPokemon?.Init();

            var newBehavior = go.GetComponent<PokemonBehaviorManager>();
            newBehavior?.EnterRoaming();
        }
        else
        {
            // successful capture
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddCapturedPokemon(speciesKey);
                InventoryManager.Instance.SpendPokeball();
            }
        }

        t = 0.35f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        ReturnToPool();

        isCapturing = false;
        activePokemon = null;
        captureRoutine = null;
    }

    // Helpers --------------------------------------------------------------

    private void SnapToGround()
    {
        if (!rb) return;

        Vector3 origin = transform.position + Vector3.up * groundSnapRayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            groundSnapRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;

            transform.position = hit.point + Vector3.up * groundOffset;
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
        else
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private string GetPokemonSpeciesKey(PokemonController pokemon)
    {
        if (pokemon != null)
        {
            string key = pokemon.GetPoolKey();
            if (!string.IsNullOrEmpty(key))
                return key;
        }
        return assignedSpeciesPoolKey ?? "Unknown";
    }

    private void CaptureBounce()
    {
        if (!rb) return;

        // ensure dynamic before we kick it
        rb.isKinematic = false;
        rb.useGravity = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float sideX = Random.Range(captureSidewaysRandom.x, captureSidewaysRandom.y);
        float sideZ = Random.Range(captureSidewaysRandom.x, captureSidewaysRandom.y);
        Vector3 dir = new Vector3(sideX, 1f, sideZ).normalized;

        rb.AddForce(dir * captureBounceForce, ForceMode.VelocityChange);
    }

    private void ReturnToPool()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // TEAM balls (belt slots that represent a specific pokémon)
        // should physically go back to their belt socket.
        if (beltSocket != null && beltSocket.GetSocketType() == BeltSocketType.TeamPokemon)
        {
            // re-parent to belt attach so it follows the belt
            if (beltAttach != null)
            {
                transform.SetParent(beltAttach, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            MakeKinematicDocked();

            if (beltAttach != null)
                transform.SetPositionAndRotation(beltAttach.position, beltAttach.rotation);

            TrySocketSelect();
            return;
        }

        // EMPTY capture balls that came from a belt socket:
        if (beltSocket != null && beltSocket.GetSocketType() == BeltSocketType.EmptyPokeball)
        {
            beltSocket.OnEmptyBallReturnedToPool(this);
        }

        if (pokeballPool != null)
            pokeballPool.ReturnPokeballToPool(this);
        else
            gameObject.SetActive(false);
    }

    private void MakeKinematicDocked()
    {
        if (rb == null) return;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void MakeDynamicForThrow()
    {
        if (rb == null) return;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void DockToBeltImmediate()
    {
        if (beltAttach != null)
            transform.SetPositionAndRotation(beltAttach.position, beltAttach.rotation);
        MakeKinematicDocked();
    }

    private void TrySocketSelect()
    {
        if (beltSocket == null)
            return;

        var interactable = this as IXRSelectInteractable;
        if (beltSocket.interactionManager != null && interactable != null)
        {
            // if socket already has something else, deselect it
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

    private IEnumerator InitialDockRoutine()
    {
        var col = GetComponent<Collider>();
        bool hadCol = col && col.enabled;
        if (col) col.enabled = false;

        MakeKinematicDocked();

        yield return null;

        float tEnd = Time.time + 1f;
        while (beltSocket != null && !beltSocket.isActiveAndEnabled && Time.time < tEnd)
            yield return null;

        DockToBeltImmediate();
        TrySocketSelect();

        yield return new WaitForSeconds(0.05f);
        if (col && hadCol) col.enabled = true;
    }

    private IEnumerator AutoReturnAfterDelay()
    {
        yield return new WaitForSeconds(groundReturnDelay);
        ReturnToPool();   // this is the same ReturnToPool that already works for capture
    }

    // Public API used by pool / belt --------------------------------------

    public void SetMode(BallMode newMode) => mode = newMode;
    public BallMode GetMode() => mode;

    public void SetAssignedSpecies(string speciesPoolKey)
    {
        assignedSpeciesPoolKey = speciesPoolKey;
    }

    public string GetAssignedSpecies() => assignedSpeciesPoolKey;

    public void SetBeltSocket(PokeBeltSocketInteractor socket)
    {
        beltSocket = socket;
    }

    public void SetBeltAttach(Transform attach)
    {
        beltAttach = attach;
    }
}