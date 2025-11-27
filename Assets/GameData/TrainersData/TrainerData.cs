using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainerData", menuName = "GameData/Trainer")]
public class TrainerData : ScriptableObject
{
    [Header("Info")]
    public string TrainerName;
    public GameObject model_pb;
    public int recomended_lvl;

    [Header("Pokemons")]
    public StoredPokemon[] pokemons;

    [Header("Rewards")]
    public int exp;
    public int money;

}
