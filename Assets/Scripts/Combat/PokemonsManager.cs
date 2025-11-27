using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonsManager : MonoBehaviour
{
    public static PokemonsManager Instance;
    private void Awake()
    {
        Instance = this;
    }
        
    [SerializeField] StoredPokemon[] equiped_pokemons;
    public List<StoredPokemon> my_pokemons = new List<StoredPokemon>();

    public void AddNewPokemon(PokemonData my_pokemon, int lvl, MoveData[] moves)
    {
        my_pokemons.Add(new StoredPokemon(my_pokemon, lvl, moves, my_pokemons.Count));
    }
    public void EquipPokemon(int pkm_idx, int team_pos)
    {
        my_pokemons[equiped_pokemons[team_pos].captured_number].equiped = false;
        equiped_pokemons[team_pos] = my_pokemons[pkm_idx];
        my_pokemons[pkm_idx].equiped = true;
    }

    public void EquipMove(int pkm_idx, MoveData new_move)
    {
        my_pokemons[pkm_idx].EquipMove(new_move);
    }

    public void UnequipMove(int pkm_idx, MoveData old_move)
    {
        my_pokemons[pkm_idx].UnequipMove(old_move);
    }

}

[Serializable]
public class StoredPokemon
{
    public PokemonData pokemon;
    int current_health;
    public int current_lvl;
    int current_exp;
    MoveData[] active_moves;
    public bool equiped;
    public int captured_number;

    public StoredPokemon(PokemonData my_pokemon, int lvl, MoveData[] moves, int captured_num, bool is_equiped = false)
    {
        this.pokemon = my_pokemon;
        current_lvl = lvl;
        current_health = my_pokemon.GetHealth(current_lvl);
        current_exp = 0;
        active_moves = moves;
        captured_number = captured_num;
        equiped = is_equiped;
    }

    public void UpdateMoves(MoveData[] moves)
    {
        active_moves = moves;
    }
    public void EquipMove(MoveData new_move)
    {
        for (int i = 0; i < 4; i++)
        {
            if (active_moves[i] == null)
            {
                active_moves[i] = new_move;
                return;
            }
        }
    }
    public void UnequipMove(MoveData old_move)
    {
        for (int i = 0; i < 4; i++)
        {
            if (active_moves[i].ID == old_move.ID)
            {
                active_moves[i] = null;
                return;
            }
        }
    }
}
