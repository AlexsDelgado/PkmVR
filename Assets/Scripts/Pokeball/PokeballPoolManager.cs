using System.Collections.Generic;
using UnityEngine;

public class PokeballPoolManager : MonoBehaviour
{
    public static PokeballPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private PokeballGrabInteractable pokeballPrefab;
    [SerializeField] private int prewarmCount = 10;

    private readonly Queue<PokeballGrabInteractable> availablePokeballs =
        new Queue<PokeballGrabInteractable>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (pokeballPrefab != null && prewarmCount > 0)
            Prewarm();
    }

    private void Prewarm()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            var ball = Instantiate(pokeballPrefab, transform);
            ball.gameObject.SetActive(false);
            availablePokeballs.Enqueue(ball);
        }
    }

    /// <summary>
    /// Get a pooled pokéball (non-belt usage).
    /// </summary>
    public PokeballGrabInteractable GetEmptyPokeball()
    {
        PokeballGrabInteractable ball = null;

        while (availablePokeballs.Count > 0 && ball == null)
        {
            ball = availablePokeballs.Dequeue();
        }

        if (ball == null && pokeballPrefab != null)
        {
            ball = Instantiate(pokeballPrefab, transform);
        }

        if (ball != null)
        {
            ball.gameObject.SetActive(true);
            var t = ball.transform;
            t.SetParent(null);
        }

        return ball;
    }

    /// <summary>
    /// Return a ball to the pool (non-belt usage).
    /// </summary>
    public void ReturnPokeballToPool(PokeballGrabInteractable ball)
    {
        if (ball == null)
            return;

        var t = ball.transform;
        t.SetParent(transform);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        ball.gameObject.SetActive(false);
        availablePokeballs.Enqueue(ball);
    }

    /// <summary>
    /// Create a new ball that can be used as a TEAM ball (if ever needed).
    /// Currently belt sockets instantiate their own prefabs, but this method
    /// is left for compatibility with older code.
    /// </summary>
    public PokeballGrabInteractable CreateTeamPokeball()
    {
        if (pokeballPrefab == null)
            return null;

        var ball = Instantiate(pokeballPrefab, transform);
        ball.gameObject.SetActive(true);
        return ball;
    }
}