using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { private set; get; }

#if UNITY_EDITOR
    private void OnValidate()
    {
    }
#endif

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

    #region 풀링
    private T SpawnPool<T>(GameObject _prefab, Vector3 _pos, Transform _parent) where T : Component
        => PoolManager.Instance?.Rent(_prefab, _pos, _parent)?.GetComponent<T>();

    public void DespawnPool(Pooling _pooling)
        => PoolManager.Instance?.Release(_pooling.gameObject);
    #endregion

    #region SET
    public void ResetEntity()
    {
        SetEntity();
    }

    public void SetEntity()
    {
    }
    #endregion

    #region GET
    #endregion

    #region 프로퍼티
    #endregion
}
