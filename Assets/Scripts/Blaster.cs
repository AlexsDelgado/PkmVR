using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blaster : MonoBehaviour
{
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private GameObject bullet;


    public void Shoot()
    {
        GameObject bala = Instantiate(bullet, bulletSpawnPoint.position,bulletSpawnPoint.rotation);
        bala.GetComponent<Rigidbody>().AddForce(bulletSpawnPoint.forward*1000f);
    }
    
}
