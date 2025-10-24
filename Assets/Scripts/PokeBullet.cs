using UnityEngine;

public class PokeBullet : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 1000f;
    private void Start()
    {
        Shoot();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer==13)
        {
            Debug.Log($"PokeBullet colisionó con: {collision.gameObject.name}");
            Debug.Log("¡Colisión con pared detectada! Squirtle pereció.");
            Destroy(gameObject);
        }
        
    }

    public void Shoot()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(transform.forward * bulletSpeed);
        }
        else
        {
            Debug.LogError("No se encontró Rigidbody en PokeBullet");
        }
    }
}
