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

    public void SetMove(string txt, int index)
    {
        my_txt.text = txt;
        idx = index;
    }

}
