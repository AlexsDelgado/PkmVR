using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeItem : MonoBehaviour
{
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material hoverMaterial;
    
    public void Hover()
    {
        gameObject.GetComponent<MeshRenderer>().material = hoverMaterial;
    }

    public void HoverDisable()
    {
        gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
    }
    public void QuestMessage()
    {
        Debug.Log("Este objeto no sirve para la mision");
    }
}
