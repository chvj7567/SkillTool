using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class AssetBundlePool
{
    #region Private Class
    [Serializable]
    class BundleItem
    {
        public string key;
        public AssetBundle obj;
    }
    #endregion

    #region Private Argument
    Dictionary<string, BundleItem> _dicBundleItem = new Dictionary<string, BundleItem>();
    #endregion

    #region Setter
    public void SetBundleItem(string _bundleName, AssetBundle _assetBundle)
    {
        if (_dicBundleItem.TryGetValue(_bundleName, out BundleItem item) == false && _assetBundle != null)
        {
            Debug.Log(_bundleName);
            _dicBundleItem.Add(_bundleName, new BundleItem
            {
                key = _bundleName,
                obj = _assetBundle,
            });
        }
    }
    #endregion

    #region Getter
    public AssetBundle GetBundleItem(string _bundleName)
    {
        if (_dicBundleItem.TryGetValue(_bundleName.ToLower(), out BundleItem ret))
        {
            return ret.obj;
        }
        else
        {
            return null;
        }
    }
    #endregion
}

public partial class ObjectPool
{
    #region Private Class
    [Serializable]
    class ObjectItem
    {
        public string key;
        public UnityEngine.Object obj;
    }
    #endregion

    #region Private Argument
    Dictionary<string, ObjectItem> _dicObjectItem = new Dictionary<string, ObjectItem>();
    #endregion

    #region Setter
    public void SetObjectItem<T>(string _bundleName, string _assetName, T _object)
    {
        string key = $"{_bundleName.ToLower()}/{_assetName.ToLower()}";
        if (_dicObjectItem.TryGetValue(key, out ObjectItem item) == false && _object != null)
        {
            _dicObjectItem.Add(key, new ObjectItem
            {
                key = _bundleName,
                obj = _object as UnityEngine.Object,
            });
        }
    }
    #endregion

    #region Getter
    public UnityEngine.Object GetObjectItem(string _bundleName, string _assetName)
    {
        string key = $"{_bundleName.ToLower()}/{_assetName.ToLower()}";
        if (_dicObjectItem.TryGetValue(key, out ObjectItem ret))
        {
            return ret.obj;
        }
        else
        {
            return null;
        }
    }
    #endregion
}

public class CHMAssetBundle : CHSingleton<CHMAssetBundle>
{
    #region Private Argument
    AssetBundlePool _assetBundlePool = new AssetBundlePool();
    ObjectPool _objectPool = new ObjectPool();
    #endregion

    #region Property
    public bool FirstDownload { get; set; }
    #endregion

    public void LoadAssetBundle(string _bundleName, AssetBundle _assetBundle)
    {
        _assetBundlePool.SetBundleItem(_bundleName, _assetBundle);
    }

    public void LoadAsset<T>(string _bundleName, string _assetName, Action<T> _callback) where T : UnityEngine.Object
    {
        var obj = _objectPool.GetObjectItem(_bundleName, _assetName);
        if (obj == null)
        {
            AssetBundle assetBundle = _assetBundlePool.GetBundleItem(_bundleName);

            if (assetBundle != null)
            {
                var tempObj = assetBundle.LoadAsset<T>(_assetName);
                _objectPool.SetObjectItem<T>(_bundleName, _assetName, tempObj);

                _callback(assetBundle.LoadAsset<T>(_assetName));
            }
            else
            {
                _callback(null);
            }
        }
        else
        {
            _callback(obj as T);
        }
    }

#if UNITY_EDITOR
    public void LoadAssetOnEditor<T>(string _bundleName, string _assetName, Action<T> _callback) where T : UnityEngine.Object
    {
        string path = null;

        if (typeof(T) == typeof(GameObject))
        {
            path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.prefab";
        }
        else if (typeof(T) == typeof(TextAsset))
        {
            path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.json";
        }
        else if (typeof(T) == typeof(Sprite))
        {
            path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.jpg";

            T temp = AssetDatabase.LoadAssetAtPath<T>(path);

            if (temp == null)
            {
                path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.png";
            }
        }
        else if (typeof(T) == typeof(Material))
        {
            path = $"Assets/AssetPieces/{_bundleName.ToLower()}/{_assetName}.mat";
        }
        else if (typeof(T) == typeof(Shader))
        {
            path = $"Assets/AssetPieces/{_bundleName.ToLower()}/{_assetName}.shader";

            T temp = AssetDatabase.LoadAssetAtPath<T>(path);

            if (temp == null)
            {
                path = $"Assets/AssetPieces/{_bundleName.ToLower()}/{_assetName}.shadergraph";
            }
        }
        else if (typeof(T) == typeof(Material))
        {
            path = $"Assets/AssetPieces/{_bundleName.ToLower()}/{_assetName}.json";
        }
        else if (typeof(T) == typeof(SkillData))
        {
            path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.asset";
        }
        else if (typeof(T) == typeof(UnitData))
        {
            path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.asset";
        }
        else if (typeof(T) == typeof(LevelData))
        {
            path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.asset";
        }
        else if (typeof(T) == typeof(ItemData))
        {
            path = $"Assets/AssetBundleResources/{_bundleName.ToLower()}/{_assetName}.asset";
        }
        T original = AssetDatabase.LoadAssetAtPath<T>(path);

        _callback(original);
    }
#endif
}
