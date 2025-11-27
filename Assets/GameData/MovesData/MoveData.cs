using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveData", menuName = "GameData/Move")]
public class MoveData : ScriptableObject
{
    [Header("Info")]
    public int ID;
    public TypeData move_type;
    public DMGType dmg_type;

    [Header("Effect")]
    public bool DMG;
    public bool HEAL;
    public bool PROTECTION;

    [Header("Quantity")]
    public int dmg_ammount;
    [Range(0, 100)]
    public int heal_ammount;

    [Header("Stats")]
    public int coolDown;
    [Range(0, 100)]
    public int hitChance = 100;
}

public enum DMGType
{
    Physicañ,
    Special
}