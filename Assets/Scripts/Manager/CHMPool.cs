using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CHMPool : CHSingleton<CHMPool>

{
    #region Private Class
    class CHPool
    {
        public GameObject Original { get; private set; }
        public Transform Root { get; set; }

        Stack<CHPoolable> stPool = new Stack<CHPoolable>();

        public void Init(GameObject original, int count = 5)
        {
            Original = original;
            Root = new GameObject().transform;
            Root.name = $"{original.name}Root";

            for (int i = 0; i < count; ++i)
            {
                CHPoolable poolable = Create();
                poolable.transform.SetParent(Root, false);
                poolable.isUse = false;
                poolable.gameObject.SetActive(false);
                Push(poolable);
            }
        }

        CHPoolable Create()
        {
            GameObject go = Object.Instantiate<GameObject>(Original);
            go.name = Original.name;
            return go.GetOrAddComponent<CHPoolable>();
        }

        public void Push(CHPoolable poolable)
        {
            if (poolable == null) return;

            poolable.transform.SetParent(Root, false);
            poolable.isUse = false;
            poolable.gameObject.SetActive(false);

            stPool.Push(poolable);
        }

        public CHPoolable Pop(Transform parent)
        {
            CHPoolable poolable;

            if (stPool.Count > 0)
            {
                do
                {
                    if (stPool.Count <= 0)
                    {
                        poolable = Create();
                        break;
                    }

                    poolable = stPool.Pop();

                } while (poolable.isUse);
            }
            else
            {
                poolable = Create();
            }

            poolable.transform.SetParent(parent, false);
            poolable.isUse = true;
            poolable.gameObject.SetActive(true);

            return poolable;
        }
    }
    #endregion

    #region Parameter
    Dictionary<string, CHPool> _dicPool = new Dictionary<string, CHPool>();
    GameObject _objRoot;
    #endregion

    #region Initialize
    public bool Initialize => _initialize;

    bool _initialize = false;

    public void Init()
    {
        if (_initialize)
            return;

        _initialize = true;

        _objRoot = GameObject.Find("@CHMPool");
        if (_objRoot == null)
        {
            _objRoot = new GameObject { name = "@CHMPool" };
        }

        Object.DontDestroyOnLoad(_objRoot);
    }

    public void Clear()
    {
        foreach (Transform child in _objRoot.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        _dicPool.Clear();
    }
    #endregion

    #region Getter
    public GameObject GetOriginal(string name)
    {
        if (_dicPool.ContainsKey(name) == false)
            return null;
        return _dicPool[name].Original;
    }
    #endregion

    public void CreatePool(GameObject original, int count = 5)
    {
        CHPool pool = new CHPool();
        pool.Init(original, count);
        pool.Root.parent = _objRoot.transform;

        _dicPool.Add(original.name, pool);
    }

    public void Push(CHPoolable poolable)
    {
        if (poolable == null)
            return;

        if (_dicPool.ContainsKey(poolable.gameObject.name) == false)
        {
            GameObject.Destroy(poolable.gameObject);
            return;
        }

        _dicPool[poolable.gameObject.name].Push(poolable);
    }

    public CHPoolable Pop(GameObject original, Transform parent = null)
    {
        if (_dicPool.ContainsKey(original.name) == false)
            CreatePool(original);

        return _dicPool[original.name].Pop(parent);
    }
}
