using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [SerializeField] private int jumpForce = 10;
    private void OnCollisionEnter(Collision collision)
    {
        collision.rigidbody.AddForce(new Vector3(0, 1, 0) * jumpForce);
        Debug.Log("Salta");
    }
}
