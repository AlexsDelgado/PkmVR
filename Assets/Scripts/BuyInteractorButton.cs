using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BuyInteractorButton : XRSimpleInteractable
{
    [Header("Referencias")]
    [SerializeField] private BuyBox buyBox;

    protected override void Awake()
    {
        base.Awake();
        
        // Si no se asignó la caja en el inspector, intentar encontrarla
        if (buyBox == null)
        {
            buyBox = FindObjectOfType<BuyBox>();
            if (buyBox == null)
            {
                Debug.LogWarning("BuyBox no encontrado. Asigna la referencia en el inspector.", this);
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        selectEntered.AddListener(OnSelectEntered);
    }

    protected override void OnDisable()
    {
        selectEntered.RemoveListener(OnSelectEntered);
        base.OnDisable();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Cuando se selecciona el botón, intentar realizar la compra
        AttemptPurchase();
    }

    /// <summary>
    /// Función pública que se puede llamar para intentar realizar la compra
    /// </summary>
    public void AttemptPurchase()
    {
        if (buyBox == null)
        {
            Debug.LogError("BuyBox no asignado. No se puede realizar la compra.", this);
            return;
        }

        buyBox.TryPurchase();
    }
}
