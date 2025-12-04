using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum BeltSocketType
{
    EmptyPokeball,   // socket for generic capture ball
    TeamPokemon      // socket bound to a team slot
}

public class PokeBeltSocketInteractor : XRSocketInteractor
{
    [Header("Socket Configuration")]
    [SerializeField] private BeltSocketType socketType = BeltSocketType.EmptyPokeball;
    [SerializeField] private Transform attachPoint;
    [Tooltip("Index in the captured pokémon list for Team sockets (0–5).")]
    [SerializeField] private int teamIndex = 0;

    [Header("Ball Prefabs")]
    [Tooltip("Prefab used for the EMPTY capture ball in this socket.")]
    [SerializeField] private PokeballGrabInteractable emptyBallPrefab;

    [Tooltip("Prefab used for TEAM balls (per team socket). If null, emptyBallPrefab is used.")]
    [SerializeField] private PokeballGrabInteractable teamBallPrefab;

    // persistent instances
    private PokeballGrabInteractable emptyBallInstance;
    private PokeballGrabInteractable teamBallInstance;

    // --------------------------------------------------------------------
    // Lifecycle
    // --------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();

        if (attachPoint == null)
            attachPoint = transform;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // Event subscription is deferred to Start via coroutine
    }

    protected override void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPokeballsChanged -= OnPokeballsChanged;
            InventoryManager.Instance.OnTeamChanged -= OnTeamChanged;
        }

        base.OnDisable();
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(InitAfterInventory());
    }

    private IEnumerator InitAfterInventory()
    {
        // Wait until InventoryManager singleton exists
        while (InventoryManager.Instance == null)
            yield return null;

        InventoryManager.Instance.OnPokeballsChanged += OnPokeballsChanged;
        InventoryManager.Instance.OnTeamChanged += OnTeamChanged;

        RefreshSocket();
    }

    // --------------------------------------------------------------------
    // XR selection hook – only empty socket cares when ball leaves
    // --------------------------------------------------------------------

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (socketType != BeltSocketType.EmptyPokeball)
            return;

        // If our empty ball was taken, re-dock it later (same instance)
        var mb = args.interactableObject as MonoBehaviour;
        var ball = mb ? mb.GetComponent<PokeballGrabInteractable>() : null;

        if (ball != null && ball == emptyBallInstance)
            StartCoroutine(RefreshSocketDelayed());
    }

    private IEnumerator RefreshSocketDelayed()
    {
        yield return null;
        RefreshSocket();
    }

    // --------------------------------------------------------------------
    // Main refresh
    // --------------------------------------------------------------------

    private void RefreshSocket()
    {
        if (socketType == BeltSocketType.EmptyPokeball)
            RefreshEmptySocket();
        else
            RefreshTeamSocket();
    }

    // --------------------------------------------------------------------
    // EMPTY SOCKET (generic capture ball, persistent)
    // --------------------------------------------------------------------

    private void RefreshEmptySocket()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
            return;

        int count = inv.GetPokeballs();
        bool shouldBeVisible = count > 0;

        // Lazily obtain an instance once (prefer the pool, fall back to prefab)
        if (emptyBallInstance == null)
        {
            // Try to get one from the pool
            if (PokeballPoolManager.Instance != null)
            {
                emptyBallInstance = PokeballPoolManager.Instance.GetOrCreatePokeball();
            }

            // If pool empty or not set up, fall back to prefab
            if (emptyBallInstance == null && emptyBallPrefab != null)
            {
                emptyBallInstance = Instantiate(emptyBallPrefab);
            }

            if (emptyBallInstance != null)
            {
                ConfigureBallForSocket(emptyBallInstance);
                emptyBallInstance.SetMode(PokeballGrabInteractable.BallMode.Empty);
                emptyBallInstance.SetAssignedSpecies(null);
            }
        }

        if (emptyBallInstance == null)
            return;

        // Hide / show based on inventory
        emptyBallInstance.gameObject.SetActive(shouldBeVisible);
        if (!shouldBeVisible)
        {
            if (hasSelection && interactablesSelected.Count > 0)
                interactionManager?.SelectExit(this, interactablesSelected[0]);
            return;
        }

        // Ensure it's docked & selected
        DockAndSelectBall(emptyBallInstance);
    }

    // --------------------------------------------------------------------
    // TEAM SOCKETS (one persistent ball per team slot)
    // --------------------------------------------------------------------

    private void RefreshTeamSocket()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
            return;

        var captured = inv.GetCapturedPokemons();
        if (captured == null || teamIndex < 0 || teamIndex >= captured.Count)
        {
            // No pokémon for this slot -> hide ball if we have one
            if (teamBallInstance != null)
            {
                if (hasSelection && interactablesSelected.Count > 0)
                    interactionManager?.SelectExit(this, interactablesSelected[0]);
                teamBallInstance.gameObject.SetActive(false);
            }
            return;
        }

        string speciesKey = captured[teamIndex].speciesPoolKey;

        // Lazily create instance
        if (teamBallInstance == null)
        {
            var prefab = teamBallPrefab != null ? teamBallPrefab : emptyBallPrefab;
            if (prefab == null)
                return;

            teamBallInstance = Instantiate(prefab);
            ConfigureBallForSocket(teamBallInstance);
        }

        if (teamBallInstance == null)
            return;

        teamBallInstance.gameObject.SetActive(true);
        teamBallInstance.SetAssignedSpecies(speciesKey);

        // If its pokémon is not currently out, keep it Full
        if (teamBallInstance.GetMode() != PokeballGrabInteractable.BallMode.Team)
        {
            teamBallInstance.SetMode(PokeballGrabInteractable.BallMode.Full);
        }

        DockAndSelectBall(teamBallInstance);
    }

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------

    private void ConfigureBallForSocket(PokeballGrabInteractable ball)
    {
        ball.SetBeltSocket(this);
        ball.SetBeltAttach(attachPoint);

        // Parent to the belt so it follows it
        var pt = attachPoint != null ? attachPoint : transform;
        ball.transform.SetParent(pt, false);              // local space of socket
        ball.transform.localPosition = Vector3.zero;
        ball.transform.localRotation = Quaternion.identity;
        ball.transform.localScale = Vector3.one;
    }

    private void DockAndSelectBall(PokeballGrabInteractable ball)
    {
        // Only manage XR selection – no manual parenting.
        var interactable = ball as IXRSelectInteractable;
        if (interactionManager != null && interactable != null)
        {
            if (hasSelection && interactablesSelected.Count > 0 &&
                interactablesSelected[0] != interactable)
            {
                interactionManager.SelectExit(this, interactablesSelected[0]);
            }

            if (!hasSelection && CanSelect(interactable))
            {
                interactionManager.SelectEnter(this, interactable);
            }
        }
    }

    // Called by PokeballGrabInteractable when an EMPTY belt ball
    // has been returned to the global pool (capture attempt finished).
    public void OnEmptyBallReturnedToPool(PokeballGrabInteractable ball)
    {
        if (socketType != BeltSocketType.EmptyPokeball)
            return;

        // If this socket was tracking that instance, forget it
        if (emptyBallInstance == ball)
            emptyBallInstance = null;

        // Ask the socket to refresh: if inventory still has pokéballs,
        // it will spawn / show a new one; if not, the socket hides.
        RefreshSocket();
    }

    // --------------------------------------------------------------------
    // Inventory events
    // --------------------------------------------------------------------

    private void OnPokeballsChanged(int newCount)
    {
        if (socketType == BeltSocketType.EmptyPokeball)
            RefreshSocket();
    }

    private void OnTeamChanged()
    {
        if (socketType == BeltSocketType.TeamPokemon)
            RefreshSocket();
    }

    // Used by PokeballGrabInteractable.ReturnToPool if you still need it
    public BeltSocketType GetSocketType() => socketType;
}