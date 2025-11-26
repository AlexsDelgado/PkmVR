using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PokemonData", menuName = "GameData/Pokemon")]
public class PokemonData : ScriptableObject
{
    [Header("Types")]
    [SerializeField] TypeData type1;
    [SerializeField] TypeData type2;

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
    [SerializeField] Move[] moveList;
}

[Serializable]
public struct Move
{
    public MoveData my_move;
    [Range(0, 100)]
    public int min_lvl;
}