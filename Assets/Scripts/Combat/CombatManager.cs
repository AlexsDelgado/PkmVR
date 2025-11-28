using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;
    private void Awake()
    {
        instance = this;
    }


    public CombatPokemon my_avtive_pokemon;
    public CombatPokemon enemy_pokemon;
    int actual_enemy_pkm = 0;

    List<TrainerData> trainers;

    [Header("SpawnPoints")]
    public Transform my_spawn;
    public Transform enemy_spawn;
    public Transform enemy_pkm_spawn;

    public Action combatStart;
    public Action changePKM;
    public void SetCombat(TrainerData trainer)
    {
        Instantiate(trainer.model_pb, enemy_spawn);
        Instantiate(trainer.pokemons[0].pokemon.model_pf, enemy_pkm_spawn);

        //TP player to my_spawn


        enemy_pokemon = new CombatPokemon(trainer.pokemons[0]);

    }

    public void StartCombat(StoredPokemon my_pkm)
    {
        my_avtive_pokemon = new CombatPokemon(my_pkm);

        // actual_turn = my_avtive_pokemon.velocity >= enemy_pokemon.velocity ? Turn.Ally : Turn.Enemy;
        StartCoroutine(StartCombatAnimation());

        
    }

    public void ChangeMyPkm(StoredPokemon my_pkm)
    {
        my_avtive_pokemon = new CombatPokemon(my_pkm);
        changePKM.Invoke();
    }

    public void CommandMove(int idx)
    {
        if (!my_avtive_pokemon.available_move[idx])
        {
            //Show that it is not available
            return;
        }
        if (my_avtive_pokemon.moves[idx].DMG) CommandAttack(idx);
        if (my_avtive_pokemon.moves[idx].HEAL) CommandHeal(idx);
        // if (my_avtive_pokemon.moves[idx].PROTECTION) CommandHeal(idx);

        StartCoroutine(StartMoveCooldown(my_avtive_pokemon, idx));
    }

    void CommandAttack(int idx)
    {
        int dmg = my_avtive_pokemon.DoDamage(my_avtive_pokemon.moves[idx].dmg_ammount, my_avtive_pokemon.moves[idx].dmg_type);
        if (enemy_pokemon.GetDamage(dmg, my_avtive_pokemon.moves[idx].dmg_type, my_avtive_pokemon.moves[idx].move_type))
        {
            // Muerte del pkm enemigo
            NextEnemyPKM();

        }

        //UpdateUI
    }

    void CommandHeal(int idx)
    {
        my_avtive_pokemon.GetHealth(my_avtive_pokemon.moves[idx].heal_ammount);

        //UpdateUI
    }

    public void NextEnemyPKM()
    {
        actual_enemy_pkm++;
        if (actual_enemy_pkm >= 3) CombatEnd();
        //Siguiente pkm
    }
    public void CombatEnd()
    {
        //Update UI
        //TP player
        //EXP
        //MONEY
    }

    public IEnumerator StartCombatAnimation()
    {
        yield return null;
        combatStart.Invoke();
    }

    public IEnumerator StartMoveCooldown(CombatPokemon pkm, int idx)
    {
        pkm.available_move[idx] = false;
        // Tal vez agregar algun tipo de animacion, qsy
        yield return new WaitForSeconds(pkm.moves[idx].coolDown);
        pkm.available_move[idx] = true;
    }
}

public class CombatPokemon
{
    PokemonData pokemon;
    public int maxHP;
    public int physicalDMG;
    public int specialDMG;
    public int physicalDeff;
    public int specialDeff;
    public int velocity;
    public int current_hp;

    public MoveData[] moves;
    public bool[] available_move;
    public CombatPokemon(StoredPokemon pkm)
    {
        pokemon = pkm.pokemon;
        moves = pkm.active_moves;
        available_move = new bool[moves.Length];
        Array.Fill(available_move, true);
        maxHP = pkm.pokemon.GetHealth(pkm.current_lvl);
        current_hp = maxHP;
        physicalDMG = pkm.pokemon.GetPhysicalDMG(pkm.current_lvl);
        specialDMG = pkm.pokemon.GetSpecialDMG(pkm.current_lvl);
        physicalDeff = pkm.pokemon.GetPhysicalDeff(pkm.current_lvl);
        specialDeff = pkm.pokemon.GetSpecialDeff(pkm.current_lvl);
        velocity = pkm.pokemon.GetVelocity(pkm.current_lvl);
    }

    public bool GetDamage(int value, DMGType dmg_type, TypeData move_type)  //true es que muere
    {
        int deff_stat = dmg_type == DMGType.Special ? specialDeff : physicalDeff;
        float dmg = value * ((int)move_type.interactions[pokemon.type1.ID].multiplier / 2) * (100 / (100 + deff_stat)); //daño * type_multi * deff
        current_hp -= Mathf.FloorToInt(dmg);
        if (CheckIfDead())
        {
            current_hp = 0;
            return true;
        }
        return false;
    }

    bool CheckIfDead()
    {
        return current_hp < 0;
    }

    public int DoDamage(int base_value, DMGType dmg_type)
    {
        int dmg_stat = dmg_type == DMGType.Special ? specialDMG : physicalDMG;
        float dmg = (base_value + dmg_stat * 0.5f) * UnityEngine.Random.Range(0.85f, 1);
        return Mathf.FloorToInt(dmg);
    }

    public void GetHealth(int value)
    {
        current_hp += value * maxHP / 100;
        if (current_hp > maxHP) current_hp = maxHP;
    }
}

