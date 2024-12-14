using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
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
        Rect buttonRect = new Rect(10, 10, 500, 300);
        if (GUI.Button(buttonRect, "Create"))
        {
            DefEnum.ELayer myLayer = (DefEnum.ELayer)Random.Range((int)DefEnum.ELayer.Red, (int)DefEnum.ELayer.Blue + 1);
            DefEnum.ELayer enemyLayer;

            if (myLayer == DefEnum.ELayer.Red)
                enemyLayer = DefEnum.ELayer.Blue;
            else
                enemyLayer= DefEnum.ELayer.Red;

            var unit = (DefEnum.EUnit)Random.Range((int)DefEnum.EUnit.None, (int)DefEnum.EUnit.Max);
            CHMUnit.Instance.CreateUnit(transform, DefEnum.EUnit.White, myLayer, enemyLayer,
                new Vector3(Random.Range(-20f, 20f), 0, Random.Range(-20f, 20f)));
        }
    }
}
