using UnityEngine;

public class BillboardFX : MonoBehaviour
{
    public Camera targetCamera;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (!targetCamera) return;

        var camForward = targetCamera.transform.forward;
        var camUp = targetCamera.transform.up;
        transform.rotation = Quaternion.LookRotation(camForward, camUp);
    }
}
