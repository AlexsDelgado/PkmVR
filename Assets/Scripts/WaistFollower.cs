using UnityEngine;
public class WaistFollower : MonoBehaviour
{
    public Transform head;              // assign Main Camera
    public Vector3 localOffset = new(0f, -0.45f, -0.1f);
    public float yawLerp = 20f;         // smoothing
    void LateUpdate()
    {
        if (!head) return;
        var yaw = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
        transform.position = head.position + yaw * localOffset;
        transform.rotation = Quaternion.Slerp(transform.rotation, yaw, Time.deltaTime * yawLerp);
    }
}