using System;
using System.Collections.Generic;
using UnityEngine;
using static DefClass;

public partial class CHMJson : CHSingleton<CHMJson>
{
    #region Private Class
    [Serializable]
    class JsonData
    {
        public StringData[] arrStringData;
    }
    #endregion

    #region Private Argument
    int _loadCompleteFileCount = 0;
    int _loadingFileCount = 0;

    List<Action<TextAsset>> _liLoadAction = new List<Action<TextAsset>>();
    Dictionary<int, string> _dicStringData = new Dictionary<int, string>();
    #endregion

    #region Property
    public float LoadingPercent
    {
        get
        {
            if (_loadingFileCount == 0 || _loadCompleteFileCount == 0)
            {
                return 0;
            }

            return ((float)_loadCompleteFileCount) / _loadingFileCount * 100f;
        }
    }
    #endregion

    #region Initialize
    public bool Initialize => _initialize;

    bool _initialize = false;

    public void Init()
    {
        if (_initialize)
            return;

        _initialize = true;

        LoadJsonData();
    }

    public void Clear()
    {
        _liLoadAction.Clear();
        _dicStringData.Clear();
    }
    #endregion

    void LoadJsonData()
    {
        _loadCompleteFileCount = 0;
        _liLoadAction.Clear();

        _liLoadAction.Add(LoadStringInfo());

        _loadingFileCount = _liLoadAction.Count;
    }

    Action<TextAsset> LoadStringInfo()
    {
        Action<TextAsset> callback;

        _dicStringData.Clear();

        CHMResource.Instance.LoadJson(DefEnum.EJson.String, callback = (TextAsset textAsset) =>
        {
            var jsonData = JsonUtility.FromJson<JsonData>(("{\"arrStringData\":" + textAsset.text + "}"));
            foreach (var data in jsonData.arrStringData)
            {
                _dicStringData.Add(data.stringID, data.value);
            }

            ++_loadCompleteFileCount;
        });

        return callback;
    }

}
