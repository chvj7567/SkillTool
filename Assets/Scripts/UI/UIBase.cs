using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class UIBase : MonoBehaviour
{
    [SerializeField] Button btnBackground;
    [SerializeField] Button btnBack;
    [ReadOnly] DefEnum.EUI _uiType;
    [ReadOnly] long _uid;

    public DefEnum.EUI UIType { get { return _uiType; } }
    public long UID { get { return _uid; } }

    public Action actBack;

    private void Awake()
    {
        GameObject _canvas = GameObject.FindGameObjectWithTag("UICanvas");
        if (_canvas)
        {
            transform.SetParent(_canvas.transform, false);
        }

        actBack += () =>
        {
            Debug.Log($"{UIType} exit");
        };

        if (btnBackground)
        {
            btnBackground.OnClickAsObservable().Subscribe(_ =>
            {
                actBack.Invoke();
                CHMUI.Instance.CloseUI(gameObject);
            });
        }

        if (btnBack)
        {
            btnBack.OnClickAsObservable().Subscribe(_ =>
            {
                actBack.Invoke();
                CHMUI.Instance.CloseUI(gameObject);
            });
        }
    }

    public virtual void Init(CHUIArg uiArg, DefEnum.EUI uiType, long uid)
    {
        _uiType = uiType;
        _uid = uid;
    }

    public virtual void CloseUI() { }
}
