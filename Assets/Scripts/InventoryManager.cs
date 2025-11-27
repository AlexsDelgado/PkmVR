using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventario")]
    [SerializeField] private int money = 1000;
    [SerializeField] private int pokeballs = 0;
    [SerializeField] private int potions = 0;

    // Eventos para notificar cambios (opcional, útil para UI)
    public System.Action<int> OnMoneyChanged;
    public System.Action<int> OnPokeballsChanged;
    public System.Action<int> OnPotionsChanged;

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
}

