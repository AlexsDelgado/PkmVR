using UnityEngine;

public class PokemonController : MonoBehaviour
{
    [SerializeField] private string poolKey;

    [SerializeField] private PokemonFXController pfx;
    [SerializeField] private string fxSwirlKey = "fx_swirl";
    private GameObject activeSwirl;

    public void Init() 
    { 
        pfx?.PlayDissolveIn();

        if (!string.IsNullOrEmpty(fxSwirlKey))
        {
            activeSwirl = PoolManager.I.Spawn(
                fxSwirlKey, transform.position, Quaternion.identity);

            if (activeSwirl)
                activeSwirl.transform.SetParent(transform, true);
        }
    }

    public void DespawnFXThenReturn()
    {
        if (pfx) pfx.PlayDissolveOut(() => PoolManager.I.Despawn(poolKey, gameObject));
        else PoolManager.I.Despawn(poolKey, gameObject);
    }
    public void Despawn() => DespawnFXThenReturn();

    /// <summary>
    /// Obtiene la clave del pool de este pokemon
    /// </summary>
    public string GetPoolKey() => poolKey;
    //ok
}
