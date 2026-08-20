using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { private set; get; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorUtility.SetDirty(this);
    }

    private static TAsset[] CollectDatas<TAsset>(string _filter, string[] _folders, System.Func<TAsset, int> _order) where TAsset : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets(_filter, _folders);
        var list = new List<TAsset>(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TAsset data = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            if (data != null)
                list.Add(data);
        }

        list.Sort((_a, _b) => _order(_a).CompareTo(_order(_b)));

        return list.ToArray();
    }

    private static T LoadAsset<T>() where T : ScriptableObject
    {
        string typeName = typeof(T).Name;
        string[] guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { "Assets/Datas" });
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
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

        SetDictionary();
    }

    #region 검색
    #endregion

    #region SET
    private void SetDictionary()
    {
    }
    #endregion

    #region GET
    #endregion

    #region 프로퍼티
    #endregion
}
