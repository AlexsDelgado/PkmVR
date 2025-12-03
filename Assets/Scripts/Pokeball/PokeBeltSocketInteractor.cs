using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum BeltSocketType
{
    EmptyPokeball,  // Socket 1 - always shows a generic empty ball
    TeamPokemon     // Sockets 2-7 - team balls bound to party slots
}

public class PokeBeltSocketInteractor : XRSocketInteractor
{
    [Header("Socket Configuration")]
    [SerializeField] private BeltSocketType socketType = BeltSocketType.EmptyPokeball;
    [SerializeField] private Transform attachPoint;
    [SerializeField] private int teamIndex = 0; // 0-5 for team slots

    [Header("Team Ball")]
    [Tooltip("Prefab used for team balls (if left empty, will use PokeballPoolManager.CreateTeamPokeball).")]
    [SerializeField] private PokeballGrabInteractable teamBallPrefab;

    private PokeballPoolManager poolManager;
    private PokeballGrabInteractable teamBall;

    protected override void Awake()
    {
        base.Awake();

        poolManager = PokeballPoolManager.Instance ?? FindFirstObjectByType<PokeballPoolManager>();

        if (attachPoint == null)
            attachPoint = transform;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPokeballsChanged += OnPokeballsInventoryChanged;
            InventoryManager.Instance.OnTeamChanged += OnTeamChanged;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPokeballsChanged -= OnPokeballsInventoryChanged;
            InventoryManager.Instance.OnTeamChanged -= OnTeamChanged;
        }
    }

    protected override void Start()
    {
        RefreshSocket();
    }

    // Refresh logic

    private void RefreshSocket()
    {
        if (socketType == BeltSocketType.EmptyPokeball)
            RefreshEmptySocket();
        else
            RefreshTeamSocket();
    }

    private void RefreshEmptySocket()
    {
        var inv = InventoryManager.Instance;
        if (inv == null || poolManager == null)
            return;

        int count = inv.GetPokeballs();

        // If no pokeballs left clear any selection
        if (count <= 0)
        {
            if (hasSelection && interactablesSelected.Count > 0)
            {
                var currentInteractable = interactablesSelected[0];
                var mb = currentInteractable as MonoBehaviour;
                var ball = mb ? mb.GetComponent<PokeballGrabInteractable>() : null;

                if (ball != null)
                {
                    interactionManager?.SelectExit(this, currentInteractable);
                    poolManager.ReturnPokeballToPool(ball);
                }
            }
            return;
        }

        // We have pokeballs in inventory
        if (hasSelection && interactablesSelected.Count > 0)
        {
            // assume current selection is already a proper Empty ball
            return;
        }

        var newBall = poolManager.GetEmptyPokeball();
        if (newBall == null)
            return;

        newBall.SetMode(PokeballGrabInteractable.BallMode.Empty);
        newBall.SetAssignedSpecies(null);
        newBall.SetBeltSocket(this);
        newBall.SetBeltAttach(attachPoint);

        newBall.transform.SetPositionAndRotation(attachPoint.position, attachPoint.rotation);

        var interactable = newBall as IXRSelectInteractable;
        if (interactionManager != null && interactable != null && CanSelect(interactable))
        {
            interactionManager.SelectEnter(this, interactable);
        }
    }

    private void RefreshTeamSocket()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        var captured = inv.GetCapturedPokemons();
        if (captured == null || teamIndex < 0 || teamIndex >= captured.Count)
        {
            // ... (your existing "no pokemon for this slot" branch)
            return;
        }

        var entry = captured[teamIndex];
        string speciesKey = entry.speciesPoolKey;

        if (teamBall == null)
        {
            if (teamBallPrefab != null)
                teamBall = Instantiate(teamBallPrefab);
            else if (poolManager != null)
                teamBall = poolManager.CreateTeamPokeball();
        }

        if (teamBall == null) return;

        // If ball already matches species and is in Team mode, keep that state (pokemon is out)
        bool pokemonIsOut = teamBall.GetMode() == PokeballGrabInteractable.BallMode.Team &&
                            teamBall.GetAssignedSpecies() == speciesKey;

        teamBall.gameObject.SetActive(true);
        teamBall.SetAssignedSpecies(speciesKey);
        teamBall.SetBeltSocket(this);
        teamBall.SetBeltAttach(attachPoint);

        if (!pokemonIsOut)
        {
            // Only force Full when we either changed species or the pokemon is not out
            teamBall.SetMode(PokeballGrabInteractable.BallMode.Full);
        }

        teamBall.transform.SetPositionAndRotation(attachPoint.position, attachPoint.rotation);

        var interactable = teamBall as IXRSelectInteractable;
        if (interactionManager != null && interactable != null)
        {
            if (hasSelection && interactablesSelected.Count > 0)
            {
                interactionManager.SelectExit(this, interactablesSelected[0]);
            }
            if (CanSelect(interactable))
                interactionManager.SelectEnter(this, interactable);
        }
    }

    // Event handlers

    private void OnPokeballsInventoryChanged(int newCount)
    {
        if (socketType == BeltSocketType.EmptyPokeball)
            RefreshSocket();
    }

    private void OnTeamChanged()
    {
        if (socketType == BeltSocketType.TeamPokemon)
            RefreshSocket();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        // Only auto-refresh empty socket when player takes the ball
        if (socketType == BeltSocketType.EmptyPokeball)
        {
            StartCoroutine(RefreshSocketDelayed());
        }
    }

    private IEnumerator RefreshSocketDelayed()
    {
        // wait a frame so the interaction manager finishes its bookkeeping
        yield return null;
        RefreshSocket();
    }

    // Public helpers

    public void SetAttachPoint(Transform point)
    {
        attachPoint = point;
    }

    public BeltSocketType GetSocketType() => socketType;
    public int GetTeamIndex() => teamIndex;
}