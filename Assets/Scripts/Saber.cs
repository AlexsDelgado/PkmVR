using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Saber : MonoBehaviour
{
 #region  Variables to use:
    public InputActionProperty particleSystem;
    public GameObject saberGO;
    public Material defaultSaber;
    public Material activatedSaber;
    
        
    public ParticleSystem saber;
    public bool isEnabled;
    #endregion
    
    #region Methods in use:

    private void Start()
    {
        isEnabled = false;
        saber.Stop();
    }

    private void Update()
    {
        //float value =particleSystem.action.ReadValue<float>();
        //
        // if (value != 0)
        // {
        //     EnableParticles();
        // }
        // else
        // {
        //     saber.Stop();
        //     saberGO.GetComponent<MeshRenderer>().material = defaultSaber;
        //     isEnabled = false;
        // }
    }

    public void EnableParticles()
    {
        // if (!isEnabled)
        // {
            saber.Play();
            saberGO.GetComponent<MeshRenderer>().material = activatedSaber;
            isEnabled = true;
       
           
        // }
        // else
        // {
        //     saber.Stop();
        //     Debug.Log("Stop");
        // }
    }


    public void DisableBlade()
    {
        saber.Stop(); 
        saberGO.GetComponent<MeshRenderer>().material = defaultSaber;
        
    }

    #endregion

}
