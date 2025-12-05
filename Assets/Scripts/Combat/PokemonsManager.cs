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
    private void Start()
    {
        PC_Manager.instance.ShowPokemonList();
    }

    [Header("Pokemons")]
    public StoredPokemon[] equiped_pokemons;
    public List<StoredPokemon> my_pokemons = new List<StoredPokemon>();

    [Header("Global parameters")]
    [SerializeField] int exp_to_lvl_up_base;
    [SerializeField] int extra_exp_per_lvl;
    [SerializeField] MoveData[] default_moves;



    public void AddNewPokemon(PokemonData my_pokemon, int lvl, MoveData[] moves = null)
    {
        if (moves == null)
        {
            my_pokemons.Add(new StoredPokemon(my_pokemon, lvl, default_moves, my_pokemons.Count));
        }
        else
        {
            my_pokemons.Add(new StoredPokemon(my_pokemon, lvl, moves, my_pokemons.Count));
            
        }
        for (int i = 0; i < 3; i++)
        {
            if (equiped_pokemons[i].captured_number == -1)
            {
                my_pokemons[my_pokemons.Count - 1].equiped = true;
                equiped_pokemons[i] = my_pokemons[my_pokemons.Count - 1];
                PC_Manager.instance.ShowPokemonList();
                return;
            }
        }
        PC_Manager.instance.ShowPokemonList();
    }
    public void EquipPokemon(int pkm_idx, int team_pos)
    {
        if (equiped_pokemons[team_pos].captured_number != -1) my_pokemons[equiped_pokemons[team_pos].captured_number].equiped = false;
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

    public void HealAllPokemon()    //Para el centro polemon
    {
        if (equiped_pokemons[0] != null) equiped_pokemons[0].HealPokemon();
        if (equiped_pokemons[1] != null) equiped_pokemons[1].HealPokemon();
        if (equiped_pokemons[2] != null) equiped_pokemons[2].HealPokemon();
    }

    public int ExpToLvlUp(int actual_lvl)
    {
        return exp_to_lvl_up_base + actual_lvl * extra_exp_per_lvl;
    }
        
}

[Serializable]
public class StoredPokemon
{
    public PokemonData pokemon;
    int current_health;
    public int current_lvl;
    int current_exp;
    public MoveData[] active_moves;
    public int equiped_moves_ammount = 0;
    public bool equiped;
    public int captured_number;

    public StoredPokemon(PokemonData my_pokemon, int lvl, MoveData[] moves, int captured_num, bool is_equiped = false)
    {
        this.pokemon = my_pokemon;
        current_lvl = lvl;
        current_health = my_pokemon.GetHealth(current_lvl);
        current_exp = 0;
        active_moves = moves;
        foreach (MoveData move in moves)
        {
            if (move != null) equiped_moves_ammount++;
        }
        captured_number = captured_num;
        equiped = is_equiped;
    }

    public bool IsMoveEquiped(MoveData move)
    {
        foreach (MoveData m in active_moves)
        {
            if (m == null) continue;
            if (m.ID == move.ID) return true;
        }
        return false;
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
                    equiped_moves_ammount++;
                    return;
                }
            }
        }
    public void UnequipMove(MoveData old_move)
        {
            for (int i = 0; i < 4; i++)
            {
                if (active_moves[i] == null) continue;
                if (active_moves[i].ID == old_move.ID)
                {
                    active_moves[i] = null;
                    equiped_moves_ammount--;
                    return;
                }
            }
        }
    public void HealPokemon(int value = -1)
    {
        current_health += value;
        if (value < 0 || current_health > pokemon.GetHealth(current_lvl))
        {
            current_health = pokemon.GetHealth(current_lvl);
        }
    }
    public void AddEXP(int value)
    {
        current_exp += value;
        int exp_needed = PokemonsManager.Instance.ExpToLvlUp(current_lvl);
        if (current_exp >= exp_needed)
        {
            current_lvl++;
            current_exp -= exp_needed;
        }
    }
}
    

