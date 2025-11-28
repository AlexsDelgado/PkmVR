using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PokeballGrabInteractable : XRGrabInteractable
{
    public enum BallMode { Empty, Captured }

    [Header("Config")]
    [SerializeField] private BallMode mode = BallMode.Empty;
    [SerializeField] private string assignedSpeciesPoolKey; // Solo usado en modo Captured
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float recallDelay = 0.12f;

    [Header("Belt Socket")]
    [SerializeField] private XRSocketInteractor beltSocket;      
    [SerializeField] private Transform beltAttach;

    [Header("Retrieval Cooldown")]
    [SerializeField] private float retrievalCooldown = 0.8f; 
    [SerializeField] private float spawnLift = 0.2f;          
    private float nextRetrievalTime = Mathf.NegativeInfinity;

    [Header("Capture Settings")]
    [SerializeField] private float captureBounceForce = 4f;
    [SerializeField] private Vector2 captureSidewaysRandom = new Vector2(-0.5f, 0.5f);
    [SerializeField] private float captureSuccessChance = 0.5f; // Probabilidad de captura exitosa

    private Rigidbody rb;
    private PokemonController activePokemon;
    private bool isCapturing = false; // Para evitar múltiples intentos de captura

    [SerializeField] private BallFXController fx;
    // Referencia al pool manager de pokeballs
    private PokeballPoolManager pokeballPool;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        pokeballPool = FindObjectOfType<PokeballPoolManager>();

        // No necesitamos suscribirnos a los eventos porque ya estamos sobrescribiendo
        // los métodos OnSelectEntered y OnSelectExited que se llaman automáticamente
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

    private System.Collections.IEnumerator NotifyPokeballGrabbedDelayed()
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

        // Modo Captured: spawnea pokemon al tocar el suelo
        if (mode == BallMode.Captured && IsGround(col.gameObject.layer))
        {
            var cp = col.GetContact(0);
            var spawnPos = cp.point + cp.normal * spawnLift;
            fx?.PlayImpactSet(cp.point, cp.normal);

            SpawnPokemonAt(spawnPos);
            nextRetrievalTime = Time.time + retrievalCooldown;
            Invoke(nameof(ReturnToPool), recallDelay);
            return;
        }

        // Modo Empty: intentar capturar pokemon o volver al pool si falla
        if (mode == BallMode.Empty)
        {
           
            // Si colisiona con algo que no es un pokemon, volver al pool
            var pokemon = col.gameObject.GetComponentInParent<PokemonController>();
            if (pokemon == null && IsGround(col.gameObject.layer))
            {
                // Colisionó con algo que no es pokemon, volver al pool
                Invoke(nameof(ReturnToPool), recallDelay);
                Debug.Log("Colisionó con el suelo, volver al pool");
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

        // Solo procesar captura en modo Empty
        if (mode != BallMode.Empty) return;
        if (Time.time < nextRetrievalTime) return;
        if (isCapturing) return;

        var pokemon = other.GetComponentInParent<PokemonController>();
        if (pokemon != null && pokemon != activePokemon)
        {
            isCapturing = true;
            AttemptCapture(pokemon);
        }
    }

    // --- Helpers -------------------------------------------------------------

    private void AttemptCapture(PokemonController pokemon)
    {
        // Verificar probabilidad de captura
        bool captureSuccess = Random.Range(0f, 1f) <= captureSuccessChance;

        if (captureSuccess)
        {
            // Captura exitosa
            string speciesKey = GetPokemonSpeciesKey(pokemon);
            
            // Agregar al inventario
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddCapturedPokemon(speciesKey);
                InventoryManager.Instance.SpendPokeball();
            }

            // Despawnear el pokemon
            pokemon.Despawn();
            activePokemon = null;
            
            // Efecto de captura
            CaptureBounce();
            
            // Volver al pool después de la captura
            StartCoroutine(ReturnToPoolAfterCapture());
        }
        else
        {
            // Captura fallida - el pokemon escapa
            activePokemon = pokemon;
            CaptureBounce();
            StartCoroutine(ReturnToPoolAfterFailedCapture());
        }
    }

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

    private void SpawnPokemonAt(Vector3 pos)
    {
        if (string.IsNullOrEmpty(assignedSpeciesPoolKey))
        {
            Debug.LogWarning("PokeballGrabInteractable: assignedSpeciesPoolKey no está asignado");
            return;
        }

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

    private IEnumerator ReturnToPoolAfterCapture()
    {
        yield return new WaitForSeconds(0.35f);
        Debug.Log("Return to pool after capture");
        ReturnToPool();
        isCapturing = false;
    }

    private IEnumerator ReturnToPoolAfterFailedCapture()
    {
        yield return new WaitForSeconds(0.35f);
        Debug.Log("Return to pool after failed capture");
        ReturnToPool();
        isCapturing = false;
        activePokemon = null;
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

