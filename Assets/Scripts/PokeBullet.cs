using UnityEngine;

public class PokeBullet : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 1000f;
    [SerializeField] private float bulletLifetime = 5f; // Destruir bala después de 5 segundos si no colisiona
    
    private void Start()
    {
        // Asegurar que el objeto tenga Rigidbody
        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
        }
        
        // Configurar el Rigidbody para detección de colisiones
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Las balas no deberían caer por gravedad
        rb.drag = 0f; // Sin resistencia al aire
        
        // Asegurar que el objeto tenga Collider
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<SphereCollider>();
        }
        
        // Configurar el Collider
        Collider col = GetComponent<Collider>();
        col.isTrigger = false; // Debe ser false para OnCollisionEnter
        
        Shoot();
        
        // Destruir la bala después de un tiempo si no colisiona
        Destroy(gameObject, bulletLifetime);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"PokeBullet colisionó con: {collision.gameObject.name}");
        Debug.Log("¡Colisión con pared detectada! Squirtle pereció.");
        Destroy(gameObject);
        
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Método alternativo usando triggers
        Debug.Log($"PokeBullet trigger con: {other.gameObject.name}");
        
        if (other.gameObject.name.Contains("cube") || other.gameObject.name.Contains("wall") || other.gameObject.name.Contains("pared"))
        {
            Debug.Log("¡Trigger con pared detectado! Squirtle pereció.");
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
