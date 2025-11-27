using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveData", menuName = "GameData/Move")]
public class MoveData : ScriptableObject
{
    [Header("Info")]
    public int ID;
    [SerializeField] TypeData type;
    [SerializeField] bool specialType;

    [Header("Effect")]
    [SerializeField] bool DMG;
    [SerializeField] bool HEAL;
    [SerializeField] bool PROTECTION;

    [Header("Quantity")]
    [SerializeField] int dmg_ammount;
    [Range(0, 100)]
    [SerializeField] int heal_ammount;

    [Header("Stats")]
    [SerializeField] int coolDown;
    [Range(0, 100)]
    [SerializeField] int hitChance;
}
