using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private struct Policy
    {
        public readonly int prewarm;
        public readonly int keep;
        public readonly int make;
        public readonly bool skip;

        public Policy(int _prewarm, int _keep, int _make, bool _skip)
        {
            prewarm = _prewarm;
            keep = _keep;
            make = _make;
            skip = _skip;
        }
    }

    private readonly Dictionary<int, Stack<GameObject>> pool = new Dictionary<int, Stack<GameObject>>();
    private readonly Dictionary<int, int> origin = new Dictionary<int, int>();
    private readonly Dictionary<int, IPoolable[]> hook = new Dictionary<int, IPoolable[]>();
    private readonly Dictionary<int, Policy> policy = new Dictionary<int, Policy>();
    private readonly Dictionary<int, int> made = new Dictionary<int, int>();

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

            if (obj.activeSelf)
            { pending.RemoveAt(i); continue; }

            obj.transform.SetParent(transform, false);
            pending.RemoveAt(i);
        }
    }

    #region 정책
    public void Register(GameObject _prefab, int _prewarm, int _keep, int _make, bool _skip)
    {
        int key = _prefab.GetInstanceID();
        policy[key] = new Policy(_prewarm, _keep, _make, _skip);
    }

    public void Prewarm(GameObject _prefab)
    {
        int key = _prefab.GetInstanceID();
        if (!policy.TryGetValue(key, out var p)) return;

        Prewarm(_prefab, p.prewarm);
    }

    public void Prewarm(GameObject _prefab, int _count)
    {
        int key = _prefab.GetInstanceID();

        if (!pool.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            pool.Add(key, stack);
        }

        int need = _count - stack.Count;
        if (need <= 0) return;

        if (policy.TryGetValue(key, out var p) && p.make > 0)
        {
            made.TryGetValue(key, out int cur);
            int room = p.make - cur;

            if (room <= 0) return;
            if (need > room) need = room;
        }

        for (int i = 0; i < need; i++)
        {
            GameObject obj = Create(_prefab, key);
            CallDespawn(obj.GetInstanceID());

            stack.Push(obj);
            pending.Add(obj);
        }
    }
    #endregion

    #region 풀링
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
            if (policy.TryGetValue(key, out var p) && p.make > 0)
            {
                made.TryGetValue(key, out int cur);

                if (cur >= p.make && p.skip)
                    return null;
            }

            obj = Create(_prefab, key);
        }

        Transform t = obj.transform;
        t.SetParent(_parent, false);
        t.SetPositionAndRotation(_pos, Quaternion.identity);

        int id = obj.GetInstanceID();
        CallSpawn(id);
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

        CallDespawn(id);

        _obj.SetActive(false);

        if (policy.TryGetValue(key, out var p) && p.keep > 0 && stack.Count >= p.keep)
        {
            origin.Remove(id);
            hook.Remove(id);

            made.TryGetValue(key, out int cur);
            made[key] = cur - 1;

            Destroy(_obj);
            return;
        }

        stack.Push(_obj);
        pending.Add(_obj);
    }
    #endregion

    #region 유틸
    private GameObject Create(GameObject _prefab, int _key)
    {
        GameObject obj = Instantiate(_prefab);
        obj.SetActive(false);

        int id = obj.GetInstanceID();

        origin[id] = _key;
        hook[id] = obj.GetComponentsInChildren<IPoolable>(true);

        made.TryGetValue(_key, out int cur);
        made[_key] = cur + 1;

        return obj;
    }

    private void CallSpawn(int _id)
    {
        if (!hook.TryGetValue(_id, out var list)) return;

        for (int i = 0; i < list.Length; i++)
            list[i].OnSpawn();
    }

    private void CallDespawn(int _id)
    {
        if (!hook.TryGetValue(_id, out var list)) return;

        for (int i = 0; i < list.Length; i++)
            list[i].OnDespawn();
    }
    #endregion
}
