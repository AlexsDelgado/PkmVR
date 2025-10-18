using UnityEngine;

public class PokeBullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Squirtle perecio");
        Destroy(gameObject);
    }
}
