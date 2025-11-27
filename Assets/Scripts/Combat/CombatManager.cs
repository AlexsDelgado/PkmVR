using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public StoredPokemon my_avtive_pokemon;
    public StoredPokemon enemy_pokemon;





    public void SetCombatPokemons(StoredPokemon my_pkm, StoredPokemon enemy_pkm)
    {
        my_avtive_pokemon = my_pkm;
        enemy_pokemon = enemy_pkm;
    }
}
