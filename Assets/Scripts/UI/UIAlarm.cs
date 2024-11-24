using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class UIAlarmArg : CHUIArg
{
    public float closeTime = .5f;
    public int stringID;
    public string text;
    public Action actClose;
}

public class UIAlarm : UIBase
{
    UIAlarmArg _arg;

    [SerializeField] CHTMPro txtAlarm;

    CancellationTokenSource _cancleTokenSource;

    public override void Init(CHUIArg uiArg, DefEnum.EUI uiType, long uid)
    {
        base.Init(uiArg, uiType, uid);
        _arg = uiArg as UIAlarmArg;
    }

    private async void Start()
    {
        _cancleTokenSource = new CancellationTokenSource();

        actBack += () =>
        {
            _cancleTokenSource.Cancel();

            if (_arg.actClose != null)
                _arg.actClose.Invoke();
        };

        if (_arg.text != null)
        {
            txtAlarm.SetText(_arg.text);
        }
        else
        {
            txtAlarm.SetStringID(_arg.stringID);
        }

        await Task.Delay((int)(_arg.closeTime * 1000));

        if (_cancleTokenSource != null && _cancleTokenSource.IsCancellationRequested)
            return;

        actBack.Invoke();

        CHMUI.Instance.CloseUI(gameObject);
    }
}
