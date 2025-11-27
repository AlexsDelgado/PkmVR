using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PC_Manager : MonoBehaviour
{
    public Image pkm_portrait;
    public TextMeshProUGUI lvl;
    public TextMeshProUGUI[] stats;
    int selected_pkm;

    public Transform move_spawn;
    public GameObject move_button_pf;
    public List<GameObject> move_list = new List<GameObject>();

    public Button addButton;

    public void ShowPKMDetails(StoredPokemon pkm, int idx)
    {
        selected_pkm = idx;
        pkm_portrait.sprite = pkm.pokemon.portrait;
        lvl.text = pkm.current_lvl.ToString();
        stats[0].text = pkm.pokemon.GetHealth(pkm.current_lvl).ToString();
        stats[1].text = pkm.pokemon.GetPhysicalDMG(pkm.current_lvl).ToString();
        stats[2].text = pkm.pokemon.GetSpecialDMG(pkm.current_lvl).ToString();
        stats[3].text = pkm.pokemon.GetPhysicalDeff(pkm.current_lvl).ToString();
        stats[4].text = pkm.pokemon.GetSpecialDeff(pkm.current_lvl).ToString();
        stats[5].text = pkm.pokemon.GetVelocity(pkm.current_lvl).ToString();

        Move move = new Move();
        PC_Move_Button pC_Move_Button = new PC_Move_Button();
        for (int i = 0; i < pkm.pokemon.moveList.Length; i++)
        {
            move = pkm.pokemon.moveList[i];
            if (i >= move_list.Count) move_list.Add(Instantiate(move_button_pf, move_spawn));   //controla pool obj
            move_list[i].SetActive(true);
            pC_Move_Button = move_list[i].GetComponent<PC_Move_Button>();
            pC_Move_Button.SetMove(move.my_move.name, i);

            pC_Move_Button.my_button.interactable = pkm.current_lvl >= move.min_lvl;
        }
        for (int i = pkm.pokemon.moveList.Length; i < move_list.Count; i++)
        {
            move_list[i].SetActive(false);
        }

        addButton.interactable = !pkm.equiped;
    }

    public void EquipPokemon(int idx)
    {
        PokemonsManager.Instantiate.
    }

    public void EquipMove()
    {

    }

    public void UnequipMove()
    {

    }


    public Transform showTeam_pos;
    public Transform hideTeam_pos;
    public float animationSpeed = 5;
    public IEnumerator ShowTeam()
    {
        Vector3 dir = new Vector3(-1, 0, 0);
        while (transform.position.x > showTeam_pos.position.x)
        {
            transform.Translate(dir * animationSpeed);
            yield return new WaitForEndOfFrame();
        }
        transform.position = showTeam_pos.position;
    }

    public IEnumerator HideTeam()
    {
        Vector3 dir = new Vector3(1, 0, 0);
        while (transform.position.x < hideTeam_pos.position.x)
        {
            transform.Translate(dir * animationSpeed);
            yield return new WaitForEndOfFrame();
        }
        transform.position = hideTeam_pos.position;
    }
}
