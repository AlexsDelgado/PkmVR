using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

public class PokemonSpawnZone : MonoBehaviour
{
    [Header("Pool")]
    [Tooltip("Pool keys (from PoolManager) for the possible Pokémon in this area.")]
    [SerializeField] private string[] pokemonPoolKeys;

    [Header("Spawn Settings")]
    [SerializeField] private int maxPokemon = 5;
    [Tooltip("Lifetime range (in minutes) for initially spawned Pokémon.")]
    [SerializeField] private float initialMinLifetimeMinutes = 3f;
    [SerializeField] private float initialMaxLifetimeMinutes = 8f;

    [Tooltip("Lifetime (in minutes) for replacement Pokémon spawned after a despawn.")]
    [SerializeField] private float replacementLifetimeMinutes = 5f;

    [Tooltip("Global cooldown (in minutes) between replacement spawns.")]
    [SerializeField] private float globalCooldownMinutes = 5f;

    [Header("Detection")]
    [Tooltip("Player HMD / camera. If left empty, will use Camera.main.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("How often to check which Pokémon have despawned (seconds).")]
    [SerializeField] private float monitorIntervalSeconds = 1f;

    private Collider zoneCollider;

    [Header("Spawn Zone")]
    [SerializeField] private BoxCollider spawnZone;

    private class ActivePokemon
    {
        public GameObject instance;
        public string poolKey;
        public Coroutine lifetimeRoutine;
    }

    private readonly List<ActivePokemon> activePokemons = new();

    private bool playerInside;
    private bool zoneActive;

    private Coroutine monitorRoutine;
    private Coroutine globalTimerRoutine;
    private bool globalTimerRunning;

    private Transform playerRoot;

    // --------------------------------------------------------------------

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (!zoneCollider)
        {
            Debug.LogError($"{nameof(PokemonSpawnZone)} requires a Collider.");
        }
        else if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning($"{nameof(PokemonSpawnZone)} collider should be set as Trigger.");
        }

        if (pokemonPoolKeys == null || pokemonPoolKeys.Length == 0)
        {
            Debug.LogWarning($"{nameof(PokemonSpawnZone)} has no pokemonPoolKeys assigned.", this);
        }

        // Auto-assign playerTarget from main camera if not set
        if (!playerTarget)
        {
            Camera cam = Camera.main;
            if (cam != null)
                playerTarget = cam.transform;
        }

        if (playerTarget != null)
            playerRoot = playerTarget.root;
        else
            Debug.LogWarning($"{nameof(PokemonSpawnZone)} could not find player camera. No spawning will occur until playerTarget is set.", this);
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (playerRoot == null)
            return false;

        Transform t = col.transform;
        while (t != null)
        {
            if (t == playerRoot)
                return true;
            t = t.parent;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        playerInside = true;

        if (!zoneActive)
        {
            zoneActive = true;
            StartZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        playerInside = false;
        StopZone();
    }

    // Zone control

    private void StartZone()
    {
        // Spawn initial batch
        for (int i = activePokemons.Count; i < maxPokemon; i++)
        {
            float lifetimeMinutes = Random.Range(initialMinLifetimeMinutes, initialMaxLifetimeMinutes);
            SpawnPokemon(lifetimeMinutes * 60f);
        }

        if (monitorRoutine == null)
            monitorRoutine = StartCoroutine(MonitorActiveRoutine());
    }

    private void StopZone()
    {
        zoneActive = false;

        if (monitorRoutine != null)
        {
            StopCoroutine(monitorRoutine);
            monitorRoutine = null;
        }

        if (globalTimerRoutine != null)
        {
            StopCoroutine(globalTimerRoutine);
            globalTimerRoutine = null;
        }

        globalTimerRunning = false;

        // Despawn all active Pokémon and stop their lifetimes
        for (int i = 0; i < activePokemons.Count; i++)
        {
            var slot = activePokemons[i];

            if (slot.lifetimeRoutine != null)
            {
                StopCoroutine(slot.lifetimeRoutine);
                slot.lifetimeRoutine = null;
            }

            if (slot.instance && slot.instance.activeSelf)
            {
                var controller = slot.instance.GetComponent<PokemonController>();
                if (controller != null)
                {
                    controller.Despawn(); // Uses your FX + pool system :contentReference[oaicite:1]{index=1}
                }
                else
                {
                    PoolManager.I.Despawn(slot.poolKey, slot.instance); // :contentReference[oaicite:2]{index=2}
                }
            }
        }

        activePokemons.Clear();
    }

    // Spawn & lifetime

    private void SpawnPokemon(float lifetimeSeconds)
    {
        if (pokemonPoolKeys == null || pokemonPoolKeys.Length == 0)
            return;

        if (activePokemons.Count >= maxPokemon)
            return;

        string key = pokemonPoolKeys[Random.Range(0, pokemonPoolKeys.Length)];
        Vector3 spawnPos = GetRandomPointInZone();

        GameObject instance = PoolManager.I.Spawn(key, spawnPos, Quaternion.identity);

        // Initialize controller & behaviour state
        var controller = instance.GetComponent<PokemonController>();
        if (controller != null)
            controller.Init(); // plays dissolve-in & FX swirl

        var behaviorMgr = instance.GetComponent<PokemonBehaviorManager>();
        if (behaviorMgr != null)
            behaviorMgr.EnterRoaming(); // make sure they start roaming

        var slot = new ActivePokemon
        {
            instance = instance,
            poolKey = key,
            lifetimeRoutine = StartCoroutine(PokemonLifetimeRoutine(instance, key, lifetimeSeconds))
        };

        activePokemons.Add(slot);
    }

    private IEnumerator PokemonLifetimeRoutine(GameObject instance, string poolKey, float seconds)
    {
        float remaining = seconds;

        while (remaining > 0f && zoneActive && instance && instance.activeSelf)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        // If zone is no longer active, or Pokémon already despawned/captured, just exit
        if (!zoneActive || !instance || !instance.activeSelf)
            yield break;

        // Lifetime expired: despawn this Pokémon
        var controller = instance.GetComponent<PokemonController>();
        if (controller != null)
        {
            controller.Despawn();
        }
        else
        {
            PoolManager.I.Despawn(poolKey, instance);
        }

        // We do NOT manipulate the list here; MonitorActiveRoutine will clean it up
    }

    // Monitor which Pokémon are still active (capture, lifetime, etc.)

    private IEnumerator MonitorActiveRoutine()
    {
        var wait = new WaitForSeconds(monitorIntervalSeconds);

        while (zoneActive)
        {
            for (int i = activePokemons.Count - 1; i >= 0; i--)
            {
                var slot = activePokemons[i];

                // If instance is gone or returned to pool
                if (slot.instance == null || !slot.instance.activeSelf)
                {
                    if (slot.lifetimeRoutine != null)
                    {
                        StopCoroutine(slot.lifetimeRoutine);
                        slot.lifetimeRoutine = null;
                    }

                    activePokemons.RemoveAt(i);
                    OnPokemonSlotFreed();
                }
            }

            yield return wait;
        }
    }

    // Global cooldown logic

    private void OnPokemonSlotFreed()
    {
        if (!zoneActive || !playerInside)
            return;

        // First despawn since last cooldown: immediate replacement + start global timer
        if (!globalTimerRunning)
        {
            if (activePokemons.Count < maxPokemon)
            {
                SpawnPokemon(replacementLifetimeMinutes * 60f);
            }
            StartGlobalTimer();
        }
        // If cooldown is already running, do nothing now.
        // A new one will spawn when the global timer reaches 0.
    }

    private void StartGlobalTimer()
    {
        if (globalTimerRunning)
            return;

        globalTimerRunning = true;
        globalTimerRoutine = StartCoroutine(GlobalTimerRoutine());
    }

    private IEnumerator GlobalTimerRoutine()
    {
        float remaining = globalCooldownMinutes * 60f;

        while (remaining > 0f && zoneActive && playerInside)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        globalTimerRunning = false;
        globalTimerRoutine = null;

        if (!zoneActive || !playerInside)
            yield break;

        // Global timer ended: if there are empty slots, spawn ONE Pokémon and restart timer
        if (activePokemons.Count < maxPokemon)
        {
            SpawnPokemon(replacementLifetimeMinutes * 60f);
            StartGlobalTimer();
        }
    }

    // Spawn position helper (uses this trigger collider as the area)

    private Vector3 GetRandomPointInZone()
    {
        // If we have a dedicated spawn zone, use that
        if (spawnZone != null)
        {
            Bounds b = spawnZone.bounds;
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            return new Vector3(x, b.center.y, z);
        }

        // Fallback: use the full trigger collider
        if (!zoneCollider)
            return transform.position;

        if (zoneCollider is BoxCollider box)
        {
            Bounds b = box.bounds;
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            return new Vector3(x, b.center.y, z);
        }

        Bounds bounds = zoneCollider.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }
}
