using UnityEngine;

public class BallFXController : MonoBehaviour
{
    [Header("Pool Keys")]
    public string kFlash = "fx_flash";
    public string kRing = "fx_ring";
    public string kDust = "fx_dust";
    public string kGlow = "fx_glow";
    public string kBallBlink = "fx_ballflash";
    public string kBallCapture = "fx_captureburst";

    [Header("Refs")]
    public TrailRenderer trail; // assign

    public void OnThrowStart() { if (trail) trail.emitting = true; }
    public void OnThrowEnd() { if (trail) trail.emitting = false; }

    public void PlayImpactSet(Vector3 pos, Vector3 normal)
    {
        var rot = Quaternion.LookRotation(normal);
        PoolManager.I.Spawn(kFlash, pos + normal * 0.02f, rot);
        PoolManager.I.Spawn(kRing, pos + normal * 0.01f, Quaternion.identity);
        PoolManager.I.Spawn(kDust, pos + normal * 0.01f, Quaternion.identity);
        PoolManager.I.Spawn(kGlow, pos + normal * 0.01f, Quaternion.identity);
        PoolManager.I.Spawn(kBallCapture, pos + normal * 0.01f, Quaternion.identity);
    }

    public void PlayRecallBlink(Transform attach)
    {
        if (!attach) return;
        PoolManager.I.Spawn(kBallBlink, attach.position, attach.rotation);
    }
}
