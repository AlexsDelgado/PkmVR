using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum ItemType
{
    Pokeball,
    Potion
}

[RequireComponent(typeof(Collider))]
public class PokeBuyableDirectInteractor : XRGrabInteractable
{
    [Header("Configuración del Item")]
    [SerializeField] private ItemType itemType = ItemType.Pokeball;
    [SerializeField] private int quantity = 1; // Cantidad que se agrega al inventario
    [SerializeField] private int price = 100; // Precio del item

    /// <summary>
    /// Verifica si este item es una pokeball
    /// </summary>
    public bool IsPokeball()
    {
        return itemType == ItemType.Pokeball;
    }

    /// <summary>
    /// Verifica si este item es una poción
    /// </summary>
    public bool IsPotion()
    {
        return itemType == ItemType.Potion;
    }

    /// <summary>
    /// Obtiene el tipo de item
    /// </summary>
    public ItemType GetItemType()
    {
        return itemType;
    }

    /// <summary>
    /// Obtiene la cantidad que se agrega al inventario
    /// </summary>
    public int GetQuantity()
    {
        return quantity;
    }

    /// <summary>
    /// Obtiene el precio del item
    /// </summary>
    public int GetPrice()
    {
        return price;
    }

    /// <summary>
    /// Establece el precio del item (útil para configurar en runtime)
    /// </summary>
    public void SetPrice(int newPrice)
    {
        price = newPrice;
    }

    /// <summary>
    /// Establece la cantidad del item (útil para configurar en runtime)
    /// </summary>
    public void SetQuantity(int newQuantity)
    {
        quantity = newQuantity;
    }
}

