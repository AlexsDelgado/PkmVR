using UnityEngine;

public class ScaleAndFadeFX : MonoBehaviour
{
    public float duration = 0.25f;
    public Vector3 startScale = Vector3.one * 0.2f;
    public Vector3 endScale = Vector3.one * 2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private float _t;
    private Renderer _renderer;
    private Material _mat;
    private Color _baseColor;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        // Instance material so only this FX changes.
        _mat = _renderer.material;
        _baseColor = _mat.GetColor("_BaseColor");
    }

    void OnEnable()
    {
        _t = 0f;
        transform.localScale = startScale;
        _mat.SetColor("_BaseColor", _baseColor);
    }

    void Update()
    {
        _t += Time.deltaTime;
        float normalized = Mathf.Clamp01(_t / duration);

        float s = scaleCurve.Evaluate(normalized);
        transform.localScale = Vector3.LerpUnclamped(startScale, endScale, s);

        float a = alphaCurve.Evaluate(normalized);
        var c = _baseColor;
        c.a = a;
        _mat.SetColor("_BaseColor", c);

        if (_t >= duration)
            gameObject.SetActive(false);
    }
}
