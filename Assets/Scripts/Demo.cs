using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    private DefEnum.ELayer _curCreateTeam = DefEnum.ELayer.Red;
    private DefEnum.EUnit _curUnit = DefEnum.EUnit.None;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        CHMJson.Instance.Init();
        CHMUnit.Instance.Init();
        CHMLevel.Instance.Init();
        CHMItem.Instance.Init();
        CHMSkill.Instance.Init();
        CHMPool.Instance.Init();
        CHMParticle.Instance.Init();
    }

    private void OnGUI()
    {
        Rect buttonRect = new Rect(10, 10, Screen.width / 10f, Screen.height / 5f);
        if (GUI.Button(buttonRect, "Create"))
        {
            DefEnum.ELayer enemyLayer = _curCreateTeam == DefEnum.ELayer.Red ? DefEnum.ELayer.Blue : DefEnum.ELayer.Red;

            if (_curUnit + 1 == DefEnum.EUnit.Max)
            {
                _curUnit = DefEnum.EUnit.None;
            }

            CHMUnit.Instance.CreateUnit(transform, ++_curUnit, _curCreateTeam, enemyLayer,
                new Vector3(Random.Range(-20f, 20f), 0, Random.Range(-20f, 20f)));

            _curCreateTeam = _curCreateTeam == DefEnum.ELayer.Red ? DefEnum.ELayer.Blue : DefEnum.ELayer.Red;
        }
    }
}
