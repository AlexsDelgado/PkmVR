using System.Collections.Generic;
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
    
    [Header("Posición Original")]
    [SerializeField] private Transform originalPosition; // Transform que define la posición original del item en la tienda
    
    private Vector3 originalPos;
    private Quaternion originalRot;

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

    protected override void Awake()
    {
        base.Awake();
        
        // Guardar la posición original
        if (originalPosition != null)
        {
            originalPos = originalPosition.position;
            originalRot = originalPosition.rotation;
        }
        else
        {
            // Si no se asignó un Transform, usar la posición actual del objeto
            originalPos = transform.position;
            originalRot = transform.rotation;
        }
    }

    /// <summary>
    /// Mueve el objeto a su posición original
    /// </summary>
    public void ReturnToOriginalPosition()
    {
        // Si está siendo agarrado, soltarlo primero
        if (isSelected && interactionManager != null)
        {
            // Crear una copia de la lista de interactors para evitar modificaciones durante la iteración
            var interactorsList = new List<IXRSelectInteractor>(interactorsSelecting);
            
            // Forzar la deselección de todos los interactors
            foreach (var interactor in interactorsList)
            {
                if (interactor != null)
                {
                    interactionManager.SelectExit(interactor, this);
                }
            }
        }

        // Mover a la posición original
        transform.position = originalPos;
        transform.rotation = originalRot;
        
        // Resetear la velocidad del rigidbody si existe
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Actualiza la posición original desde el Transform asignado
    /// </summary>
    public void UpdateOriginalPosition()
    {
        if (originalPosition != null)
        {
            originalPos = originalPosition.position;
            originalRot = originalPosition.rotation;
        }
    }
}

