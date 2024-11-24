using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    private void Start()
    {
        Init();

        //CHMResource.Instance.Instantiate(CHMUnit.Instance.GetOriginBall());

        CHMUnit.Instance.CreateUnit(transform, DefEnum.EUnit.White, DefEnum.ELayer.Red, DefEnum.ELayer.Blue, Vector3.zero);

        CHMUnit.Instance.CreateUnit(transform, DefEnum.EUnit.White, DefEnum.ELayer.Blue, DefEnum.ELayer.Red, new Vector3(10, 0, 10));

        CHMUnit.Instance.CreateUnit(transform, DefEnum.EUnit.White, DefEnum.ELayer.Red, DefEnum.ELayer.Blue, new Vector3(20, 0, 10));

        CHMUnit.Instance.CreateUnit(transform, DefEnum.EUnit.White, DefEnum.ELayer.Blue, DefEnum.ELayer.Red, new Vector3(20, 0, 20));
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
}
