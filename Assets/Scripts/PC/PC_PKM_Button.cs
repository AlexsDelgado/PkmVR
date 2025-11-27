using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PC_PKM_Button : MonoBehaviour
{
    public Image portrait;
    public TextMeshProUGUI txt;
    StoredPokemon my_pkm;
    public void SetPKMButton(StoredPokemon pkm)
    {
        my_pkm = pkm;
        portrait.sprite = pkm.pokemon.portrait;
        txt.text = pkm.pokemon.name;
    }

    public void SelectPKM()
    {
        PC_Manager.instance.ShowPKMDetails(my_pkm);
    }
}
