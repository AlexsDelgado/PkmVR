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
    private Coroutine initialDockRoutine;

    [SerializeField] private BallFXController fx;
    private PokeballPoolManager pokeballPool;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        pokeballPool = PokeballPoolManager.Instance ?? FindFirstObjectByType<PokeballPoolManager>();

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }
    }

    private void Start()
    {
        // Start docked/stable when spawned under a belt socket.
        initialDockRoutine = StartCoroutine(InitialDockRoutine());
    }

    // XR selection ---------------------------------------------------------

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        var socketInteractor = args.interactorObject as XRSocketInteractor;

        // 1) Belt socket grabs: stay parented to the belt & kinematic
        if (socketInteractor != null)
        {
            MakeKinematicDocked();
            base.OnSelectEntered(args);
            return;
        }

        // 2) ANY non-socket grab (hand, ray, etc.)
        //    -> cancel the auto-dock routine so it doesn't re-parent in mid air
        if (initialDockRoutine != null)
        {
            StopCoroutine(initialDockRoutine);
            initialDockRoutine = null;
        }

        // completely detach from the belt / XR rig so it can't be moved by the HMD
        transform.SetParent(null, true);   // keep world pose but clear parent

        MakeDynamicForThrow();
        base.OnSelectEntered(args);
        fx?.OnThrowStart();

        if (mode == BallMode.Empty && beltSocket != null)
        {
            StartCoroutine(NotifyPokeballGrabbedDelayed());
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        bool isSocket = args.interactorObject is XRSocketInteractor;

        // If released from a hand/controller, keep physics active
        if (!isSocket && rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Let XRGrabInteractable do its thing (may try to re-parent)
        base.OnSelectExited(args);

        // Then force the ball to remain un-parented so belt / HMD movement
        // cannot affect its trajectory.
        if (!isSocket)
        {
            transform.SetParent(null, true); // keep world pose, clear parent
        }
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
        // During a wild capture, let the capture coroutine control ReturnToPool
        if (isCapturing)
            return;

        if (rb != null && rb.isKinematic) return;

        // Ignore collisions with anything under the same root as the belt,
        // so the player body never counts as "ground".
        if (beltSocket != null)
        {
            Transform t = col.transform;
            Transform beltRoot = beltSocket.transform.root;
            while (t != null)
            {
                if (t == beltRoot)
                    return; // it's part of the player rig, not ground
                t = t.parent;
            }
        }

        // Only react to configured ground layers (floor)
        if (((1 << col.gameObject.layer) & groundLayers.value) == 0)
            return;

        // SPECIAL CASE: Team balls have custom ground behaviour
        if (beltSocket != null && beltSocket.GetSocketType() == BeltSocketType.TeamPokemon)
        {
            HandleTeamBallGroundCollision(col);
            return;
        }

        // Default behaviour: empty capture balls etc.
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
            if (PokemonsManager.Instance != null)
            {
                PokemonsManager.Instance.AddNewPokemon(pokemon.pkm_data, 5);
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
            if (beltAttach != null)
            {
                // IMPORTANT: keep world scale when reparenting, so it doesn't shrink
                transform.SetParent(beltAttach, true);  // was: false

                // Snap position/rotation to the attach point (scale untouched)
                transform.position = beltAttach.position;
                transform.rotation = beltAttach.rotation;
            }

            MakeKinematicDocked();
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

        // While docked or otherwise static, don't use velocity tracking / throw
        movementType = MovementType.Kinematic;   // or Instantaneous
        trackPosition = false;
        trackRotation = false;
        throwOnDetach = false;
    }

    private void MakeDynamicForThrow()
    {
        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;

        // In-hand behaviour: follow controller with velocity tracking + throw
        movementType = MovementType.VelocityTracking;
        trackPosition = true;
        trackRotation = true;
        throwOnDetach = true;
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
        if (beltSocket == null || beltAttach == null)
            yield break;

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

        //if a capture started during the delay, abort the auto-return
        if (isCapturing)
            yield break;

        ReturnToPool();
    }

    private void HandleTeamBallGroundCollision(Collision col)
    {
        // Only meaningful if this ball belongs to a team socket
        if (beltSocket == null || beltSocket.GetSocketType() != BeltSocketType.TeamPokemon)
            return;

        // Where to spawn the Pokémon on release
        Vector3 spawnPos = transform.position;
        if (col.contacts != null && col.contacts.Length > 0)
        {
            spawnPos = col.contacts[0].point;
        }
        spawnPos += Vector3.up * spawnLift; // small lift so it doesn't clip

        switch (mode)
        {
            // FULL = Pokémon is inside the ball. Hitting the ground should RELEASE it.
            case BallMode.Full:
                {
                    string speciesKey = assignedSpeciesPoolKey;
                    if (!string.IsNullOrEmpty(speciesKey))
                    {
                        // Spawn the party Pokémon using the assigned pool key
                        var go = PoolManager.I.Spawn(speciesKey, spawnPos, Quaternion.identity);
                        var pokemon = go.GetComponent<PokemonController>();
                        pokemon?.Init();

                        // Mark it as a caught/party Pokémon so it runs the right behaviours
                        var behavior = go.GetComponent<PokemonBehaviorManager>();
                        behavior?.EnterCaught();

                        // Link this ball to that specific instance
                        activePokemon = pokemon;

                        // Small cooldown so ball doesn't instantly re-trigger on overlap
                        nextRetrievalTime = Time.time + retrievalCooldown;
                    }
                    else
                    {
                        Debug.LogWarning("[Pokeball] Team ball has no assigned species key when trying to release.");
                    }

                    // Ball is now "empty but linked" (owner Pokémon is out in the world)
                    mode = BallMode.Team;

                    // Immediately go back to its belt socket
                    ReturnToPool();
                    break;
                }

            // TEAM = Pokémon is already out; ground hit just sends ball home.
            case BallMode.Team:
                {
                    ReturnToPool();
                    break;
                }

            // EMPTY here shouldn't really happen for a team socket,
            // but just in case, treat it like "go back to belt".
            case BallMode.Empty:
                {
                    ReturnToPool();
                    break;
                }
        }
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