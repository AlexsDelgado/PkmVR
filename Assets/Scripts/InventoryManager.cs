using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CapturedPokemon
{
    public string speciesPoolKey; // Clave del pool del pokemon (ej: "Squirtle")
    public PokemonData pokemonData; // Datos del pokemon (opcional, para stats, etc)
    
    public CapturedPokemon(string poolKey, PokemonData data = null)
    {
        speciesPoolKey = poolKey;
        pokemonData = data;
    }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventario")]
    [SerializeField] private int money = 1000;
    [SerializeField] private int pokeballs = 0;
    [SerializeField] private int potions = 0;

    [Header("Equipo de Pokemons")]
    [SerializeField] private List<CapturedPokemon> capturedPokemons = new List<CapturedPokemon>();
    private const int MAX_TEAM_SIZE = 6;

    // Eventos para notificar cambios (opcional, útil para UI)
    public System.Action<int> OnMoneyChanged;
    public System.Action<int> OnPokeballsChanged;
    public System.Action<int> OnPotionsChanged;
    public System.Action OnTeamChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Getters
    public int GetMoney() => money;
    public int GetPokeballs() => pokeballs;
    public int GetPotions() => potions;

    // Métodos para agregar items
    public void AddPokeballs(int amount)
    {
        pokeballs += amount;
        OnPokeballsChanged?.Invoke(pokeballs);
        Debug.Log($"Pokeballs agregadas. Total: {pokeballs}");
    }

    public void AddPotions(int amount)
    {
        potions += amount;
        OnPotionsChanged?.Invoke(potions);
        Debug.Log($"Pociones agregadas. Total: {potions}");
    }

    // Métodos para gastar dinero
    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            OnMoneyChanged?.Invoke(money);
            Debug.Log($"Dinero gastado: {amount}. Restante: {money}");
            return true;
        }
        Debug.LogWarning($"No hay suficiente dinero. Tienes: {money}, necesitas: {amount}");
        return false;
    }

    // Método para agregar dinero (útil para testing o recompensas)
    public void AddMoney(int amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(money);
        Debug.Log($"Dinero agregado: {amount}. Total: {money}");
    }

    // Métodos para gastar pokeballs
    public bool SpendPokeball()
    {
        if (pokeballs > 0)
        {
            pokeballs--;
            OnPokeballsChanged?.Invoke(pokeballs);
            Debug.Log($"Pokeball gastada. Restantes: {pokeballs}");
            return true;
        }
        Debug.LogWarning("No hay pokeballs disponibles");
        return false;
    }

    // Métodos para manejar pokemons capturados
    public List<CapturedPokemon> GetCapturedPokemons() => new List<CapturedPokemon>(capturedPokemons);
    
    public int GetCapturedPokemonCount() => capturedPokemons.Count;

    public void AddCapturedPokemon(string poolKey)
    {
        if (string.IsNullOrEmpty(poolKey))
        {
            Debug.LogWarning("[Inventory] Tried to add pokemon with empty poolKey");
            return;
        }

        capturedPokemons.Add(new CapturedPokemon(poolKey));
        Debug.Log($"[Inventory] Added captured Pokémon {poolKey}. Total now: {capturedPokemons.Count}");
        OnTeamChanged?.Invoke();
    }

    public CapturedPokemon GetPokemonAt(int index)
    {
        if (index >= 0 && index < capturedPokemons.Count)
            return capturedPokemons[index];
        return null;
    }
    
    public bool RemovePokemonAt(int index)
    {
        if (index >= 0 && index < capturedPokemons.Count)
        {
            capturedPokemons.RemoveAt(index);
            OnTeamChanged?.Invoke();
            Debug.Log($"Pokemon removido del equipo en índice {index}");
            return true;
        }
        return false;
    }
}

