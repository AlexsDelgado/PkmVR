using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum BeltSocketType
{
    EmptyPokeball,  // Socket 1 - siempre tiene una pokeball empty
    TeamPokemon     // Sockets 2-7 - pokeballs captured según el equipo
}

public class PokeBeltSocketInteractor : XRSocketInteractor
{
    [Header("Socket Configuration")]
    [SerializeField] private BeltSocketType socketType = BeltSocketType.EmptyPokeball;
    [SerializeField] private Transform attachPoint; // Punto de attach para este socket específico
    [SerializeField] private int teamIndex = 0; // Índice del pokemon en el equipo (0-5, solo para TeamPokemon)

    private PokeballPoolManager poolManager;    

    protected override void Awake()
    {
        base.Awake();
        
        poolManager = PokeballPoolManager.Instance;
        if (poolManager == null)
        {
            poolManager = FindObjectOfType<PokeballPoolManager>();
        }

        // Si no se asignó attach point, usar el transform del socket
        if (attachPoint == null)
        {
            attachPoint = transform;
        }
    }

    protected override void Start()
    {
        base.Start();
        
        // Suscribirse a eventos del inventario
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPokeballsChanged += OnPokeballsInventoryChanged;
            InventoryManager.Instance.OnTeamChanged += OnTeamChanged;
        }

        // Inicializar el socket según su tipo
        RefreshSocket();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPokeballsChanged -= OnPokeballsInventoryChanged;
            InventoryManager.Instance.OnTeamChanged -= OnTeamChanged;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        
        // Cuando se suelta una pokeball, esperar un frame y refrescar
        StartCoroutine(RefreshSocketDelayed());
    }

    /// <summary>
    /// Refresca el socket según su tipo
    /// </summary>
    public void RefreshSocket()
    {
        if (socketType == BeltSocketType.EmptyPokeball)
        {
            RefreshEmptySocket();
        }
        else if (socketType == BeltSocketType.TeamPokemon)
        {
            RefreshTeamSocket();
        }
    }

    /// <summary>
    /// Refresca el socket 1 (Empty Pokeball)
    /// </summary>
    private void RefreshEmptySocket()
    {
        // Si ya tiene una pokeball, no hacer nada
        if (hasSelection) return;

        // Verificar si hay pokeballs en el inventario
        if (InventoryManager.Instance == null || InventoryManager.Instance.GetPokeballs() <= 0)
        {
            return;
        }

        // Obtener una pokeball empty del pool
        if (poolManager != null)
        {
            var pokeball = poolManager.GetEmptyPokeball();
            if (pokeball != null)
            {
                SetupPokeballInSocket(pokeball);
            }
        }
    }

    /// <summary>
    /// Refresca un socket del equipo (2-7)
    /// </summary>
    private void RefreshTeamSocket()
    {
        if (InventoryManager.Instance == null) return;

        var capturedPokemons = InventoryManager.Instance.GetCapturedPokemons();
        
        // Verificar si hay un pokemon para este índice
        if (teamIndex < capturedPokemons.Count)
        {
            var pokemon = capturedPokemons[teamIndex];
            
            // Si el socket ya tiene la pokeball correcta, no hacer nada
            if (hasSelection && interactablesSelected.Count > 0)
            {
                var currentSelection = interactablesSelected[0];
                var currentPokeball = (currentSelection as MonoBehaviour)?.GetComponent<PokeballGrabInteractable>();
                
                if (currentPokeball != null && 
                    currentPokeball.GetMode() == PokeballGrabInteractable.BallMode.Captured &&
                    currentPokeball.GetAssignedSpecies() == pokemon.speciesPoolKey)
                {
                    return; // Ya tiene la pokeball correcta
                }
                else
                {
                    // Remover la pokeball incorrecta
                    if (currentPokeball != null)
                    {
                        interactionManager?.SelectExit(this, currentSelection);
                        poolManager?.ReturnPokeballToPool(currentPokeball);
                    }
                }
            }

            // Obtener o crear una pokeball captured para este pokemon
            if (poolManager != null)
            {
                var pokeball = poolManager.GetCapturedPokeball(pokemon.speciesPoolKey);
                if (pokeball != null)
                {
                    SetupPokeballInSocket(pokeball);
                }
            }
        }
        else
        {
            // No hay pokemon para este socket, limpiarlo si tiene algo
            if (hasSelection && interactablesSelected.Count > 0)
            {
                var currentSelection = interactablesSelected[0];
                var currentPokeball = (currentSelection as MonoBehaviour)?.GetComponent<PokeballGrabInteractable>();
                
                if (currentPokeball != null)
                {
                    interactionManager?.SelectExit(this, currentSelection);
                    poolManager?.ReturnPokeballToPool(currentPokeball);
                }
            }
        }
    }

    /// <summary>
    /// Configura una pokeball en este socket
    /// </summary>
    private void SetupPokeballInSocket(PokeballGrabInteractable pokeball)
    {
        if (pokeball == null) return;

        // Configurar la pokeball
        pokeball.SetBeltSocket(this);
        pokeball.SetBeltAttach(attachPoint);

        // Posicionar la pokeball en el socket
        pokeball.transform.position = attachPoint.position;
        pokeball.transform.rotation = attachPoint.rotation;

        // Intentar que el socket seleccione la pokeball
        var interactable = pokeball.GetComponent<IXRSelectInteractable>();
        if (interactable != null && CanSelect(interactable))
        {
            interactionManager?.SelectEnter(this, interactable);
        }
    }

    /// <summary>
    /// Limpia el socket (remueve la pokeball actual)
    /// </summary>
    public void ClearSocket()
    {
        if (!hasSelection || interactablesSelected.Count == 0) return;

        var currentSelection = interactablesSelected[0];
        var currentPokeball = (currentSelection as MonoBehaviour)?.GetComponent<PokeballGrabInteractable>();
        
        if (currentPokeball != null)
        {
            interactionManager?.SelectExit(this, currentSelection);
            poolManager?.ReturnPokeballToPool(currentPokeball);
        }
    }

    private void OnPokeballsInventoryChanged(int newCount)
    {
        // Solo afecta al socket 1 (Empty)
        if (socketType == BeltSocketType.EmptyPokeball)
        {
            if (newCount <= 0)
            {
                ClearSocket();
            }
            else
            {
                RefreshSocket();
            }
        }
    }

    private void OnTeamChanged()
    {
        // Solo afecta a los sockets del equipo (2-7)
        if (socketType == BeltSocketType.TeamPokemon)
        {
            RefreshSocket();
        }
    }

    private IEnumerator RefreshSocketDelayed()
    {
        yield return null; // Esperar un frame para que el socket se limpie
        RefreshSocket();
    }

    // --- Public methods para configuración ---

    /// <summary>
    /// Establece el tipo de socket
    /// </summary>
    public void SetSocketType(BeltSocketType type)
    {
        socketType = type;
    }

    /// <summary>
    /// Establece el índice del equipo (solo para TeamPokemon)
    /// </summary>
    public void SetTeamIndex(int index)
    {
        teamIndex = index;
    }

    /// <summary>
    /// Establece el punto de attach
    /// </summary>
    public void SetAttachPoint(Transform point)
    {
        attachPoint = point;
    }

    /// <summary>
    /// Obtiene el tipo de socket
    /// </summary>
    public BeltSocketType GetSocketType() => socketType;

    /// <summary>
    /// Obtiene el índice del equipo
    /// </summary>
    public int GetTeamIndex() => teamIndex;
}
