using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PokeballGrabInteractable : XRGrabInteractable
{
    public enum BallMode
    {
        Empty,   // no pokemon assigned, used only to capture roaming pokemon
        Full,    // team ball with a pokemon "inside"
        Team     // pokemon is out, ball is linked but empty; can only recall that pokemon
    }

    [Header("Config")]
    [SerializeField] private BallMode mode = BallMode.Empty;
    [SerializeField] private string assignedSpeciesPoolKey; // used in Full/Team modes
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float recallDelay = 0.12f;

    [Header("Belt Socket")]
    [SerializeField] private XRSocketInteractor beltSocket;      
    [SerializeField] private Transform beltAttach;

    [Header("Retrieval Cooldown")]
    [SerializeField] private float retrievalCooldown = 0.8f; 
    [SerializeField] private float spawnLift = 0.2f;          
    private float nextRetrievalTime = Mathf.NegativeInfinity;

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

    // For Team mode: currently spawned instance for this ball
    private PokemonController activePokemon;

    // For Empty capture mode
    private bool isCapturing = false;
    private Coroutine captureRoutine;

    [SerializeField] private BallFXController fx;
    // Referencia al pool manager de pokeballs
    private PokeballPoolManager pokeballPool;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        pokeballPool = FindFirstObjectByType<PokeballPoolManager>();
    }

    void Start()
    {
        // Start docked and stable (prevents initial fall)
        StartCoroutine(InitialDockRoutine());
    }

    protected override void OnDestroy()
    {
        // No necesitamos remover listeners porque no los agregamos
        base.OnDestroy();
    }

    // --- Selection events ----------------------------------------------------

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        // IMPORTANTE: Cambiar el estado del Rigidbody ANTES de llamar a base
        // para evitar que XR Interaction Toolkit intente aplicar física a un cuerpo cinemático
        
        // Si es el socket del cinturón, mantenerlo estático
        if (args.interactorObject is XRSocketInteractor)
        {
            MakeKinematicDocked();
            base.OnSelectEntered(args);
        }
        else
        {
            // Si es una mano agarrando, activar física PRIMERO
            MakeDynamicForThrow();
            base.OnSelectEntered(args);
            fx?.OnThrowStart();
            
            // Si es modo Empty y se agarra del socket 1, notificar al pool manager
            // para que spawnee otra pokeball si hay en inventario
            if (mode == BallMode.Empty && beltSocket != null)
            {
                // Usar coroutine para esperar un frame y que el socket se limpie
                StartCoroutine(NotifyPokeballGrabbedDelayed());
            }
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        // IMPORTANTE: Asegurar que la gravedad esté activada ANTES de llamar a base
        // para que XR Interaction Toolkit pueda aplicar la física correctamente
        
        // Si se soltó de una mano (no de un socket), asegurar que la gravedad esté activada
        if (!(args.interactorObject is XRSocketInteractor))
        {
            // Asegurar que la pokeball tenga física activa cuando se suelta
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
        yield return null; // Esperar un frame para que el socket se limpie
        pokeballPool?.OnPokeballGrabbed();
    }

    // --- Collisions / triggers ----------------------------------------------

    void OnCollisionEnter(Collision col)
    {
        // Ignorar colisiones si está en modo cinemático (en el socket) o siendo agarrada por un socket
        if (rb != null && rb.isKinematic) return;
        if (isSelected && interactorsSelecting.Count > 0)
        {
            foreach (var interactor in interactorsSelecting)
            {
                if (interactor is XRSocketInteractor)
                    return; // Está siendo agarrada por un socket, ignorar colisiones
            }
        }

        if (IsGround(col.gameObject.layer))
        {
            switch (mode)
            {
                case BallMode.Full:
                    HandleFullBallGroundHit(col);
                    return;

                case BallMode.Team:
                    // Pokemon already out; ball just returns to belt/pool
                    Invoke(nameof(ReturnToPool), recallDelay);
                    return;

                case BallMode.Empty:
                    HandleEmptyBallGroundHit(col);
                    return;
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        // Ignorar triggers si está en modo cinemático (en el socket) o siendo agarrada por un socket
        if (rb != null && rb.isKinematic) return;
        if (isSelected && interactorsSelecting.Count > 0)
        {
            foreach (var interactor in interactorsSelecting)
            {
                if (interactor is XRSocketInteractor)
                    return; // Está siendo agarrada por un socket, ignorar triggers
            }
        }

        if (Time.time < nextRetrievalTime) return;

        var pokemon = other.GetComponentInParent<PokemonController>();
        if (pokemon == null || pokemon == activePokemon)
            return;

        switch (mode)
        {
            case BallMode.Empty:
                HandleEmptyBallPokemonTrigger(pokemon);
                break;

            case BallMode.Full:
                // Full ball never captures or recalls via trigger; just bounces
                break;

            case BallMode.Team:
                HandleTeamBallPokemonTrigger(pokemon);
                break;
        }
    }

    // --- MODES ---------------------------------------------

    private void HandleFullBallGroundHit(Collision col)
    {
        // If for some reason we already have an activePokemon, don't spawn another
        if (activePokemon != null)
        {
            Invoke(nameof(ReturnToPool), recallDelay);
            return;
        }

        if (string.IsNullOrEmpty(assignedSpeciesPoolKey))
        {
            Debug.LogWarning("Full ball has no assignedSpeciesPoolKey, cannot spawn team Pokémon.");
            Invoke(nameof(ReturnToPool), recallDelay);
            return;
        }

        var cp = col.GetContact(0);
        var spawnPos = cp.point + cp.normal * spawnLift;
        fx?.PlayImpactSet(cp.point, cp.normal);

        // Spawn assigned Pokémon ONCE and switch to Team mode
        var go = PoolManager.I.Spawn(assignedSpeciesPoolKey, spawnPos, Quaternion.identity);
        activePokemon = go.GetComponent<PokemonController>();
        activePokemon?.Init();

        mode = BallMode.Team;
        nextRetrievalTime = Time.time + retrievalCooldown;

        // Return ball to belt/pool after short delay
        Invoke(nameof(ReturnToPool), recallDelay);
    }

    private void HandleTeamBallPokemonTrigger(PokemonController pokemon)
    {
        // Only react if this is OUR pokemon
        if (activePokemon == null || pokemon != activePokemon)
            return;

        // Recall: despawn and mark as stored again
        activePokemon.Despawn();
        activePokemon = null;

        mode = BallMode.Full; // pokemon back "inside" the ball
        nextRetrievalTime = Mathf.NegativeInfinity;

        CaptureBounce(); // small rebound feedback
        Invoke(nameof(ReturnToPool), recallDelay);
    }

    private void HandleEmptyBallGroundHit(Collision col)
    {
        // If it hits ground and didn't capture anything, just go back to pool
        Invoke(nameof(ReturnToPool), recallDelay);
    }

    private void HandleEmptyBallPokemonTrigger(PokemonController pokemon)
    {
        if (isCapturing) return;

        // Only capture roaming Pokémon
        var behavior = pokemon.GetComponent<PokemonBehaviorManager>();
        if (behavior != null && behavior.CurrentState != PokemonState.Roaming)
            return;

        // Avoid weird self-case
        if (pokemon == activePokemon) return;

        isCapturing = true;
        captureRoutine = StartCoroutine(CaptureSequence(pokemon));
    }

    private IEnumerator CaptureSequence(PokemonController pokemon)
    {
        isCapturing = true;
        activePokemon = pokemon;

        // Datos del pokémon
        string speciesKey = GetPokemonSpeciesKey(pokemon);
        Vector3 pokemonPos = pokemon.transform.position;

        // “Entra” en la pokeball, despawn con FX
        pokemon.Despawn();
        var behavior = pokemon.GetComponent<PokemonBehaviorManager>();
        if (behavior != null)
            behavior.EnterRoaming(); // aseguramos que vuelva a roam si lo respawneamos

        CaptureBounce();

        // Dejar que termine el rebote
        float t = 0.25f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        // Asegurar que la pokeball quede apoyada en el suelo para el "Shake"
        SnapToGround();

        Quaternion baseRot = transform.rotation;
        bool escaped = false;

        for (int i = 0; i < shakesCount; i++)
        {
            // Pequeña pausa antes del movimiento
            float prePause = timeBetweenShakes * 0.25f;
            t = prePause;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                yield return null;
            }

            // Animación de sacudida
            float duration = timeBetweenShakes * 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float n = elapsed / duration;          // 0–1
                float angle = Mathf.Sin(n * Mathf.PI * 2f) * shakeAngle;
                transform.rotation = baseRot * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            transform.rotation = baseRot;

            // Tirada de escape en este “Shake”
            float roll = Random.value;
            if (roll > stayChancePerShake)
            {
                escaped = true;
                break;
            }

            // Pequeña pausa después del movimiento
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
            // El pokémon escapa, respawn en el punto original
            var go = PoolManager.I.Spawn(speciesKey, pokemonPos, Quaternion.identity);
            var newPokemon = go.GetComponent<PokemonController>();
            newPokemon?.Init();

            var newBehavior = go.GetComponent<PokemonBehaviorManager>();
            newBehavior?.EnterRoaming();
        }
        else
        {
            // Captura exitosa, mandar al PC / inventario
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddCapturedPokemon(speciesKey);
                InventoryManager.Instance.SpendPokeball();
            }

            //TO DO: chequer si esto es suficiente para mandar al pokemon a la lista de la pc
        }

        // Pequeña espera para ver el resultado y luego volver al cinturón/pool
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
            // mantener yaw, pero nivelar la pokeball
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
        else
        {
            // fallback: simplemente parar la física
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }


    // --- Helpers -------------------------------------------------------------

    private string GetPokemonSpeciesKey(PokemonController pokemon)
    {
        if (pokemon != null)
        {
            string key = pokemon.GetPoolKey();
            if (!string.IsNullOrEmpty(key))
            {
                return key;
            }
        }
        return assignedSpeciesPoolKey ?? "Unknown";
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

    private void ReturnToPool()
    {
        // Solo establecer velocidad si el cuerpo no es cinemático
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Notificar al pool manager para que devuelva esta pokeball al pool
        if (pokeballPool != null)
        {
            pokeballPool.ReturnPokeballToPool(this);
            Debug.Log("Pokeball returned to pool");
        }
        else
        {
            // Si no hay pool manager, simplemente desactivar
            gameObject.SetActive(false);
            Debug.Log("No hay pool manager, desactivar");
        }
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

    private void DockToBeltImmediate()
    {
        if (beltAttach != null)
            transform.SetPositionAndRotation(beltAttach.position, beltAttach.rotation);
        MakeKinematicDocked();
    }

    private void TrySocketSelect()
    {
        if (!beltSocket) return;

        var interactable = this as IXRSelectInteractable;
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

    // --- Public methods para configurar la pokeball -------------------------

    public void SetMode(BallMode newMode)
    {
        mode = newMode;
    }

    public BallMode GetMode() => mode;

    public void SetAssignedSpecies(string speciesPoolKey)
    {
        assignedSpeciesPoolKey = speciesPoolKey;
    }

    public string GetAssignedSpecies() => assignedSpeciesPoolKey;

    public void SetBeltSocket(XRSocketInteractor socket)
    {
        beltSocket = socket;
    }

    public void SetBeltAttach(Transform attach)
    {
        beltAttach = attach;
    }
}

