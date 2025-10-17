using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string key;
        public GameObject prefab;
        public int prewarm = 4;
    }

    public static PoolManager I { get; private set; }
    [SerializeField] private List<Pool> pools = new();
    private readonly Dictionary<string, Queue<GameObject>> dict = new();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        foreach (var p in pools)
        {
            var q = new Queue<GameObject>(p.prewarm);
            for (int i = 0; i < p.prewarm; i++)
            {
                var go = Instantiate(p.prefab);
                go.SetActive(false);
                q.Enqueue(go);
            }
            dict[p.key] = q;
        }
    }

    public GameObject Spawn(string key, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        var q = dict[key];
        GameObject go = q.Count > 0 ? q.Dequeue() : Instantiate(GetPrefab(key));
        go.transform.SetPositionAndRotation(pos, rot);
        if (parent) go.transform.SetParent(parent, true);
        go.SetActive(true);
        return go;
    }

    public void Despawn(string key, GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(null, true);
        dict[key].Enqueue(go);
    }

    GameObject GetPrefab(string key)
    {
        foreach (var p in pools) if (p.key == key) return p.prefab;
        Debug.LogError($"Pool key not found: {key}");
        return null;
    }
}
