using UnityEngine;

public class PokemonController : MonoBehaviour
{
    [SerializeField] private string poolKey;
    public void Init() { /* TODO VFX/SFX */ }
    public void Despawn() => PoolManager.I.Despawn(poolKey, gameObject);
}
