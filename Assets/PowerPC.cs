using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

public class PowerPC : MonoBehaviour
{
    public Animator anim;

    [SerializeField] private Transform playerTarget;
    private Transform playerRoot;

    private void Awake()
    {
        // Auto-assign playerTarget from main camera if not set
        if (!playerTarget)
        {
            Camera cam = Camera.main;
            if (cam != null)
                playerTarget = cam.transform;
        }

        if (playerTarget != null)
            playerRoot = playerTarget.root;
        else
            Debug.LogWarning($"{nameof(PokemonSpawnZone)} could not find player camera. No spawning will occur until playerTarget is set.", this);
    }
    public void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other)) return;
        anim.SetBool("IsOn", true);
        SoundManager.Instance.PlaySFX(SoundName.PC_ON);

    }

    public void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;
        anim.SetBool("IsOn", false);
        SoundManager.Instance.PlaySFX(SoundName.PC_OFF);
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (playerRoot == null)
            return false;

        // NEW: ignore pokéballs (and anything under them) even though they are
        // parented under the XR Origin / player root.
        if (col.GetComponentInParent<PokeballGrabInteractable>() != null)
            return false;

        Transform t = col.transform;
        while (t != null)
        {
            if (t == playerRoot)
                return true;
            t = t.parent;
        }
        return false;
    }
}
