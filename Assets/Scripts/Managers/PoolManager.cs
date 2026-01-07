using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { private set; get; }

    private readonly Dictionary<int, int> origin = new Dictionary<int, int>();

    private readonly Dictionary<int, Stack<GameObject>> pool = new Dictionary<int, Stack<GameObject>>();
    private readonly List<GameObject> pending = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void LateUpdate()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            GameObject obj = pending[i];

            if (obj == null)
            { pending.RemoveAt(i); continue; }

            obj.transform.SetParent(transform, false);
            pending.RemoveAt(i);
        }
    }

    public GameObject Get(GameObject _prefab, Vector3 _pos, Transform _parent)
    {
        int key = _prefab.GetInstanceID();

        if (!pool.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            pool.Add(key, stack);
        }

        GameObject obj = null;
        while (stack.Count > 0 && obj == null)
            obj = stack.Pop();

        if (obj == null)
        {
            obj = Instantiate(_prefab);
            origin[obj.GetInstanceID()] = key;
        }

        Transform t = obj.transform;
        t.SetParent(_parent, false);
        t.SetPositionAndRotation(_pos, Quaternion.identity);

        obj.SetActive(true);
        return obj;
    }

    public void Release(GameObject _obj)
    {
        int id = _obj.GetInstanceID();

        if (!origin.TryGetValue(id, out int key))
        {
            Destroy(_obj);
            return;
        }

        if (!pool.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            pool.Add(key, stack);
        }

        _obj.SetActive(false);
        stack.Push(_obj);
        pending.Add(_obj);
    }
}
