using UnityEngine;
using System.Collections;

public class PokemonFXController : MonoBehaviour
{
    [SerializeField] private Renderer[] rend;
    [SerializeField] private string dissolveProp = "_Dissolve";
    [SerializeField] private string edgeColorProp = "_EdgeColor";
    [SerializeField] private float inTime = 0.35f;
    [SerializeField] private float outTime = 0.25f;
    [ColorUsage(true, true)] public Color edgeColor = new Color(0.6f, 0.9f, 1f, 1f);
    [Range(0f, 0.2f)] public float edgeWidth = 0.08f;

    MaterialPropertyBlock _mpb;

    void Awake() { _mpb = new MaterialPropertyBlock(); }

    public void PlayDissolveIn()
    {
        StopAllCoroutines();
        StartCoroutine(CoDissolve(1f, 0f, inTime));
    }

    public void PlayDissolveOut(System.Action onDone)
    {
        StopAllCoroutines();
        StartCoroutine(CoDissolve(0f, 1f, outTime, onDone));
    }

    IEnumerator CoDissolve(float from, float to, float t, System.Action onDone = null)
    {
        float e = 0f;
        while (e < t)
        {
            e += Time.deltaTime;
            float v = Mathf.Lerp(from, to, e / t);
            _mpb.SetFloat(dissolveProp, v);
            _mpb.SetColor(edgeColorProp, edgeColor);
            foreach (var r in rend)
            {
                if (!r) continue;
                _mpb.SetFloat("_EdgeWidth", edgeWidth);
                r.SetPropertyBlock(_mpb);
            }
            yield return null;
        }
        _mpb.SetFloat(dissolveProp, to);
        foreach (var r in rend) r?.SetPropertyBlock(_mpb);
        onDone?.Invoke();
    }
}