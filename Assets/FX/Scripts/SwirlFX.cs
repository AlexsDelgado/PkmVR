using UnityEngine;

public class SwirlFX : MonoBehaviour
{
    public float angularSpeed = 360f;
    public float radius = 0.3f;
    public int quadCount = 3;
    public Material swirlMaterial;

    private Transform[] _quads;

    void Awake()
    {
        _quads = new Transform[quadCount];
        for (int i = 0; i < quadCount; i++)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "SwirlQuad_" + i;
            q.transform.SetParent(transform, false);
            q.GetComponent<Renderer>().material = swirlMaterial;
            q.transform.localScale = new Vector3(0.1f, 0.4f, 1f);
            _quads[i] = q.transform;
        }
    }

    void Update()
    {
        float angleStep = 360f / quadCount;
        for (int i = 0; i < quadCount; i++)
        {
            float angle = (Time.time * angularSpeed) + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            _quads[i].localPosition = pos;
            _quads[i].LookAt(transform.position);
        }
    }
}
