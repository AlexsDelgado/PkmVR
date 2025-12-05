using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public SoundName hoverSound;
    public SoundName clickSound;


    SoundManager s_manager;


    void Start()
    {
        s_manager = SoundManager.Instance;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
       s_manager.PlaySFX(hoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        s_manager.PlaySFX(clickSound);
    }
}
