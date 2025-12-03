using UnityEngine;

public enum PokemonState
{
    Roaming,
    Combat,
    Caught
}

public class PokemonBehaviorManager : MonoBehaviour
{
    [Header("Initial State")]
    [SerializeField] private PokemonState initialState = PokemonState.Roaming;

    [Header("Behaviours per State")]
    [Tooltip("Scripts that should run only while roaming in the overworld.")]
    [SerializeField] private MonoBehaviour[] roamingBehaviours;

    [Tooltip("Scripts that should run only during combat.")]
    [SerializeField] private MonoBehaviour[] combatBehaviours;

    [Tooltip("Scripts that should run only after being caught (party / follower logic).")]
    [SerializeField] private MonoBehaviour[] caughtBehaviours;

    private PokemonState currentState;

    public PokemonState CurrentState => currentState;

    private void Awake()
    {
        // Apply initial state once
        SetState(initialState, force: true);
    }

    // Public API
    public void SetState(PokemonState newState)
    {
        SetState(newState, force: false);
    }
    
    // Just in case
    public void EnterRoaming() => SetState(PokemonState.Roaming);
    public void EnterCombat() => SetState(PokemonState.Combat);
    public void EnterCaught() => SetState(PokemonState.Caught);

    // --------------------------------------------------------------------

    private void SetState(PokemonState newState, bool force)
    {
        if (!force && newState == currentState)
            return;

        currentState = newState;

        // Enable / disable groups. Manager itself is never in these arrays.
        bool roamingOn = newState == PokemonState.Roaming;
        bool combatOn = newState == PokemonState.Combat;
        bool caughtOn = newState == PokemonState.Caught;

        SetGroupEnabled(roamingBehaviours, roamingOn);
        SetGroupEnabled(combatBehaviours, combatOn);
        SetGroupEnabled(caughtBehaviours, caughtOn);
    }

    private static void SetGroupEnabled(MonoBehaviour[] group, bool enabled)
    {
        if (group == null) return;

        for (int i = 0; i < group.Length; i++)
        {
            var b = group[i];
            if (b != null)
                b.enabled = enabled;
        }
    }
}
