using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    public Transform reference;
    public float range;
    public float angle;
    public LayerMask obsMask;
    public bool CheckRange(Transform target)
    {
        float distanceToTarget = Vector3.Distance(target.position, Origin);
        return distanceToTarget <= range;
    }
    public bool CheckAngle(Transform target)
    {
        //B-A
        Vector3 dirToTarget = target.position - Origin;
        float angleToTarget = Vector3.Angle(dirToTarget, Forward);
        return angleToTarget <= angle / 2;
    }
    public bool CheckView(Transform target)
    {
        Vector3 dirToTarget = target.position - Origin;
        return !Physics.Raycast(Origin, dirToTarget.normalized, dirToTarget.magnitude, obsMask);
    }
    Vector3 Origin
    {
        get
        {
            if (reference == null) return transform.position;
            return reference.position;
        }
    }
    Vector3 Forward
    {
        get
        {
            if (reference == null) return transform.forward;
            return reference.forward;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Color myColor = Color.blue;
        myColor.a = 0.5f;
        Gizmos.color = myColor;
        Gizmos.DrawWireSphere(Origin, range);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(Origin, Quaternion.Euler(0, angle / 2, 0) * Forward * range);
        Gizmos.DrawRay(Origin, Quaternion.Euler(0, -angle / 2, 0) * Forward * range);
    }
}
