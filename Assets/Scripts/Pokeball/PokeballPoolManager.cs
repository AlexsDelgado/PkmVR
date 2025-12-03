using System.Collections.Generic;
using UnityEngine;

public class PokeballPoolManager : MonoBehaviour
{
    public static PokeballPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private PokeballGrabInteractable pokeballPrefab;
    [SerializeField] private int prewarmCount = 10;

    private readonly Queue<PokeballGrabInteractable> availablePokeballs = new Queue<PokeballGrabInteractable>();
    private readonly HashSet<PokeballGrabInteractable> activePokeballs = new HashSet<PokeballGrabInteractable>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (pokeballPrefab == null)
        {
            Debug.LogError("PokeballPoolManager: pokeballPrefab is not assigned.", this);
            return;
        }

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

    public void OnPokeballGrabbed()
    {
        // intentionally empty
    }


    // Get an Empty pokeball instance from the pool.

    public PokeballGrabInteractable GetEmptyPokeball()
    {
        if (pokeballPrefab == null)
            return null;

        PokeballGrabInteractable ball;

        if (availablePokeballs.Count > 0)
        {
            ball = availablePokeballs.Dequeue();
        }
        else
        {
            ball = Instantiate(pokeballPrefab, transform);
        }

        ball.SetMode(PokeballGrabInteractable.BallMode.Empty);
        ball.SetAssignedSpecies(null);
        ball.gameObject.SetActive(true);

        activePokeballs.Add(ball);
        return ball;
    }

    // Return an Empty pokeball to the pool.
    // Team balls (Full/Team) should never be returned here – they re-dock to the belt.

    public void ReturnPokeballToPool(PokeballGrabInteractable pokeball)
    {
        if (pokeball == null)
            return;

        // Only empties are meant to be pooled
        if (pokeball.GetMode() != PokeballGrabInteractable.BallMode.Empty)
        {
            Debug.LogWarning("PokeballPoolManager: tried to return a non-empty ball to the pool. This ball should be managed by its belt socket instead.", pokeball);
            return;
        }

        if (!activePokeballs.Contains(pokeball))
        {
            // Already returned or not tracked, but we can still safely disable it
            pokeball.gameObject.SetActive(false);
            return;
        }

        activePokeballs.Remove(pokeball);

        pokeball.gameObject.SetActive(false);
        pokeball.transform.SetParent(transform, false);
        pokeball.SetAssignedSpecies(null);

        availablePokeballs.Enqueue(pokeball);
    }


    // Utility for team sockets: create a new team ball instance that is not managed by the pool.
    // It starts disabled; caller should configure species/mode and position.

    public PokeballGrabInteractable CreateTeamPokeball()
    {
        if (pokeballPrefab == null)
            return null;

        var ball = Instantiate(pokeballPrefab);
        ball.gameObject.SetActive(false);
        return ball;
    }

    public int GetAvailableCount() => availablePokeballs.Count;
    public int GetActiveCount() => activePokeballs.Count;
}
