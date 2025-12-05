using System.Collections;
using UnityEngine;

public class PokemonController : MonoBehaviour
{
    [SerializeField] private string poolKey;
    public PokemonData pkm_data;
    public AudioSource a_source;

    [SerializeField] private PokemonFXController pfx;
    [SerializeField] private string fxSwirlKey = "fx_swirl";
    private GameObject activeSwirl;

    public void Init() 
    {
        a_source.clip = pkm_data.cry;
        StartCoroutine(CryRutine());
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

    public IEnumerator CryRutine()
    {
        a_source.Play();
        yield return new WaitForSeconds(Random.Range(8, 15));
        StartCoroutine(CryRutine());
    }
}
