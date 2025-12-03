using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PokeballPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class PokeballPool
    {
        public string key = "PokeballGrabInteractable";
        public GameObject prefab;
        public int prewarm = 10;
    }

    public static PokeballPoolManager Instance { get; private set; }

    [Header("Pool Configuration")]
    [SerializeField] private PokeballPool poolConfig = new PokeballPool();

    private Queue<PokeballGrabInteractable> availablePokeballs = new Queue<PokeballGrabInteractable>();
    private List<PokeballGrabInteractable> activePokeballs = new List<PokeballGrabInteractable>();
    private Dictionary<PokeballGrabInteractable, bool> pokeballStates = new Dictionary<PokeballGrabInteractable, bool>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Precalentar el pool
        PrewarmPool();
    }

    private void PrewarmPool()
    {
        if (poolConfig.prefab == null)
        {
            Debug.LogError("PokeballPoolManager: Prefab no asignado", this);
            return;
        }

        for (int i = 0; i < poolConfig.prewarm; i++)
        {
            var pokeball = CreatePokeball();
            pokeball.gameObject.SetActive(false);
            availablePokeballs.Enqueue(pokeball);
        }
    }

    private PokeballGrabInteractable CreatePokeball()
    {
        var go = Instantiate(poolConfig.prefab);
        var pokeball = go.GetComponent<PokeballGrabInteractable>();
        
        if (pokeball == null)
        {
            Debug.LogError("PokeballPoolManager: El prefab no tiene componente PokeballGrabInteractable", this);
            Destroy(go);
            return null;
        }

        return pokeball;
    }

    /// <summary>
    /// Obtiene una pokeball del pool (modo Empty)
    /// </summary>
    public PokeballGrabInteractable GetEmptyPokeball()
    {
        // Verificar si hay pokeballs en el inventario
        if (InventoryManager.Instance == null || InventoryManager.Instance.GetPokeballs() <= 0)
        {
            Debug.Log("No hay pokeballs en el inventario");
            return null;
        }

        PokeballGrabInteractable pokeball;

        // Intentar obtener una del pool disponible
        if (availablePokeballs.Count > 0)
        {
            pokeball = availablePokeballs.Dequeue();
        }
        else
        {
            // Crear una nueva si no hay disponibles
            pokeball = CreatePokeball();
        }

        if (pokeball != null)
        {
            pokeball.SetMode(PokeballGrabInteractable.BallMode.Empty);
            pokeball.gameObject.SetActive(true);
            activePokeballs.Add(pokeball);
            pokeballStates[pokeball] = true;
        }

        return pokeball;
    }

    /// <summary>
    /// Obtiene una pokeball del pool (modo Captured) con un pokemon asignado
    /// </summary>
    public PokeballGrabInteractable GetCapturedPokeball(string speciesPoolKey)
    {
        PokeballGrabInteractable pokeball;

        // Intentar obtener una del pool disponible
        if (availablePokeballs.Count > 0)
        {
            pokeball = availablePokeballs.Dequeue();
        }
        else
        {
            // Crear una nueva si no hay disponibles
            pokeball = CreatePokeball();
        }

        if (pokeball != null)
        {
            pokeball.SetMode(PokeballGrabInteractable.BallMode.Full);
            pokeball.SetAssignedSpecies(speciesPoolKey);
            pokeball.gameObject.SetActive(true);
            activePokeballs.Add(pokeball);
            pokeballStates[pokeball] = true;
        }

        return pokeball;
    }

    /// <summary>
    /// Devuelve una pokeball al pool
    /// </summary>
    public void ReturnPokeballToPool(PokeballGrabInteractable pokeball)
    {
        if (pokeball == null) return;

        if (pokeball.GetMode() != BallMode.Empty)
        {
            Debug.LogWarning("Tried to return a non-empty team ball to pool – this should not happen.");
            return;
        }

        // Remover de la lista de activas
        if (activePokeballs.Contains(pokeball))
        {
            activePokeballs.Remove(pokeball);
        }

        if (pokeballStates.ContainsKey(pokeball))
        {
            pokeballStates.Remove(pokeball);
        }

        // Desactivar y devolver al pool
        pokeball.gameObject.SetActive(false);
        pokeball.transform.SetParent(null);
        
        // Resetear estado
        pokeball.SetMode(PokeballGrabInteractable.BallMode.Empty);
        pokeball.SetAssignedSpecies(null);

        availablePokeballs.Enqueue(pokeball);
    }

    /// <summary>
    /// Se llama cuando se agarra una pokeball del socket 1
    /// Los sockets ahora se auto-gestionan, así que este método ya no es necesario
    /// pero se mantiene por compatibilidad
    /// </summary>
    public void OnPokeballGrabbed()
    {
        // Los sockets ahora se refrescan automáticamente en OnSelectExited
        // Este método se mantiene por compatibilidad pero no hace nada
    }

    /// <summary>
    /// Obtiene la cantidad de pokeballs disponibles en el pool
    /// </summary>
    public int GetAvailableCount() => availablePokeballs.Count;

    /// <summary>
    /// Obtiene la cantidad de pokeballs activas
    /// </summary>
    public int GetActiveCount() => activePokeballs.Count;
}

