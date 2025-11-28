using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BuyBox : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool isTrigger = true;
    
    private HashSet<PokeBuyableDirectInteractor> itemsInBox = new HashSet<PokeBuyableDirectInteractor>();
    private Collider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<Collider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = isTrigger;
        }
        else
        {
            Debug.LogError("BuyBox requiere un Collider para detectar items", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var buyableItem = other.GetComponent<PokeBuyableDirectInteractor>();
        if (buyableItem != null)
        {
            itemsInBox.Add(buyableItem);
            Debug.Log($"Item agregado a la caja: {other.gameObject.name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        var buyableItem = other.GetComponent<PokeBuyableDirectInteractor>();
        if (buyableItem != null)
        {
            itemsInBox.Remove(buyableItem);
            Debug.Log($"Item removido de la caja: {other.gameObject.name}");
        }
    }

    /// <summary>
    /// Función pública que chequea los items en la caja e intenta realizar la compra
    /// </summary>
    /// <returns>True si la compra fue exitosa, False si falló</returns>
    public bool TryPurchase()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager no encontrado en la escena", this);
            return false;
        }

        if (itemsInBox.Count == 0)
        {
            Debug.Log("No hay items en la caja para comprar");
            return false;
        }

        int totalCost = 0;
        int pokeballsToAdd = 0;
        int potionsToAdd = 0;

        // Calcular el costo total y las cantidades a agregar
        foreach (var item in itemsInBox)
        {
            if (item == null) continue;

            int itemCost = item.GetPrice();
            totalCost += itemCost;

            if (item.IsPokeball())
            {
                pokeballsToAdd += item.GetQuantity();
            }
            else if (item.IsPotion())
            {
                potionsToAdd += item.GetQuantity();
            }
        }

        // Verificar si hay suficiente dinero
        if (!InventoryManager.Instance.SpendMoney(totalCost))
        {
            Debug.LogWarning($"No hay suficiente dinero. Costo total: {totalCost}");
            return false;
        }

        // Agregar items al inventario
        if (pokeballsToAdd > 0)
        {
            InventoryManager.Instance.AddPokeballs(pokeballsToAdd);
        }

        if (potionsToAdd > 0)
        {
            InventoryManager.Instance.AddPotions(potionsToAdd);
        }

        Debug.Log($"Compra exitosa! Costo: {totalCost}, Pokeballs: +{pokeballsToAdd}, Pociones: +{potionsToAdd}");
        
        // Mover todos los items a su posición original antes de limpiar la lista
        foreach (var item in itemsInBox)
        {
            if (item != null)
            {
                item.ReturnToOriginalPosition();
            }
        }
        
        // Limpiar la lista después de la compra exitosa
        itemsInBox.Clear();
        
        return true;
    }

    /// <summary>
    /// Obtiene la cantidad de items actualmente en la caja
    /// </summary>
    public int GetItemCount()
    {
        // Limpiar items nulos
        itemsInBox.RemoveWhere(item => item == null);
        return itemsInBox.Count;
    }
}
