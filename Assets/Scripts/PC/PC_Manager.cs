using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PC_Manager : MonoBehaviour
{
    public static PC_Manager instance;
    private void Awake()
    {
        instance = this;
    }


    public Image pkm_portrait;
    public TextMeshProUGUI lvl;
    public TextMeshProUGUI[] stats;
    StoredPokemon selected_pkm;

    public Transform move_spawn;
    public GameObject move_button_pf;
    public List<GameObject> move_list = new List<GameObject>();

    public Button addButton;

    public PokemonsManager pkm_manager;
    public List<GameObject> pc_pokemons = new List<GameObject>();
    public GameObject pc_pkm_pf;
    public Transform pc_pkm_spwn;

    public Image[] teamPortraits;
    public TextMeshProUGUI[] teamNames;

    public int Used_move_slots => selected_pkm.equiped_moves_ammount;
    public void ShowPokemonList()
    {
        List<StoredPokemon> pkmList = pkm_manager.my_pokemons;

        for (int i = 0; i < pkmList.Count; i++)
        {
            if (i >= pc_pokemons.Count) pc_pokemons.Add(Instantiate(pc_pkm_pf, pc_pkm_spwn));
            pc_pokemons[i].SetActive(true);
            pc_pokemons[i].GetComponent<PC_PKM_Button>().SetPKMButton(pkmList[i]);

        }
        for (int i = pkmList.Count; i < pc_pokemons.Count; i++)
        {
            pc_pokemons[i].SetActive(false);
        }
        UpdateTeamPortraits();
    }

    public void ShowPKMDetails(StoredPokemon pkm)
    {
        selected_pkm = pkm;
        pkm_portrait.sprite = pkm.pokemon.portrait;
        lvl.text      = "Lv. "  + pkm.current_lvl.ToString();
        stats[0].text = "Hp: "  + pkm.pokemon.GetHealth(pkm.current_lvl).ToString();
        stats[1].text = "Atk: " + pkm.pokemon.GetPhysicalDMG(pkm.current_lvl).ToString();
        stats[2].text = "SpA: " + pkm.pokemon.GetSpecialDMG(pkm.current_lvl).ToString();
        stats[3].text = "Def: " + pkm.pokemon.GetPhysicalDeff(pkm.current_lvl).ToString();
        stats[4].text = "SpD: " + pkm.pokemon.GetSpecialDeff(pkm.current_lvl).ToString();
        stats[5].text = "Spe: " + pkm.pokemon.GetVelocity(pkm.current_lvl).ToString();

        Move move = new Move();
        PC_Move_Button pC_Move_Button = new PC_Move_Button();
        for (int i = 0; i < pkm.pokemon.moveList.Length; i++)
        {
            move = pkm.pokemon.moveList[i];
            if (i >= move_list.Count) move_list.Add(Instantiate(move_button_pf, move_spawn));   //controla pool obj
            move_list[i].SetActive(true);
            pC_Move_Button = move_list[i].GetComponent<PC_Move_Button>();
            pC_Move_Button.SetMove(move.my_move, i, pkm.IsMoveEquiped(move.my_move));

            pC_Move_Button.my_button.interactable = pkm.current_lvl >= move.min_lvl;
        }
        for (int i = pkm.pokemon.moveList.Length; i < move_list.Count; i++)
        {
            move_list[i].SetActive(false);
        }

        addButton.interactable = !pkm.equiped;
    }

    public void EquipPokemon(int team_pos)
    {
        pkm_manager.EquipPokemon(selected_pkm.captured_number, team_pos);
        UpdateTeamPortraits();
    }

    public void UpdateTeamPortraits()
    {
        for (int i = 0; i < 3; i++)
        {
            if (pkm_manager.equiped_pokemons[i].pokemon == null) continue;
            teamPortraits[i].sprite = pkm_manager.equiped_pokemons[i].pokemon.portrait;
            teamNames[i].text = pkm_manager.equiped_pokemons[i].pokemon.name;
        }
    }

    public void EquipMove(MoveData new_move)
    {
        pkm_manager.EquipMove(selected_pkm.captured_number, new_move);
    }

    public void UnequipMove(MoveData old_move)
    {
        pkm_manager.UnequipMove(selected_pkm.captured_number, old_move);
    }

    [Header("PC Team")]
    public RectTransform teamUI;
    public RectTransform showTeam_pos;
    public RectTransform hideTeam_pos;
    public float animationSpeed = 5;

    public void ShowTeamCoroutine()
    {
        StartCoroutine(ShowTeam());
    }
    public void HideTeamCoroutine()
    {
        StartCoroutine(HideTeam());
    }

    public IEnumerator ShowTeam()
    {
        Vector2 dir = new Vector2(-1, 0);
        while (teamUI.position.x > showTeam_pos.position.x)
        {
            // teamUI.anchoredPosition += dir * animationSpeed * Time.deltaTime;
            teamUI.Translate(dir * animationSpeed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
        teamUI.position = showTeam_pos.position;
    }

    public IEnumerator HideTeam()
    {
        Vector2 dir = new Vector2(1, 0);
        while (teamUI.position.x < hideTeam_pos.position.x)
        {
            // teamUI.anchoredPosition += dir * animationSpeed * Time.deltaTime;
            teamUI.Translate(dir * animationSpeed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
        teamUI.position = hideTeam_pos.position;
    }
}
