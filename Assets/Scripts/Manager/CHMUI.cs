using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CHUIArg
{

}

public class CHMUI : CHSingleton<CHMUI>
{
    class CHUIWaitData
    {
        public int uid;
        public DefEnum.EUI uiType;
        public CHUIArg uiArg;
    }

    List<GameObject> _liActiveUI = new List<GameObject>();
    List<int> _liActiveUID = new List<int>();
    List<CHUIWaitData> _liWaitActiveUI = new List<CHUIWaitData>();
    List<GameObject> _liWaitCloseUI = new List<GameObject>();
    GameObject _objCarmera = null;
    
    public int UID { get; private set; }

    public void CreateEventSystemObject()
    {
        CHMResource.Instance.InstantiateUI(DefEnum.EUI.EventSystem);
    }

    public void UpdateUI()
    {
        if (_liWaitCloseUI.Count != 0)
        {
            foreach (var waitData in _liWaitCloseUI)
            {
                CloseWaitUI(waitData);
            }

            _liWaitCloseUI.Clear();
        }

        if (_liWaitActiveUI.Count != 0)
        {
            foreach (var waitData in _liWaitActiveUI)
            {
                ShowUI(waitData);
            }

            _liWaitActiveUI.Clear();
        }
    }

    public int ShowUI(DefEnum.EUI eUI, CHUIArg uiArg)
    {
        var uiWateData = new CHUIWaitData
        {
            uid = UID,
            uiType = eUI,
            uiArg = uiArg,
        };

        _liActiveUID.Add(UID);
        _liWaitActiveUI.Add(uiWateData);

        return UID++;
    }

    void ShowUI(CHUIWaitData uiWaitData)
    {
        GameObject uiCanvas = null;

        uiCanvas = GameObject.FindGameObjectWithTag("UICanvas");

        if (uiCanvas == null)
        {
            CHMResource.Instance.InstantiateUI(DefEnum.EUI.UICanvas, (GameObject canvas) =>
            {
                uiCanvas = canvas;
            });
        }

        CHMResource.Instance.InstantiateUI(uiWaitData.uiType, (GameObject uiObj) =>
        {
            if (uiObj == null)
                return;

            uiObj.transform.SetParent(uiCanvas.transform);
            if (false == _liActiveUID.Contains(uiWaitData.uid))
            {
                uiObj.SetActive(false);
                CHMResource.Instance.Destroy(uiObj);
                uiObj = null;
            }

            if (uiObj)
            {
                _liActiveUI.Add(uiObj);
                var script = uiObj.GetComponent<UIBase>();
                script.Init(uiWaitData.uiArg, uiWaitData.uiType, uiWaitData.uid);
                uiObj.SetActive(true);

                var local = uiObj.transform.localPosition;
                local.z = 0;
                uiObj.transform.localPosition = local;
            }
        });
    }

    public void CloseWaitUI(GameObject objUI)
    {
        if (objUI)
        {
            _liActiveUI.Remove(objUI);
            objUI.SetActive(false);
            CHMResource.Instance.Destroy(objUI);
        }

        if (_liActiveUI.IsNullOrEmpty())
        {
            if (_objCarmera != null) _objCarmera.SetActive(false);
        }
    }

    public void CloseUI(GameObject objUI)
    {
        if (objUI)
        {
            var popup = objUI.GetComponent<UIBase>();
            popup.CloseUI();
            _liWaitCloseUI.Add(objUI);
        }
    }

    public void CloseUI(DefEnum.EUI eUI)
    {
        _liActiveUI = _liActiveUI.FindAll(_ => _ != null);
        foreach (var obj in _liActiveUI)
        {
            var ui = obj.GetComponent<UIBase>();
            if (ui.UIType == eUI)
            {
                ui.CloseUI();
                _liWaitCloseUI.Add(obj);
            }
        }
    }

    public bool CheckShowUI()
    {
        _liActiveUI = _liActiveUI.FindAll(_ => _ != null);
        foreach (var obj in _liActiveUI)
        {
            return true;
        }

        return false;
    }

    public bool CheckShowUI(DefEnum.EUI eUI)
    {
        _liActiveUI = _liActiveUI.FindAll(_ => _ != null);
        foreach (var obj in _liActiveUI)
        {
            var ui = obj.GetComponent<UIBase>();
            if (ui.UIType == eUI)
            {
                return true;
            }
        }

        return false;
    }
}
