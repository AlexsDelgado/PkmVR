using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TypeData", menuName = "GameData/Type")]
public class TypeData : ScriptableObject
{
    public int ID;
    public List<TypeInteraction> interactions = new List<TypeInteraction>();
}

[Serializable]
public struct TypeInteraction
{
    public TypeData targetType;
    public Multiplier multiplier;
}

public enum Multiplier      //We have to divide the value by 2
{   
    SuperEffective = 4,     // -> 4/2 = 2
    Normal = 2,             // -> 2/2 = 1
    NonEffective = 1,       // -> 1/2 = 0.5
    Inmune = 0              // -> 0/2 = 0
}