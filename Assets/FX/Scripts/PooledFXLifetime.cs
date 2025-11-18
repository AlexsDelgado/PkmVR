using UnityEngine;

public class PooledFXLifetime : MonoBehaviour
{
    public float lifetime = 0.2f;
    private float _timer;

    void OnEnable()
    {
        _timer = 0f;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifetime)
        {
            gameObject.SetActive(false); // Your pool should re-use it.
        }
    }
}
