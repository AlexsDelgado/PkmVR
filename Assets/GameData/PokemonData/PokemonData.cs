using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PokemonData", menuName = "GameData/Pokemon")]
public class PokemonData : ScriptableObject
{
    [Header("Info")]
    public Sprite portrait;
    public GameObject model_pf;

    [Header("Types")]
    public TypeData type1;
    // public TypeData type2;

    [Header("Base Stats")]
    [SerializeField] int maxHP;
    [SerializeField] int physicalDMG;
    [SerializeField] int specialDMG;
    [SerializeField] int physicalDeff;
    [SerializeField] int specialDeff;
    [SerializeField] int velocity;

    [Header("Stats per Lvl")]
    [SerializeField] int extra_maxHP;
    [SerializeField] int extra_physicalDMG;
    [SerializeField] int extra_specialDMG;
    [SerializeField] int extra_physicalDeff;
    [SerializeField] int extra_specialDeff;
    [SerializeField] int extra_velocity;

    [Header("Moves")]
    public Move[] moveList;

    public int GetHealth(int lvl) => maxHP + extra_maxHP * lvl;
    public int GetPhysicalDMG(int lvl) => physicalDMG + extra_physicalDMG * lvl;
    public int GetSpecialDMG(int lvl) => specialDMG + extra_specialDMG * lvl;
    public int GetPhysicalDeff(int lvl) => physicalDeff + extra_physicalDeff * lvl;
    public int GetSpecialDeff(int lvl) => specialDeff + extra_specialDeff * lvl;
    public int GetVelocity(int lvl) => velocity + extra_velocity * lvl;
}

[Serializable]
public struct Move
{
    public MoveData my_move;
    [Range(0, 100)]
    public int min_lvl;
}