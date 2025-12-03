using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PC_Move_Button : MonoBehaviour
{
    public TextMeshProUGUI my_txt;
    public int idx;
    public Button my_button;
    public Image my_image;
    public Color equiped_move;
    Color unequiped_move = Color.white;
    public bool equiped;
    PC_Manager pc_manager;
    MoveData move;

    public void SetMove(MoveData move, int index, bool equiped)
    {
        this.move = move;
        my_txt.text = move.name;
        idx = index;
        this.equiped = equiped;
        pc_manager = PC_Manager.instance;
        my_image.color = equiped ? equiped_move : unequiped_move;
    }

    public void TryEquip()
    {
        if (equiped)
        {
            pc_manager.UnequipMove(move);
            my_image.color = unequiped_move;
            equiped = false;
        }
        else
        {
            if (pc_manager.Used_move_slots >= 4) return;
            pc_manager.EquipMove(move);
            my_image.color = equiped_move;
            equiped = true;
        }
    }

}
